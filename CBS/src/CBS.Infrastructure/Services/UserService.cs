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






    // ---------------------------------- CREATE ------------------------------


    public async Task<Result<UserResponseDTO>> CreateUserAsync(UserCreateDTO dto, int currentUserId)
    {
        // 1. Domain-level validation and normalization are recommended here.
        AppUser user;
        try
        {
            // password hash
            string hashPass = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);

            user = AppUser.Create(dto.Username, hashPass, dto.Role, dto.BranchId, currentUserId);


        }
        catch (Exception ex)
        {
            return Result<UserResponseDTO>.Failure(ex.Message);
        }

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // 2. Duplicate username check (A Unique Index must exist in the database for absolute safety).
            const string dupSql = "SELECT COUNT(1) FROM users WHERE username = @Username;";
            int exists = await conn.ExecuteScalarAsync<int>(dupSql, new { Username = user.Username }, transaction);
            if (exists > 0) return Result<UserResponseDTO>.Failure("Username already exists.");

            // 3. Insert query
            const string insertSql = @"
            INSERT INTO users (username, password_hash, role, branch_id, created_by, row_version, created_at)
            VALUES (@Username, @PasswordHash, @Role, @BranchId, @CreatedBy, @RowVersion, @CreatedAt);
            SELECT LAST_INSERT_ID();";

            var newId = await conn.ExecuteScalarAsync<int>(insertSql, new
            {
                user.Username,
                user.PasswordHash,
                Role = user.Role.ToString(),
                user.BranchId,
                user.CreatedBy,
                user.RowVersion,
                user.CreatedAt // Ensure this is coming from the Domain as UTC time
            }, transaction);

            // 4. Audit logging (using the existing connection and transaction)
            var branchSql = "SELECT branch_code FROM branches WHERE id = @BranchId;";
            var branchCode = await conn.QueryFirstOrDefaultAsync<string>(branchSql, new { user.BranchId }, transaction) ?? "N/A";

            var auditRes = await _auditLogService.CreateAuditLogAsync(new AuditLogCreateDTO(
               BranchCode: branchCode, // Using the fetched code
                TableName: "users",
                Action: "CREATE",
                UpdatedBy: null,
                ApprovedBy: null,
                OldValue: "-",
                NewValue: $"Username:{user.Username}, Role:{user.Role}, Branch:{branchCode}",
                Description: "New user created",
                CreatedBy: currentUserId
            ), conn, transaction);

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
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return Result<UserResponseDTO>.Failure("Database Error during user creation.");
        }
    }











    // ---------------------------------- SEARCH ------------------------------


    public async Task<Result<UserSearchDTO>> GetByUsernameAsync(string userName, int currentUserId)
    {
        // 1. Guard Clause: Input validation
        if (string.IsNullOrWhiteSpace(userName))
            return Result<UserSearchDTO>.Failure("Username is required.");

        await using var conn = await _dataSource.OpenConnectionAsync();

        try
        {
            // 2. JOIN Query: Fetching User and Branch data in a single call (Best for performance)
            const string sql = @"
            SELECT 
                u.id, u.username, u.role, u.branch_id AS BranchId, 
                u.failed_attempts AS FailedAttempts, u.lock_until AS LockUntil, 
                u.is_locked AS IsLocked, u.is_active AS IsActive, 
                u.last_login AS LastLogin, u.created_by AS CreatedBy, 
                u.approved_by AS ApprovedBy, u.updated_by AS UpdatedBy, 
                u.created_at AS CreatedAt, u.updated_at AS UpdatedAt, 
                u.row_version AS RowVersion,
                b.branch_code AS BranchCode, b.branch_name AS BranchName
            FROM users u
            LEFT JOIN branches b ON u.branch_id = b.id
            WHERE u.username = @UserName";

            // Dapper automatically maps the result to UserSearchDTO properties
            var dto = await conn.QueryFirstOrDefaultAsync<UserSearchDTO>(sql, new { UserName = userName.Trim().ToLowerInvariant() });

            if (dto == null)
                return Result<UserSearchDTO>.Failure("User not found.");



            return Result<UserSearchDTO>.Success(dto);
        }
        catch (Exception)
        {
            // Actual exception 'ex.Message' should be logged internally (e.g., using Serilog or NLog)
            return Result<UserSearchDTO>.Failure("An internal error occurred while searching for the user.");
        }
    }




