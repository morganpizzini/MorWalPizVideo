using System.Security.Cryptography;

namespace MorWalPizVideo.ShootingRange.Security;

public static class PasswordHashing
{
    private const int SaltSizeBytes = 32;
    private const int Iterations = 100000;
    private const int HashSizeBytes = 32;
    private const int LegacyHashSizeBytes = 256;

    public static string HashPassword(string password, out string salt)
    {
        using var rng = RandomNumberGenerator.Create();
        var saltBytes = new byte[SaltSizeBytes];
        rng.GetBytes(saltBytes);
        salt = Convert.ToBase64String(saltBytes);

        return HashPassword(password, saltBytes);
    }

    public static bool VerifyPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt)) return false;

        try
        {
            var hashBytes = Convert.FromBase64String(hash);
            var saltBytes = Convert.FromBase64String(salt);
            var testHash = HashPassword(password, saltBytes, hashBytes.Length);
            return CryptographicOperations.FixedTimeEquals(hashBytes, Convert.FromBase64String(testHash));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string HashPassword(string password, byte[] saltBytes) => HashPassword(password, saltBytes, HashSizeBytes);

    private static string HashPassword(string password, byte[] saltBytes, int hashSizeBytes)
    {
        var resolvedHashSize = hashSizeBytes == LegacyHashSizeBytes ? LegacyHashSizeBytes : HashSizeBytes;
        return Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, resolvedHashSize));
    }
}