using CBS.Application.DTO;
using CBS.Application.Interfaces;
using CBS.Domain.Common;
using CBS.Domain.Entities;
using Dapper;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBS.Infrastructure.Services;


public class UserService : IUserService
{
    
    // Login policy
    private const int MAX_WRONG_ATTEMPTS = 3;
    private static readonly TimeSpan LOCK_DURATION = TimeSpan.FromMinutes(15);
   
    
    private readonly MySqlDataSource _dataSource;
    private readonly IAuditLogService _auditLogService;

    public UserService(MySqlDataSource dataSource, IAuditLogService auditLogService)
    {
        _dataSource = dataSource;
        _auditLogService = auditLogService;
    }






    public async Task<Result<UserResponseDTO>> CreateUserAsync(UserCreateDTO dto, int currentUserId)
    {
        using var conn = await _dataSource.OpenConnectionAsync();
        using var transactoin = await conn.BeginTransactionAsync();

        try
        {
            // 1) Duplicate username check
            const string dupSql = "SELECT COUNT(1) FROM users WHERE username = @Username;";
            int exists = await conn.ExecuteScalarAsync<int>(dupSql, new { Username = dto.Username.Trim() }, transactoin);
            if (exists > 0) return Result<UserResponseDTO>.Failure("Username already exists.");

            // 2) Hash password using BCrypt
            string hashPass = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 3) Domain entity creation (business validation in one place)
            AppUser user;
            try
            {
                user = AppUser.Create(dto.Username, hashPass, dto.Role, dto.BranchCode);
            }
            catch (Exception ex)
            {
                return Result<UserResponseDTO>.Failure(ex.Message);
            }

            // 4) Insert user
            const string insertSql = @"
                                     INSERT INTO users (username, password_hash, role, branch_code, failed_attempts, lock_until, is_locked, is_active, last_login)
                                     VALUES (@Username, @PasswordHash, @Role, @BranchCode, @FailedAttempts, @LockUntil, @IsLocked, @IsActive, @LastLogin);
                                     SELECT LAST_INSERT_ID();";

            int newId = await conn.ExecuteScalarAsync<int>(insertSql, new
            {
                Username = user.Username,
                PasswordHash = user.PasswordHash,
                Role = user.Role.ToString(), // store enum as string matching DB enum
                BranchCode = user.BranchCode,
                FailedAttempts = user.FailedAttempts,
                LockUntil = user.LockUntil,
                IsLocked = user.IsLocked,
                IsActive = user.IsActive,
                LastLogin = user.LastLogin
            }, transactoin);

            if (newId <= 0)
            {
                await transactoin.RollbackAsync();
                return Result<UserResponseDTO>.Failure("Failed! User not created.");
            }

            // 5) Audit log inside same transaction behaviour (if you want strict consistency)
            var auditDto = new AuditLogCreateDTO(
                BranchCode: user.BranchCode,
                TableName: "users",
                Action: "CREATE",
                UpdatedBy: null,
                ApprovedBy: null,
                OldValue: "-",
                NewValue: $"Username:{user.Username}, Role:{user.Role}, BranchCode: { user.BranchCode}, Status: {(user.IsActive ? "Active" : "Inactive")}",
                Description: "New user created",
                CreatedBy: currentUserId
            );



         var auditRes = await _auditLogService.CreateAuditLogAsync(auditDto);
            if (!auditRes.IsSuccess)
            {
                await transactoin.RollbackAsync();
                return Result<UserResponseDTO>.Failure("Audit log failed. Transaction rolled back.");
            }

            await transactoin.CommitAsync();

            var response = new UserResponseDTO(
                                Id: newId,
                                Username: user.Username,
                                Role: user.Role,
                                BranchCode: user.BranchCode,
                                IsActive: user.IsActive,
                                IsLocked: user.IsLocked,
                                FailedAttempts: user.FailedAttempts,
                                LockUntil: user.LockUntil,
                                LastLogin: user.LastLogin
                            );

            return Result<UserResponseDTO>.Success(response, "User created successfully.");
        }
        catch (Exception ex)
        {
            await transactoin.RollbackAsync();
            return Result<UserResponseDTO>.Failure("Database Error: " + ex.Message);
        }
    }








}