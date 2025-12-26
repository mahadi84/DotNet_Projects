namespace OnlineBanking.Security;

public static class PasswordHasher
{
    // HashPassword uses salt internally (BCrypt)
    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    // Verify compares plaintext with stored hash
    public static bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
