using Microsoft.AspNetCore.Authorization;

namespace MorWalPizVideo.BackOffice.Authorization;

public sealed class AllowUserAttribute : AuthorizeAttribute
{
    public AllowUserAttribute(params string[] requirements)
    {
        Policy = AllowUserPolicyName.Build(requirements);
    }
}

public static class AllowUserPolicyName
{
    public const string Prefix = "AllowUser:";

    public static string Build(IEnumerable<string> requirements)
    {
        var sanitized = requirements
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray();

        return $"{Prefix}{string.Join(',', sanitized)}";
    }
}
