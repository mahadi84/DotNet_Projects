using Microsoft.EntityFrameworkCore;
using WesterUnionPD.Models;

namespace WesterUnionPD.Data;

/// <summary>
/// Database context – stores ONLY aggregated data.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UploadJob> UploadJobs => Set<UploadJob>();
    public DbSet<BranchSummary> BranchSummaries => Set<BranchSummary>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
}
