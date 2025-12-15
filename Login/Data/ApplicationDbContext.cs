using Login.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Login.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // DbContext-এ আপনার টেবিল/মডেল যোগ করুন
        public DbSet<User> user { get; set; }
        public DbSet<Emp> emp { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // **১. পাসওয়ার্ড হ্যাশার তৈরি**
            // TUser টাইপের জন্য User ক্লাসটি ব্যবহার করা হয়েছে
            var hasher = new PasswordHasher<User>();

            // **২. ডামি ইউজার ডেটা তৈরি**
            var usersToSeed = new List<User>
        {
            new User
            {
               Id=1, //Will auto increment, No Id will show error on migration
                Username = "admin",
                Role = "Admin",
                // পাসওয়ার্ড হ্যাশ করে ঢোকানো হচ্ছে
                PasswordHash = hasher.HashPassword(null, "Admin@123")
            },
            new User
            {
                Id=2,
                Username = "testuser",
                Role = "User",
                // পাসওয়ার্ড হ্যাশ করে ঢোকানো হচ্ছে
                PasswordHash = hasher.HashPassword(null, "User@456")
            }
        };

            // **৩. HasData মেথডের মাধ্যমে ডেটা সীড করা**
            modelBuilder.Entity<User>().HasData(usersToSeed);
        }
    }
}
