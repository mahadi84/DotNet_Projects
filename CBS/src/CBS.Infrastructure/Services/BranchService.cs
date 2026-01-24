
using CBS.Application.DTO;
using CBS.Application.Interfaces;
using CBS.Domain.Common;
using CBS.Domain.Entities;
using Dapper;
using Microsoft.VisualBasic;
using MySqlConnector;
using System.Reflection.Metadata;
using System.Transactions;


namespace CBS.Infrastructure.Services;


public class BranchService : IBranchService
{
    private readonly MySqlDataSource _dataSource;
    private readonly IAuditLogService _auditLogService;

    public BranchService(MySqlDataSource dataSource, IAuditLogService auditLogService)
    {
        _dataSource = dataSource;
        _auditLogService = auditLogService;
    }















    public async Task<Result<dynamic>> CreateBranchAsync(BranchCreateDTO bcdto, int UserID)
    {
        using var connection = await _dataSource.OpenConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(); // Banking transaction start

        try
        {
            // 1. duplicate check
            const string checkSql = "SELECT COUNT(1) FROM branches WHERE branch_code = @BranchCode;";
            var countBranchCode = await connection.ExecuteScalarAsync<int>(checkSql, new { bcdto.BranchCode }, transaction);

            if (countBranchCode > 0)
            {
                return Result<dynamic>.Failure("Branch code already exists.");
            }

            // 2. data insert 
            const string insertSql = @"
                INSERT INTO branches (branch_code, branch_name, vault_balance, created_by, updated_by, approved_by, row_version, is_active) 
                VALUES (@BranchCode, @BranchName, @VaultBalance, @CreatedBy, @UpdatedBy, @ApprovedBy, @RowVersion, @IsActive);
                SELECT LAST_INSERT_ID();";

            var insertParameters = new
            {
                bcdto.BranchCode,  // ✅ First parameter should match column order
                bcdto.BranchName,
                bcdto.VaultBalance,
                CreatedBy = UserID,     // ✅ UserID from method parameter
                UpdatedBy = (int?)null,    // ✅ Use 0 or NULL as needed
                ApprovedBy = (int?)null,   // ✅ Use 0 or NULL as needed
                RowVersion = 1,
                IsActive = true
            };

            var newId = await connection.ExecuteScalarAsync<int>(insertSql, insertParameters, transaction);

            if (newId <= 0)
            {
                await transaction.RollbackAsync(); //RollBack, If Branch data not saved
                return Result<dynamic>.Failure("Failed! Branch data not saved.");
            }

            // 3. audit log create(new data save so OldValue=null) 
            var auditDto = new AuditLogCreateDTO(
                BranchCode: bcdto.BranchCode,
                CreatedBy: UserID,
                UpdatedBy: (int?)null,
                ApprovedBy: (int?)null,
                TableName: "branches",
                Action: "CREATE",
                OldValue: "-",
                NewValue: $"{bcdto.VaultBalance}",
                Description: "New Branch Created"
            );

            var auditResult = await _auditLogService.CreateAuditLogAsync(auditDto);

            if (!auditResult.IsSuccess)
            {
                await transaction.RollbackAsync(); //RollBack, If failed to create Audit Log  
                return Result<dynamic>.Failure("Audit log failed. Transaction rolled back.");
            }

            // 4. commit transaciton, if everything is ok
            await transaction.CommitAsync();

            // 5. get response 
            const string selectSql = "SELECT branch_name, branch_code, row_version FROM branches WHERE id = @Id;";
            var createdData = await connection.QuerySingleOrDefaultAsync<dynamic>(selectSql, new { Id = newId });

            return Result<dynamic>.Success(createdData, "Branch Created Successfully with Audit Log.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(); //RollBack, if data is not saved properly
            return Result<dynamic>.Failure("Database Error: " + ex.Message);
        }
    }











    public async Task<Result<BranchSearchDTO>> GetByBranchCodeAsync(string branchCode, int UserID)
    {
        using var conn = await _dataSource.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync(); // Banking transaction start

        try
        {

            //var branch = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, branchCode);
            //if (branch == null) return Result<dynamic>.Failure("Branch not found");
            //return Result<dynamic>.Success(branch, "Branch found");


            //const string sql = @"SELECT id, branch_code, branch_name, vault_balance, row_version 
            //                     FROM branches WHERE branch_code = @BranchCode AND is_active = TRUE";

            const string sql = @"SELECT 
                        id AS Id, 
                        branch_code AS BranchCode, 
                        branch_name AS BranchName, 
                        vault_balance AS VaultBalance, 
                        row_version AS RowVersion 
                     FROM branches 
                     WHERE branch_code = @BranchCode AND is_active = TRUE";

            //<BranchSearchDTO>কি,কি অর্ডার (কাচ্চি বিরিয়ানি,মাছ)
            var branch = await conn.QueryFirstOrDefaultAsync<BranchSearchDTO>(sql, new { BranchCode = branchCode.Trim() }, transaction);

            if (branch == null)
            {
                //ওয়েটার খালি হাতে ফিরে এসে চুপচাপ দাঁড়িয়ে থাকলো। আপনি বুঝলেন না কি হয়েছে। এটি হলো null রিটার্ন করা।
                //কিভাবে রিপোর্ট করছি, যদি কাচ্চি বিরিয়ানি, মাছ না পাওয়া যায়, তবে বক্সে একটি চিরকুট থাকবে: "দুঃখিত, এই কোডের কোনো কাচ্চি বিরিয়ানি,মাছ (ব্রাঞ্চ) নেই।"
                return Result<BranchSearchDTO>.Failure("Branch not found"); 
            }

            // 3. audit log create(new data save so OldValue=null) 
            var auditDto = new AuditLogCreateDTO(
                BranchCode: branch.BranchCode,
                CreatedBy: UserID,
                UpdatedBy: (int?)null,
                ApprovedBy: (int?)null,
                TableName: "branches",
                Action: "READ",
                OldValue: $"{branch.VaultBalance}",
                NewValue: $"{branch.VaultBalance}",
                Description: "Branch Info. Searched by User."
            );

            var auditResult = await _auditLogService.CreateAuditLogAsync(auditDto);

            if (!auditResult.IsSuccess)
            {
                await transaction.RollbackAsync(); 
                return Result<BranchSearchDTO>.Failure("Audit log failed. Transaction rolled back.");
            }

            await transaction.CommitAsync();
            return Result<BranchSearchDTO>.Success(branch); // মাছ পেলে, সুন্দর বক্সে ভরে পাঠাচ্ছেন(Result<>,Wrapper Class)
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result<BranchSearchDTO>.Failure("Database Error"+ ex.Message);
        }

    }











    public async Task<Result<bool>> UpdateBranchAsync(BranchUpdateDTO udto, int currentUserId)
    {
        try
        {
            using var conn = await _dataSource.OpenConnectionAsync();
            using var transaction = await conn.BeginTransactionAsync();

            // 1. Fetch current branch data from database for validation and audit trail
            //const string selectSql = "SELECT * FROM branches WHERE id = @Id AND is_active = 1";


            const string selectSql = @"SELECT 
                                        id AS Id, 
                                        branch_name AS BranchName, 
                                        branch_code AS BranchCode, 
                                        vault_balance AS VaultBalance, 
                                        created_by AS CreatedBy, 
                                        updated_by AS UpdatedBy, 
                                        approved_by AS ApprovedBy, 
                                        row_version AS RowVersion
                                      FROM branches WHERE id = @Id AND is_active = 1";


            var branch = await conn.QueryFirstOrDefaultAsync<Branch>(selectSql, new { Id = udto.Id }, transaction);

            if (branch == null)
                return Result<bool>.Failure("Branch not found or is currently inactive.");

            // 2. Capture original values before modification to store in Audit Log
            string oldVaultBalance = branch.VaultBalance.ToString();
            string oldBranchCode = branch.BranchCode;
            string oldBranchName = branch.BranchName;

            // 3. Update entity information using Domain Methods
            branch.UpdateInfo(udto.BranchCode, udto.BranchName, currentUserId);

            // 4. Validate business rules (e.g., balance limits) before hitting the database
            var balanceResult = branch.UpdateVaultBalance(udto.VaultBalance);
            if (!balanceResult.IsSuccess)
            {
                return Result<bool>.Failure(balanceResult.Message);
            }

            // 5. Final Update Query including Optimistic Concurrency check (row_version)
            const string updateSql = @"UPDATE branches 
                                   SET branch_name = @BranchName, 
                                       branch_code = @BranchCode, 
                                       vault_balance = @VaultBalance, 
                                       updated_by = @UpdatedBy,
                                       row_version = row_version + 1 
                                   WHERE id = @Id AND row_version = @RowVersion";

            // Execute update using data from the modified Domain Entity
            var affected = await conn.ExecuteAsync(updateSql, new
            {
                branch.BranchName,
                branch.BranchCode,
                branch.VaultBalance,
                branch.UpdatedBy,
                branch.Id,
                RowVersion = udto.RowVersion // Client-side version to detect concurrent changes
            }, transaction);

            if (affected <= 0)
            {
                await transaction.RollbackAsync();
                return Result<bool>.Failure("Update failed. The record was modified by another user (Concurrency Conflict).");
            }

            // 6. Create Audit Log comparing Old Values with New Values
            var auditDto = new AuditLogCreateDTO(
                BranchCode: branch.BranchCode,
                CreatedBy: branch.CreatedBy,
                UpdatedBy: currentUserId,
                ApprovedBy: branch.ApprovedBy,
                TableName: "branches",
                Action: "UPDATE",
                OldValue: $"{oldVaultBalance}",
                NewValue: $"{branch.VaultBalance}",
                Description: "Branch information updated via user"
            );

            var auditResult = await _auditLogService.CreateAuditLogAsync(auditDto);

            // 7. Commit transaction only if both Update and Audit Log creation succeed
            if (auditResult.IsSuccess)
            {
                await transaction.CommitAsync();
                return Result<bool>.Success(true, "Branch updated successfully.");
            }

            // Rollback if audit logging fails
            await transaction.RollbackAsync();
            return Result<bool>.Failure("Update failed due to an error in the audit logging service.");

        }
        catch (Exception ex)
        {
            // Log the exception here (e.g., _logger.LogError(ex, "UpdateBranchAsync failed"))
            return Result<bool>.Failure("A database error occurred while processing your request.");
        }
    }


    //public async Task<Result<bool>> UpdateBranchAsync(BranchUpdateDTO dto)
    //{
    //    using var conn = await _dataSource.OpenConnectionAsync();
    //    const string sql = @"UPDATE Branches 
    //                        SET branch_name = @BranchName, 
    //                            branch_code = @BranchCode, 
    //                            vault_balance = @VaultBalance, 
    //                            UpdatedAt = GETDATE(), 
    //                            row_version = RowVersion + 1 
    //                         WHERE id = @Id AND row_version = @RowVersion";

    //    var affected = await conn.ExecuteAsync(sql, dto);
    //    return affected > 0
    //        ? Result<bool>.Success(true, "Branch Update Successful")
    //        : Result<bool>.Failure("Update failed (Concurrency conflict).");
    //}






}

