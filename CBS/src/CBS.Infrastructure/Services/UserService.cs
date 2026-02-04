using CBS.Application.DTO;
using CBS.Application.Interfaces;
using CBS.Domain.Common;
using CBS.Domain.Entities;
using Dapper;
using Microsoft.VisualBasic;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

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
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // 1) Duplicate username check (Trim used for safety)
            const string dupSql = "SELECT COUNT(1) FROM users WHERE username = @Username;";
            int exists = await conn.ExecuteScalarAsync<int>(dupSql, new { Username = dto.Username.Trim() }, transaction);
            if (exists > 0) return Result<UserResponseDTO>.Failure("Username already exists.");

            // 2) Hash password
            string hashPass = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 3) Domain entity creation
            AppUser user;
            try
            {
                user = AppUser.Create(dto.Username, hashPass, dto.Role, dto.BranchId);
            }
            catch (Exception ex)
            {
                return Result<UserResponseDTO>.Failure(ex.Message);
            }

            // 4) Insert user
            const string insertSql = @"
            INSERT INTO users (username, password_hash, role, branch_id, failed_attempts, lock_until, is_locked, is_active, last_login, created_by, row_version)
            VALUES (@Username, @PasswordHash, @Role, @BranchId, @FailedAttempts, @LockUntil, @IsLocked, @IsActive, @LastLogin, @CreatedBy, @RowVersion);
            SELECT LAST_INSERT_ID();";

            int newId = await conn.ExecuteScalarAsync<int>(insertSql, new
            {
                user.Username,
                user.PasswordHash,
                Role = user.Role.ToString(),
                user.BranchId,
                user.FailedAttempts,
                user.LockUntil,
                user.IsLocked,
                user.IsActive,
                user.LastLogin,
                CreatedBy = currentUserId,
                user.RowVersion
            }, transaction);

            if (newId <= 0)
            {
                await transaction.RollbackAsync();
                return Result<UserResponseDTO>.Failure("Failed! User not created.");
            }

            //  5) Fetch Branch Code (Separated Logic for Clarity) ---
            string branchCode = "N/A";
            if (user.BranchId.HasValue && user.BranchId > 0)
            {
                const string branchSql = "SELECT branch_code FROM branches WHERE id = @BranchId;";
                branchCode = await conn.QueryFirstOrDefaultAsync<string>(branchSql, new { BranchId = user.BranchId }, transaction) ?? "N/A";
            }

            // 6) Audit Log
            var auditDto = new AuditLogCreateDTO(
                BranchCode: branchCode, // Using the fetched code
                TableName: "users",
                Action: "CREATE",
                UpdatedBy: null,
                ApprovedBy: null,
                OldValue: "-",
                NewValue: $"Username:{user.Username}, Role:{user.Role}, Branch:{branchCode}",
                Description: "New user created",
                CreatedBy: currentUserId
            );

            var auditRes = await _auditLogService.CreateAuditLogAsync(auditDto);
            if (!auditRes.IsSuccess)
            {
                await transaction.RollbackAsync();
                return Result<UserResponseDTO>.Failure("Audit log failed. Transaction rolled back.");
            }

            await transaction.CommitAsync();

            // 7) Mapping to Response DTO (UI will use BranchCode to show labels)
            var response = new UserResponseDTO(
                Id: newId,
                Username: user.Username,
                Role: user.Role,
                BranchCode: branchCode, // ✅ Best way: UI now has the display string
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
            await transaction.RollbackAsync();
            return Result<UserResponseDTO>.Failure("Critical Error: " + ex.Message);
        }
    }










    public async Task<Result<UserSearchDTO>> GetByUsernameAsync(string userName, int currentUserId)
    {
        using var conn = await _dataSource.OpenConnectionAsync();
        
        try
        {

            const string sql = @"SELECT 
                                id AS Id, 
                                username AS Username, 
                                role AS Role, 
                                branch_id AS BranchId, 
                                failed_attempts AS FailedAttempts, 
                                lock_until AS LockUntil, 
                                is_locked AS IsLocked, 
                                is_active AS IsActive, 
                                last_login AS LastLogin,
                                created_by AS CreatedBy,
                                approved_by AS ApprovedBy,
                                updated_by AS UpdatedBy,
                                created_at AS CreatedAt,
                                updated_at AS UpdatedAt,
                                row_version AS RowVersion
                             FROM users 
                             WHERE username = @userName";

            //<BranchSearchDTO>কি,কি অর্ডার (কাচ্চি বিরিয়ানি,মাছ)
            var user = await conn.QueryFirstOrDefaultAsync<UserSearchDTO>(sql, new { UserName = userName.Trim() });

            if (user == null)
            {
                return Result<UserSearchDTO>.Failure("Branch not found");
            }

            // --- Find branch code with branch id---
            string branchCodeForLog = "N/A"; 

            if (user.BranchId != null && user.BranchId > 0)
            {
                const string branchSql = "SELECT branch_code AS branchCode, branch_name AS branchName FROM branches WHERE id = @BranchId;";
                var result = await conn.QueryFirstOrDefaultAsync<dynamic>(branchSql, new { BranchId = user.BranchId });

                if (result != null)
                {
                    user.BranchCode = result.branchCode; //set branch code and name to user dto
                    user.BranchName = result.branchName; //set branch code and name to user dto

                    branchCodeForLog = result.branchCode;
                }
            }


            // 3. audit log create(new data save so OldValue=null) 
            var auditDto = new AuditLogCreateDTO(
                BranchCode: branchCodeForLog,
                CreatedBy: user.Id,
                UpdatedBy: user.UpdatedBy,
                ApprovedBy: user.ApprovedBy,
                TableName: "users",
                Action: "READ",
                OldValue: $"Username:{user.Username}, Role:{user.Role}, BranchCode:{branchCodeForLog}, Active:{user.IsActive}",
                NewValue: $"Read By:{currentUserId}",
                Description: "Branch Info. Searched by User."
            );

            var auditResult = await _auditLogService.CreateAuditLogAsync(auditDto);

            if (!auditResult.IsSuccess)
            {
                return Result<UserSearchDTO>.Failure("Audit log failed. Transaction rolled back.");
            }

           
            return Result<UserSearchDTO>.Success(user); // Got fish?! pack and send(Result<>,Wrapper Class)
        }
        catch (Exception ex)
        {
            return Result<UserSearchDTO>.Failure("Database Error in GetByUsernameAsync : " + ex.Message);
        }

    }









    public async Task<Result<bool>> UpdateUserAsync(UserUpdateDTO dto, int currentUserId)
    {
        using var conn = await _dataSource.OpenConnectionAsync();
        using var transaction = conn.BeginTransaction(); // Start the transaction

        try
        {
            // Check if user exists (Get old data for auditing)
            const string checkSql = "SELECT role, branch_id, is_active, is_locked, row_version FROM users WHERE id = @Id";
            var user = await conn.QueryFirstOrDefaultAsync<AppUser>(checkSql, new { Id = dto.Id }, transaction);

            if (user == null)
            {
                return Result<bool>.Failure("User not found.");
            }

            var OldValue = $"Role:{user.Role}, Branch:{user.BranchId}, Active:{user.IsActive}, Active:{user.IsLocked}";

            try
            {
                user.UpdateGeneralInfo(dto.Username, dto.Role, dto.BranchId, dto.IsActive, dto.IsLocked, dto.RowVersion, currentUserId);
            }
            catch(Exception ex)
            {
                return Result<bool>.Failure(ex.Message);
            }

            // Perform Update
            const string updateSql = @"
            UPDATE users SET 
                role = Role,
                branch_id = @BranchId,
                is_active = @IsActive,
                is_locked = @IsLocked,
                updated_by = @UpdatedBy,
                updated_at = @UpdatedAt,
                row_version = @RowVersion
            WHERE id = @Id";

            var affectedRows = await conn.ExecuteAsync(updateSql, new
            {
                Role = user.Role.ToString(),
                user.BranchId,
                user.IsActive,
                user.IsLocked,
                user.UpdatedBy,
                user.UpdatedAt,
                user.RowVersion,
                Id = dto.Id
            }, transaction);

            if (affectedRows <= 0)
            {
                transaction.Rollback();
                return Result<bool>.Failure("Update failed. No records were modified.");
            }

            //Fetch Branch Code(Separated Logic for Clarity) ---
           string branchCode = "N/A";
            if (dto.BranchId != null && dto.BranchId > 0)
            {
                const string branchSql = "SELECT branch_code FROM branches WHERE id = @BranchId;";
                branchCode = await conn.QueryFirstOrDefaultAsync<string>(branchSql, new { BranchId = dto.BranchId }, transaction) ?? "N/A";
            }

            // Create Audit Log (Pass the transaction to the audit service if supported)
            var auditDto = new AuditLogCreateDTO(
                BranchCode: branchCode,
                CreatedBy: dto.CreatedBy,
                UpdatedBy: currentUserId,
                ApprovedBy: dto.ApprovedBy,
                TableName: "users",
                Action: "UPDATE",
                OldValue: OldValue,
                NewValue: $"Role:{dto.Role}, Branch:{branchCode}, Active:{dto.IsActive}",
                Description: $"User {dto.Username} updated by ID {currentUserId}"
            );

            // NOTE: Ensure your _auditLogService.CreateAuditLogAsync can accept an IDbTransaction
            var auditResult = await _auditLogService.CreateAuditLogAsync(auditDto);

            if (!auditResult.IsSuccess)
            {
                transaction.Rollback(); // Rollback update if audit fails
                return Result<bool>.Failure("Audit log failed. Update rolled back.");
            }

            transaction.Commit(); // Success! Save everything.
            return Result<bool>.Success(true, "User updated successfully.");
        }
        catch (Exception ex)
        {
            transaction.Rollback(); // Rollback on any error
            return Result<bool>.Failure("Database Error: " + ex.Message);
        }
    }







    public async Task<Result<IEnumerable<GetAllUsersIdAndNameDTO>>> GetAllUsersIdAndNameAsync()
    {
        using var conn = await _dataSource.OpenConnectionAsync();

        try
        {
            const string sql = @"SELECT 
                                id AS Id, 
                                user_name AS UserName
                             FROM users 
                             WHERE is_active = true";
            //AND is_active = true";

            var users = await conn.QueryAsync<GetAllUsersIdAndNameDTO>(sql);

            if (users == null || !users.Any())
            {
                return Result<IEnumerable<GetAllUsersIdAndNameDTO>>.Failure("No Users Found for dropdown");
            }

            return Result<IEnumerable<GetAllUsersIdAndNameDTO>>.Success(users, "Users found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<GetAllUsersIdAndNameDTO>>.Failure("Database Error: " + ex.Message);
        }
    }



















}