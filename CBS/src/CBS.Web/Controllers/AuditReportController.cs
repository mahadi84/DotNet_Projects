using CBS.Application.DTO;
using CBS.Application.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CBS.Web.Controllers;

public class AuditReportController : Controller
{
    private readonly IAuditLogService _auditLogService;

    public AuditReportController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? branchCode, int? createdBy, int? updatedBy, int? approvedBy, DateTime? fromDate, DateTime? toDate, int pageNumber = 1, int pageSize = 10)
    {
        var filter = new AuditReportFilterDTO(
            BranchCode: branchCode,
            CreatedBy: createdBy,
            UpdatedBy: updatedBy,
            ApprovedBy: approvedBy,
            FromDate: fromDate,
            ToDate: toDate,
            PageNumber: pageNumber,
            PageSize: pageSize
        );

        var result = await _auditLogService.GetAuditReportAsync(filter);




        // এই ভ্যালুগুলো ভিউতে ইনপুট ফিল্ডে পুনরায় দেখানোর জন্য
        ViewData["BranchCode"] = branchCode;
        ViewData["UserId"]     = createdBy;
        ViewData["FromDate"]   = fromDate?.ToString("yyyy-MM-dd");
        ViewData["ToDate"]     = toDate?.ToString("yyyy-MM-dd");

        return View(result);
    }
}