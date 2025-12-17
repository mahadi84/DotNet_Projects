namespace RestAPI.DTOs
{
    public class LoginDto
    {
        // ক্লায়েন্ট থেকে আসা ইউজারনেম (Username)
        public required string Username { get; set; }

        // ক্লায়েন্ট থেকে আসা প্লেন টেক্সট পাসওয়ার্ড (Plain Text Password)
        public required string Password { get; set; }
    }
}