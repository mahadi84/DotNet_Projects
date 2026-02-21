using CBS.Application.DTO;
using CBS.Application.Interfaces;
using CBS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CBS.Web.Controllers;


[Authorize(Roles = "HoSuperAdmin,Maker")]
public class AuditReportController : Controller
{
    private readonly IAuditLogService _auditLogService;

    public AuditReportController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }





    [HttpGet]
    public async Task<IActionResult> Index(string ? branchCode, int? userId, DateTime? fromDate, DateTime? toDate, int pageNumber = 1)
    {
        try
        {
            var filter = new AuditReportFilterDTO(
                BranchCode: branchCode,
                CreatedBy: userId,
                FromDate: fromDate,
                ToDate: toDate,
                PageNumber: pageNumber, // Use the parameter here
                PageSize: 10
            );

            var result = await _auditLogService.GetAuditReportAsync(filter);

            // Keep filter values for the UI
            ViewData["BranchCode"] = branchCode;
            ViewData["UserId"] = userId;
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

            return View(result);
        }
        catch (Exception ex)
        {

            TempData["Error"] = ex.Message;

            // Return an empty model so the View doesn't crash trying to loop through null data
            return View(new PagedResult<IEnumerable<AuditReportViewDTO>>(
                Enumerable.Empty<AuditReportViewDTO>(), 0, 1, 10));
        }
    }


    [HttpGet]
    public async Task<IActionResult> DownloadPdf(string? branchCode, int? userId, DateTime? fromDate, DateTime? toDate)
    {
        var filter = new AuditReportFilterDTO(
            BranchCode: branchCode,
            CreatedBy: userId,
            FromDate: fromDate,
            ToDate: toDate,
            PageNumber: 1,
            PageSize: 10000
        );

        byte[] pdfBytes = await _auditLogService.GenerateAuditPdfAsync(filter);
        string fileName = $"AuditReport_{DateTime.Now:yyyyMMdd}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }













}