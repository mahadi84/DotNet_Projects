using CBS.Application.DTO;
using CBS.Domain.Common;

namespace CBS.Application.Interfaces;

public interface IAuditLogService
{
    Task<Result<bool>> CreateAuditLogAsync(AuditLogCreateDTO dto);

    Task<PagedResult<IEnumerable<AuditReportViewDTO>>> GetAuditReportAsync(AuditReportFilterDTO filter);

}