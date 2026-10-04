namespace MorWalPizVideo.Domain.Push;

/// <summary>
/// Validates notification destinations. Only same-origin relative paths are accepted so a notification can
/// never be used to send a subscriber to an attacker-controlled origin.
/// </summary>
public static class PushDestination
{
    public const string Default = "/";

    /// <summary>
    /// Normalizes a destination to a rooted, same-origin relative path.
    /// Returns null when the value is absolute, protocol-relative, or otherwise not same-origin.
    /// </summary>
    public static string? Normalize(string? destination)
    {
        if (string.IsNullOrWhiteSpace(destination)) return Default;

        var value = destination.Trim();
        if (value.StartsWith("//", StringComparison.Ordinal)) return null;
        if (Uri.IsWellFormedUriString(value, UriKind.Absolute)) return null;
        if (value.Contains("://", StringComparison.Ordinal)) return null;
        if (value.StartsWith('#')) return null;
        if (value.Contains("..", StringComparison.Ordinal)) return null;
        if (value.Any(char.IsControl)) return null;

        if (!value.StartsWith('/')) value = "/" + value;
        return value;
    }
}
