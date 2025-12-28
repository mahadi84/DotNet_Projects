
using SwiftXmlToExcel.Models;
using Microsoft.EntityFrameworkCore;

namespace SwiftXmlToExcel.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}
