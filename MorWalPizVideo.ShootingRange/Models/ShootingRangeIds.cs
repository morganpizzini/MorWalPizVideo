using System.Security.Cryptography;

namespace MorWalPizVideo.ShootingRange.Models;

public static class ShootingRangeIds
{
    public static string NewId() => Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
}
