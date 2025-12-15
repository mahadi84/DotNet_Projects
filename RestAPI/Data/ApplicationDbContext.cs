using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestAPI.Models;

namespace RestAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // DbContext-এ আপনার টেবিল/মডেল যোগ করুন

        public DbSet<Emp> Emp { get; set; }
        public DbSet<User> User { get; set; }


        // Function to insert ডামি ইউজার ডেটা
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
                                              Id = 1,
                                              Username = "admin",
                                              Role = "Admin",
                                              // পাসওয়ার্ড হ্যাশ করে ঢোকানো হচ্ছে
                                              PasswordHash = hasher.HashPassword(null, "Admin@123")
                                          },
                                          new User
                                          {
                                              Id = 2,
                                              Username = "user",
                                              Role = "User",
                                              // পাসওয়ার্ড হ্যাশ করে ঢোকানো হচ্ছে
                                              PasswordHash = hasher.HashPassword(null, "User@123")
                                          }
                                        };

            // **৩. HasData মেথডের মাধ্যমে ডেটা সীড করা**
            modelBuilder.Entity<User>().HasData(usersToSeed);
        }













    }
}
