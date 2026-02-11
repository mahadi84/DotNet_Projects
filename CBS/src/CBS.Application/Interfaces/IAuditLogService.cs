using CBS.Application.DTO;
using CBS.Domain.Common;
using System.Data.Common;

namespace CBS.Application.Interfaces;

public interface IAuditLogService
{
    Task<Result<bool>> CreateAuditLogAsync(AuditLogCreateDTO dto, DbConnection? connection = null,  DbTransaction? transaction = null);

    Task<PagedResult<IEnumerable<AuditReportViewDTO>>> GetAuditReportAsync(AuditReportFilterDTO filter);
    Task<byte[]> GenerateAuditPdfAsync(AuditReportFilterDTO filter);

}