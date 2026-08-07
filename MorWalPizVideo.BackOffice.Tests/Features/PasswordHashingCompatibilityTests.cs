using System.Security.Cryptography;
using MorWalPizVideo.Domain.Security;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public class PasswordHashingCompatibilityTests
{
    [Fact]
    public void Hash_and_verify_use_current_pbkdf2_profile()
    {
        var hash = PasswordHashing.HashPassword("Secret123!", out var salt);

        Assert.True(PasswordHashing.VerifyPassword("Secret123!", hash, salt));
        Assert.False(PasswordHashing.VerifyPassword("Wrong123!", hash, salt));
    }

    [Fact]
    public void Verify_supports_legacy_256_byte_pbkdf2_hashes()
    {
        var saltBytes = RandomNumberGenerator.GetBytes(32);
        var salt = Convert.ToBase64String(saltBytes);
        var legacyHash = Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(
            "LegacyPass123!",
            saltBytes,
            100000,
            HashAlgorithmName.SHA256,
            256));

        Assert.True(PasswordHashing.VerifyPassword("LegacyPass123!", legacyHash, salt));
        Assert.False(PasswordHashing.VerifyPassword("WrongLegacy!", legacyHash, salt));
    }

    [Fact]
    public void Verify_handles_invalid_hash_and_salt_values()
    {
        Assert.False(PasswordHashing.VerifyPassword("Secret123!", "not-base64", "also-not-base64"));
    }
}