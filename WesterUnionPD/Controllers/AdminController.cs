using Microsoft.AspNetCore.Mvc;
using WesterUnionPD.Data;
using WesterUnionPD.Services;

namespace WesterUnionPD.Controllers;

/// <summary>
/// Dangerous admin operations.
/// </summary>
public sealed class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;

    public AdminController(AppDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteJob(Guid jobId)
    {
        // Find the job
        var job = await _db.UploadJobs.FindAsync(jobId);
        if (job == null)
            return NotFound();

        // Delete ONLY this job's data
        var summaries = _db.BranchSummaries
            .Where(x => x.UploadJobId == jobId);

        _db.BranchSummaries.RemoveRange(summaries);
        _db.UploadJobs.Remove(job);

        await _db.SaveChangesAsync();

        // Go back to Upload page (job list refresh হবে)
        return RedirectToAction("Upload", "BranchCharge");
    }







    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAll(string password)
    {
        if (password != _cfg["Admin:DeletePassword"])
            return BadRequest("Invalid password.");

        _db.BranchSummaries.RemoveRange(_db.BranchSummaries);
        _db.UploadJobs.RemoveRange(_db.UploadJobs);
        _db.AuditLogs.RemoveRange(_db.AuditLogs);

        await _db.SaveChangesAsync();

        return RedirectToAction("Upload", "BranchCharge");
    }
}
