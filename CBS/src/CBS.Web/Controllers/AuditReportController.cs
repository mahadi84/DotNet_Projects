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
    // Show at first load(with pagination) than
    // Search(with branchCode, CreatedBy, from DateTime to DateTime)+Show (with pagination)
    public async Task<IActionResult> Index(string? branchCode, int? userId, DateTime? fromDate, DateTime? toDate)
    {
        var filter = new AuditReportFilterDTO(
            BranchCode: branchCode,
            CreatedBy: userId,
            FromDate: fromDate,
            ToDate: toDate,
            PageNumber: 1,
            PageSize: 10
        );

        var result = await _auditLogService.GetAuditReportAsync(filter);




        // এই ভ্যালুগুলো ভিউতে ইনপুট ফিল্ডে পুনরায় দেখানোর জন্য
        ViewData["BranchCode"] = branchCode;
        ViewData["UserId"]     = userId;
        ViewData["FromDate"]   = fromDate?.ToString("yyyy-MM-dd");
        ViewData["ToDate"]     = toDate?.ToString("yyyy-MM-dd");

        return View(result);
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