// ---------------------------------- UPDATE ------------------------------

    public async Task<Result<bool>> UpdateUserAsync(UserUpdateDTO dto, int currentUserId)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // 1. Fetch current data with BranchCode for a complete Audit Trail
            const string checkSql = @"
            SELECT u.id, u.username, u.role, u.branch_id, u.is_active, u.is_locked, u.row_version, b.branch_code 
            FROM users u
            LEFT JOIN branches b ON u.branch_id = b.id
            WHERE u.id = @Id";

            var oldData = await conn.QueryFirstOrDefaultAsync<dynamic>(checkSql, new { Id = dto.Id }, transaction);

            if (oldData == null)
                return Result<bool>.Failure("User not found.");

            // 2. Prepare OldValue snapshot (includes BranchCode for better auditing)
            var oldValue = $"Role:{oldData.role}, Branch:{oldData.branch_code}, Active:{oldData.is_active}, Locked:{oldData.is_locked}, Version:{oldData.row_version}";

            // 3. Apply Domain Logic and Concurrency Check
            var userEntity = AppUser.Reconstruct(
                (int)oldData.id, (string)oldData.username, (UserRole)Enum.Parse(typeof(UserRole), oldData.role),
                (int)oldData.branch_id, (bool)oldData.is_active, (bool)oldData.is_locked, (int)oldData.row_version
            );

            try
            {
                // Validates version and increments RowVersion inside this method
                userEntity.UpdateGeneralInfo(dto.Username, dto.Role, dto.BranchId, dto.IsActive, dto.IsLocked, dto.RowVersion, currentUserId);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message);
            }

            // 4. Secure SQL Update with Optimistic Concurrency (WHERE row_version = @OldVersion)
            const string updateSql = @"
            UPDATE users SET 
                role = @Role,
                branch_id = @BranchId,
                is_active = @IsActive,
                is_locked = @IsLocked,
                updated_by = @UpdatedBy,
                updated_at = @UpdatedAt,
                row_version = @NewRowVersion
            WHERE id = @Id AND row_version = @OldRowVersion";

            var affectedRows = await conn.ExecuteAsync(updateSql, new
            {
                Role = userEntity.Role.ToString(),
                userEntity.BranchId,
                userEntity.IsActive,
                userEntity.IsLocked,
                userEntity.UpdatedBy,
                userEntity.UpdatedAt,
                NewRowVersion = userEntity.RowVersion, // Incremented version
                OldRowVersion = dto.RowVersion,        // Original version from UI
                Id = dto.Id
            }, transaction);

            if (affectedRows <= 0)
            {
                await transaction.RollbackAsync();
                return Result<bool>.Failure("Update failed: The record was modified by another user (Concurrency Conflict).");
            }

            // 5. Fetch New Branch Code for the NewValue snapshot
            const string branchSql = "SELECT branch_code FROM branches WHERE id = @BranchId;";
            var newBranchCode = await conn.QueryFirstOrDefaultAsync<string>(branchSql, new { BranchId = userEntity.BranchId }, transaction) ?? "N/A";

            // Create Audit Log (Pass the transaction to the audit service if supported)
            var auditDto = new AuditLogCreateDTO(
                BranchCode: newBranchCode,
                CreatedBy: dto.CreatedBy,
                UpdatedBy: currentUserId,
                ApprovedBy: dto.ApprovedBy,
                TableName: "users",
                Action: "UPDATE",
                OldValue: oldValue,
                NewValue: $"Role:{userEntity.Role}, Branch:{newBranchCode}, Active:{userEntity.IsActive}, Locked:{userEntity.IsLocked}, Version:{userEntity.RowVersion}",
                Description: $"User {dto.Username} updated by ID {currentUserId}"
            );

            // Crucial: Pass the existing transaction to the audit service
            var auditResult = await _auditLogService.CreateAuditLogAsync(auditDto, conn, transaction);

            if (!auditResult.IsSuccess)
            {
                await transaction.RollbackAsync();
                return Result<bool>.Failure("Security Policy Violation: Audit log failed. Transaction aborted.");
            }

            await transaction.CommitAsync();
            return Result<bool>.Success(true, "User updated successfully.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return Result<bool>.Failure("A critical database error occurred. Operation rolled back.");
        }
    }










    // ---------------------------------- ALL USERS ------------------------------

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