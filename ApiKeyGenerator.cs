using System.Security.Cryptography;

public static class ApiKeyGenerator
{
    private const int ApiKeyByteLength = 32;

    public static string GenerateApiKey()
    {
        byte[] randomNumber = new byte[ApiKeyByteLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
