using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WesterUnionPD.Data;
using WesterUnionPD.Models;
using WesterUnionPD.Services;

namespace WesterUnionPD.Controllers;

/// <summary>
/// Handles CSV upload, job tracking, result view and Excel download.
/// Upload is done via AJAX to support client-side progress bar.
/// </summary>
public sealed class BranchChargeController : Controller
{
    private readonly AppDbContext _db;
    private readonly IUploadJobQueue _queue;
    private readonly IExcelExportService _excel;
    private readonly long _maxBytes;

    public BranchChargeController(
        AppDbContext db,
        IUploadJobQueue queue,
        IExcelExportService excel,
        IConfiguration cfg)
    {
        _db = db;
        _queue = queue;
        _excel = excel;

        // 1GB hard limit (from appsettings.json)
        _maxBytes = cfg.GetValue<long>("Upload:MaxUploadBytes");
    }

    // ---------------------------------------------------------
    // GET: Upload page
    // ---------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Upload()
    {
        // Last 10 jobs (newest first)
        var recentJobs = await _db.UploadJobs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedUtc)
            .Take(10)
            .ToListAsync();

        return View(recentJobs);
    }


    // ---------------------------------------------------------
    // POST (AJAX): Upload CSV with progress bar support
    // This endpoint is called via XMLHttpRequest from the View
    // ---------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAjax(IFormFile file, CancellationToken ct)
    {
        // Basic validations
        if (file == null || file.Length == 0)
            return BadRequest("File is missing.");

        if (!Path.GetExtension(file.FileName)
            .Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only CSV files are allowed.");

        if (file.Length > _maxBytes)
            return BadRequest("File size exceeds 1GB limit.");

        // Save CSV to protected temp directory (NOT wwwroot)
        var tempDir = Path.Combine(Path.GetTempPath(), "wupd_uploads");
        Directory.CreateDirectory(tempDir);

        var tempPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}.csv");

        await using (var fs = System.IO.File.Create(tempPath))
        {
            // Copy upload stream to disk
            await file.CopyToAsync(fs, ct);
        }

        // Create a new upload job
        var job = new UploadJob
        {
            Id = Guid.NewGuid(),
            Status = "Queued",
            CreatedUtc = DateTime.UtcNow,
            ProcessedRows = 0
        };

        _db.UploadJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        // Enqueue background processing (no timeout risk)
        await _queue.EnqueueAsync(job.Id, tempPath, ct);

        // Return JSON so JS can redirect to Result page
        return Json(new
        {
            jobId = job.Id,
            redirectUrl = Url.Action(nameof(Result), new { id = job.Id })
        });
    }

    // ---------------------------------------------------------
    // GET: Result page (shows status, progress, data)
    // ---------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Result(Guid id)
    {
        var job = await _db.UploadJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (job == null)
            return NotFound();

        var summaries = await _db.BranchSummaries
            .AsNoTracking()
            .Where(x => x.UploadJobId == id)
            .OrderBy(x => x.AbdCode)
            .ToListAsync();

        ViewBag.Job = job;
        return View(summaries);
    }

    // ---------------------------------------------------------
    // GET: Download aggregated result as Excel
    // ---------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Download(Guid id)
    {
        var job = await _db.UploadJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (job == null)
            return NotFound();

        if (!string.Equals(job.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Job is not completed yet.");

        var summaries = await _db.BranchSummaries
            .AsNoTracking()
            .Where(x => x.UploadJobId == id)
            .OrderBy(x => x.AbdCode)
            .ToListAsync();

        var excelBytes = _excel.CreateExcel(summaries);

        return File(
            excelBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BranchTotals_{id:N}.xlsx"
        );
    }
}
