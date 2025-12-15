namespace RestAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; } // হ্যাশড পাসওয়ার্ড সংরক্ষণের জন্য
        public required string Role { get; set; }
    }
}
