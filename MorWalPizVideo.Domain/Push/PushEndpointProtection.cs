using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.Domain.Push;

/// <summary>
/// Hashing and redaction helpers for Web Push endpoints and anonymous management credentials.
/// Push endpoints are bearer-capable URLs: they must never reach logs, API responses or BackOffice UI verbatim.
/// </summary>
public static class PushEndpointProtection
{
    /// <summary>Length of the stable, non-reversible fragment surfaced to operators.</summary>
    private const int FingerprintLength = 8;

    /// <summary>Stable SHA-256 hex hash used as the deduplication key for an endpoint.</summary>
    public static string HashEndpoint(string endpoint) => Hash(endpoint.Trim());

    /// <summary>Stable SHA-256 hex hash of an anonymous management credential. Only the hash is persisted.</summary>
    public static string HashCredential(string credential) => Hash(credential.Trim());

    /// <summary>Generates a 256-bit anonymous management credential. Returned once, stored only as a hash.</summary>
    public static string CreateCredential() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Produces a log- and UI-safe representation of a push endpoint: the push service host plus a short
    /// hash fingerprint. The opaque subscription token is never included.
    /// </summary>
    public static string Redact(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) return "(none)";

        var fingerprint = HashEndpoint(endpoint)[..FingerprintLength].ToLowerInvariant();
        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            ? $"{uri.Host}/#{fingerprint}"
            : $"(opaque)/#{fingerprint}";
    }

    /// <summary>Redacts an endpoint when only its hash is available.</summary>
    public static string RedactHash(string? endpointHash) =>
        string.IsNullOrWhiteSpace(endpointHash) ? "(none)" : $"(hash)/#{endpointHash[..Math.Min(FingerprintLength, endpointHash.Length)].ToLowerInvariant()}";

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    /// <summary>Constant-time comparison of two hex hashes.</summary>
    public static bool HashesMatch(string? left, string? right) =>
        !string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(right) &&
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}
