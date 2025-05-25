using Microsoft.AspNetCore.Identity;

public static class PasswordHelper
{
    private static readonly IPasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

    public static string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(null, password);
    }

    public static bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
