namespace MorWalPizVideo.Domain.Scenarios;

public static class MockScenarioNames
{
    public const string Primary = "Primary";
    public const string Empty = "Empty";
    public const string Authorization = "Authorization";
    public const string ExternalFailure = "ExternalFailure";
    public const string LegacyCompatibility = "LegacyCompatibility";

    public static string Normalize(string? name) => name?.Trim() switch
    {
        null or "" => Primary,
        var value when value.Equals(Primary, StringComparison.OrdinalIgnoreCase) => Primary,
        var value when value.Equals(Empty, StringComparison.OrdinalIgnoreCase) => Empty,
        var value when value.Equals(Authorization, StringComparison.OrdinalIgnoreCase) => Authorization,
        var value when value.Equals(ExternalFailure, StringComparison.OrdinalIgnoreCase) => ExternalFailure,
        var value when value.Equals(LegacyCompatibility, StringComparison.OrdinalIgnoreCase) => LegacyCompatibility,
        _ => throw new ArgumentException($"Unknown mock scenario '{name}'.", nameof(name))
    };
}