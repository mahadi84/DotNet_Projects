
using CBS.Application.DTO;
using CBS.Application.Interfaces;
using CBS.Domain.Common;
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











    public async Task<Result<BranchResponseDTO>> CreateBranchAsync(BranchCreateDTO bcdto, int UserID)
    {
        using var connection = await _dataSource.OpenConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Duplicate Branch Check
            const string checkSql = "SELECT COUNT(1) FROM branches WHERE branch_code = @BranchCode;";
            var countBranchId = await connection.ExecuteScalarAsync<int>(checkSql, new { bcdto.BranchCode }, transaction);

            if (countBranchId > 0)
            {
                return Result<BranchResponseDTO>.Failure("Branch code already exists.");
            }

            // 2. Domain Entity Creation & Business Logic Validation
            // Using the static factory method to ensure all business rules (e.g., Min Balance 500) are met.
            Branch newBranch;
            try
            {
                newBranch = Branch.Create(bcdto.BranchCode, bcdto.BranchName, bcdto.VaultBalance, UserID);
            }
            catch (Exception ex)
            {
                // Catch validation errors thrown by the Domain Entity
                return Result<BranchResponseDTO>.Failure(ex.Message);
            }

            // 3. Data Insertion using Entity Properties
            const string insertSql = @"
            INSERT INTO branches (branch_code, branch_name, vault_balance, created_by, row_version, is_active, created_at) 
            VALUES (@BranchCode, @BranchName, @VaultBalance, @CreatedBy, @RowVersion, @IsActive, @CreatedAt);
            SELECT LAST_INSERT_ID();";

            // Map parameters directly from the validated Entity object
            var insertParameters = new
            {
                newBranch.BranchCode,
                newBranch.BranchName,
                newBranch.VaultBalance,
                newBranch.CreatedBy,
                newBranch.RowVersion,
                newBranch.IsActive,
                newBranch.CreatedAt
            };

            var newId = await connection.ExecuteScalarAsync<int>(insertSql, insertParameters, transaction);

            if (newId <= 0)
            {
                await transaction.RollbackAsync();
                return Result<BranchResponseDTO>.Failure("Failed! Branch data not saved.");
            }

            // 4. Audit Log Creation
            // Recording the 'CREATE' action with final validated values
            var auditDto = new AuditLogCreateDTO(
                BranchCode: newBranch.BranchCode,
                CreatedBy: UserID,
                UpdatedBy: null,
                ApprovedBy: null,
                TableName: "branches",
                Action: "CREATE",
                OldValue: "-",
                NewValue: $"Name: {newBranch.BranchName}, Branch_Code: {newBranch.BranchCode}, Balance: {newBranch.VaultBalance}, Status: {(newBranch.IsActive ? "Active" : "Inactive")}",
                Description: "New Branch created"
            );

            var auditResult = await _auditLogService.CreateAuditLogAsync(auditDto);

            if (!auditResult.IsSuccess)
            {
                // Rollback the entire transaction if the audit log fails
                await transaction.RollbackAsync();
                return Result<BranchResponseDTO>.Failure("Audit log failed. Transaction rolled back.");
            }

            // 5. Commit Transaction
            // Finalizing the changes in the database
            await transaction.CommitAsync();

            // 6. Return Response Data for the UI
            var response= new BranchResponseDTO(
                Id :newId,
                BranchName: newBranch.BranchName,
                BranchCode: newBranch.BranchCode,
                VaultBalance: newBranch.VaultBalance);
            return Result<BranchResponseDTO>.Success( response, "Branch created successfully.");
        }
        catch (Exception ex)
        {
            // Rollback on any unexpected database or system error
            await transaction.RollbackAsync();
            return Result<BranchResponseDTO>.Failure("Database Error: " + ex.Message);
        }
    }





    public async Task<Result<SearchBranchByCodeDTO>> SearchBranchByCodeAsync(string branchCode, int UserID)
    {
        using var conn = await _dataSource.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync(); // Banking transaction start

        try
        {

           const string sql = @"SELECT 
                        id AS Id, 
                        branch_code AS BranchCode, 
                        branch_name AS BranchName, 
                        vault_balance AS VaultBalance, 
                        row_version AS RowVersion,
                        is_active AS IsActive
                     FROM branches 
                     WHERE branch_code = @BranchCode";

            //<SearchBranchByCodeDTO>কি,কি অর্ডার (কাচ্চি বিরিয়ানি,মাছ)
            var branch = await conn.QueryFirstOrDefaultAsync<SearchBranchByCodeDTO>(sql, new { BranchCode = branchCode.Trim() }, transaction);

            if (branch == null)
            {
                return Result<SearchBranchByCodeDTO>.Failure("Branch not found"); 
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
                return Result<SearchBranchByCodeDTO>.Failure("Audit log failed. Transaction rolled back.");
            }

            await transaction.CommitAsync();
            return Result<SearchBranchByCodeDTO>.Success(branch); 
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result<SearchBranchByCodeDTO>.Failure("Database Error"+ ex.Message);
        }

    }



    public async Task<Result<bool>> UpdateBranchAsync(BranchUpdateDTO udto, int currentUserId)
    {
        using var conn = await _dataSource.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // 1. Fetch the current record from the database
            const string selectSql = @"SELECT 
                                          id AS Id,
                                          branch_name AS BranchName,
                                          branch_code AS BranchCode,
                                          vault_balance AS VaultBalance,
                                          created_by AS CreatedBy,
                                          approved_by AS ApprovedBy,
                                          row_version AS RowVersion,
                                          is_active AS IsActive
                                        FROM branches
                                        WHERE id = @Id;
                                        ";
            //<Branch> is because we need to use the Entity's methods later
            var branch = await conn.QueryFirstOrDefaultAsync<Branch>(selectSql, new { Id = udto.Id }, transaction);

            if (branch == null)  return Result<bool>.Failure("Branch not found.");


            // 2. Capture old values for Audit Logging before updating
            string oldData = $"Name: {branch.BranchName}, Code: {branch.BranchCode}, Balance: {branch.VaultBalance}, Status: {(branch.IsActive ? "Active" : "Inactive")}";

            // 3. Update the Domain Entity using its internal logic
            // The advantage: business rules only need to be updated in one place within the Entity.
            try
            {
                branch.UpdateGeneralInfo(udto.BranchCode, udto.BranchName, udto.VaultBalance, currentUserId);

                //enabling trigger extra actions (like notifications or balance checks) whenever a status changes, all from a single location in the Entity."
                if (udto.IsActive) branch.Activate(); else branch.Deactivate();
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message);
            }

            // 4. Final Update query with Optimistic Concurrency check
            const string updateSql = @"UPDATE branches 
                                   SET branch_name = @BranchName, 
                                       branch_code = @BranchCode, 
                                       vault_balance = @VaultBalance, 
                                       is_active = @IsActive,
                                       updated_by = @UpdatedBy,
                                       updated_at = @UpdatedAt,
                                       row_version = row_version + 1 
                                   WHERE id = @Id AND row_version = @RowVersion";

            var affected = await conn.ExecuteAsync(updateSql, new
            {
                branch.BranchName,
                branch.BranchCode,
                branch.VaultBalance,
                branch.IsActive,
                branch.UpdatedBy,
                branch.UpdatedAt,
                branch.Id,
                RowVersion = udto.RowVersion // Use the RowVersion from DTO to prevent concurrency issues
            }, transaction);

            // If affected is 0, it means the RowVersion has changed (someone else updated it)
            if (affected <= 0)
            {
                await transaction.RollbackAsync();
                return Result<bool>.Failure("Update failed. Data was modified by another user (Concurrency Error).");
            }

            // 5. Create Audit Log
            var auditDto = new AuditLogCreateDTO(
                BranchCode: branch.BranchCode, // latest branch code
                CreatedBy: branch.CreatedBy,
                UpdatedBy: currentUserId,
                ApprovedBy: branch.ApprovedBy,
                TableName: "branches",
                Action: "UPDATE",
                OldValue: oldData,
                NewValue: $"Name: {branch.BranchName}, Code: {branch.BranchCode}, Balance: {branch.VaultBalance}, Status: {(branch.IsActive ? "Active" : "Inactive")}",
                Description: "Branch information updated successfully"
            );

            var auditResult = await _auditLogService.CreateAuditLogAsync(auditDto);

            if (!auditResult.IsSuccess)
            {
                await transaction.RollbackAsync();
                return Result<bool>.Failure("Update failed due to audit logging error.");
            }

            // 6. Commit transaction if everything is successful
            await transaction.CommitAsync();
            return Result<bool>.Success(true, "Branch updated successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result<bool>.Failure("Database Error: " + ex.Message);
        }
    }





    public async Task<Result<IEnumerable<GetAllBranchNameAndCodeDTO>>> GetAllBranchNameAndCodeAsync()
    {
        using var conn = await _dataSource.OpenConnectionAsync();

        try
        {
            const string sql = @"SELECT 
                                id AS BranchId,
                                branch_code AS BranchCode, 
                                branch_name AS BranchName
                             FROM branches 
                             WHERE is_active = true"; 
                                   //AND is_active = true";

            var branches = await conn.QueryAsync<GetAllBranchNameAndCodeDTO>(sql);

            if (branches == null || !branches.Any())
            {
                return Result<IEnumerable<GetAllBranchNameAndCodeDTO>>.Failure("No Branch Found for dropdown");
            }

            return Result<IEnumerable<GetAllBranchNameAndCodeDTO>>.Success(branches, "Branches found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<GetAllBranchNameAndCodeDTO>>.Failure("Database Error: " + ex.Message);
        }
    }











}

