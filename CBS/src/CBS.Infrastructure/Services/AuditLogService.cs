using CBS.Application.DTO;
using CBS.Application.Interfaces;
using CBS.Domain.Common;
using Dapper;
using MySqlConnector;
using QuestPDF.Fluent;
using System.Text;

namespace CBS.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly MySqlDataSource _dataSource;

    public AuditLogService(MySqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }







    public async Task<Result<bool>> CreateAuditLogAsync(AuditLogCreateDTO dto)
    {
        try
        {
            using var connection = await _dataSource.OpenConnectionAsync();

            
            string sql = @"INSERT INTO audit_logs 
                          (branch_code, created_by, updated_by, approved_by, table_name, action, old_value, new_value, description, created_at) 
                          VALUES 
                          (@BranchCode, @CreatedBy, @UpdatedBy, @ApprovedBy, @TableName, @Action, @OldValue, @NewValue, @Description, NOW())";

            // Dapper auto mapping
            var result = await connection.ExecuteAsync(sql, dto);

            if (result > 0)
            {
                return Result<bool>.Success(true, "Audit log created successfully");
            }

            return Result<bool>.Failure("Failed to insert audit log");
        }
        catch (Exception ex)
        {
            // in Banking Logiin (Serilog/NLog) is must
            return Result<bool>.Failure("Database Error");
        }
    }








    public async Task<PagedResult<IEnumerable<AuditReportViewDTO>>> GetAuditReportAsync(AuditReportFilterDTO filter)
    {
        using var connection = await _dataSource.OpenConnectionAsync();

        var sqlBody = new StringBuilder(@"
                    FROM audit_logs al
                    WHERE 1=1 ");

        var parameters = new DynamicParameters();

        // ✅ BranchCode ফিল্টার (টেবিলে VARCHAR, DTO-তে string? - সরাসরি compare)
        if (!string.IsNullOrEmpty(filter.BranchCode))
        {
            sqlBody.Append(" AND al.branch_code = @BranchCode ");
            parameters.Add("BranchCode", filter.BranchCode);
        }

        if (filter.CreatedBy.HasValue)
        {
            sqlBody.Append(" AND al.created_by = @CreatedBy ");
            parameters.Add("CreatedBy", filter.CreatedBy);
        }

        //// UpdatedBy ফিল্টার (যদি filter-এ থাকে)
        //if (filter.UpdatedBy.HasValue)
        //{
        //    sqlBody.Append(" AND al.updated_by = @UpdatedBy ");
        //    parameters.Add("UpdatedBy", filter.UpdatedBy);
        //}

        //// ApprovedBy ফিল্টার (যদি filter-এ থাকে)
        //if (filter.ApprovedBy.HasValue)
        //{
        //    sqlBody.Append(" AND al.approved_by = @ApprovedBy ");
        //    parameters.Add("ApprovedBy", filter.ApprovedBy);
        //}

        if (filter.FromDate.HasValue && filter.ToDate.HasValue)
        {
            sqlBody.Append(" AND DATE(al.created_at) BETWEEN DATE(@FromDate) AND DATE(@ToDate) ");
            parameters.Add("FromDate", filter.FromDate);
            parameters.Add("ToDate", filter.ToDate);
        }

        // টোটাল রেকর্ড কাউন্ট
        string countSql = $"SELECT COUNT(*) {sqlBody}";
        int totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // ✅ this data will show in the view
        string selectSql = $@"
                            SELECT 
                                al.id,
                                al.branch_code as BranchCode, 
                                al.created_by as CreatedBy,                                
                                al.table_name as TableName,
                                al.action as Action,
                                al.old_value as OldValue,
                                al.new_value as NewValue,
                                al.description as Description,
                                al.created_at as CreatedAt
                            {sqlBody} 
                            ORDER BY al.id DESC 
                            LIMIT @Offset, @PageSize";

        parameters.Add("Offset", (filter.PageNumber - 1) * filter.PageSize);
        parameters.Add("PageSize", filter.PageSize);

        var data = await connection.QueryAsync<AuditReportViewDTO>(selectSql, parameters);

        return new PagedResult<IEnumerable<AuditReportViewDTO>>(data, totalRecords, filter.PageNumber, filter.PageSize);
    }



    public async Task<byte[]> GenerateAuditPdfAsync(AuditReportFilterDTO filter)
    {
        // পেজিনেশন এড়িয়ে সব ডাটা আনার জন্য PageSize বড় করে দিন
        filter = filter with { PageNumber = 1, PageSize = 10000 };
        var reportData = await GetAuditReportAsync(filter);

        var document = new AuditReportPDF(reportData.Items);
        return document.GeneratePdf();
    }









}