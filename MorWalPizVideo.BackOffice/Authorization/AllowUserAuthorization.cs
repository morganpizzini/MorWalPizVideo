using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Domain.Security;

namespace MorWalPizVideo.BackOffice.Authorization;

public enum AllowUserRequirementType
{
    Any,
    Group,
    Permission
}

public sealed record AllowUserToken(AllowUserRequirementType Type, string Value);

public sealed class AllowUserRequirement(IReadOnlyList<AllowUserToken> tokens) : IAuthorizationRequirement
{
    public IReadOnlyList<AllowUserToken> Tokens { get; } = tokens;
}

public sealed class AllowUserPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(AllowUserPolicyName.Prefix, StringComparison.Ordinal))
        {
            return base.GetPolicyAsync(policyName);
        }

        var rawTokens = policyName[AllowUserPolicyName.Prefix.Length..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var tokens = rawTokens
            .Select(ParseToken)
            .Where(token => !string.IsNullOrWhiteSpace(token.Value))
            .ToArray();

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new AllowUserRequirement(tokens))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    private static AllowUserToken ParseToken(string token)
    {
        var normalized = UserAccessResolver.Normalize(token);
        if (normalized.StartsWith("group:", StringComparison.Ordinal))
        {
            return new AllowUserToken(AllowUserRequirementType.Group, normalized["group:".Length..]);
        }

        if (normalized.StartsWith("perm:", StringComparison.Ordinal))
        {
            return new AllowUserToken(AllowUserRequirementType.Permission, normalized["perm:".Length..]);
        }

        return new AllowUserToken(AllowUserRequirementType.Any, normalized);
    }
}

public interface IUserAuthorizationEvaluator
{
    Task<bool> IsAuthorizedAsync(ClaimsPrincipal principal, IReadOnlyList<AllowUserToken> requiredTokens);
}

public sealed class UserAuthorizationEvaluator(IUserAccessResolver userAccessResolver) : IUserAuthorizationEvaluator
{
    public async Task<bool> IsAuthorizedAsync(ClaimsPrincipal principal, IReadOnlyList<AllowUserToken> requiredTokens)
    {
        if (requiredTokens.Count == 0)
        {
            return false;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await userAccessResolver.ResolveAsync(userId ?? string.Empty);

        var groupCodes = profile?.GroupCodes.ToHashSet(StringComparer.OrdinalIgnoreCase) ??
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var effectivePermissions = profile?.EffectivePermissions.ToHashSet(StringComparer.OrdinalIgnoreCase) ??
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in principal.FindAll(ClaimTypes.Role))
        {
            groupCodes.Add(UserAccessResolver.Normalize(role.Value));
        }

        foreach (var permissionClaim in principal.FindAll("permission"))
        {
            effectivePermissions.Add(UserAccessResolver.Normalize(permissionClaim.Value));
        }

        foreach (var permissionClaim in principal.FindAll("permissions"))
        {
            foreach (var value in permissionClaim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                effectivePermissions.Add(UserAccessResolver.Normalize(value));
            }
        }

        return requiredTokens.Any(token => IsTokenSatisfied(token, groupCodes, effectivePermissions));
    }

    private static bool IsTokenSatisfied(
        AllowUserToken token,
        IReadOnlySet<string> groups,
        IReadOnlySet<string> permissions)
    {
        var value = UserAccessResolver.Normalize(token.Value);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return token.Type switch
        {
            AllowUserRequirementType.Group => groups.Contains(value),
            AllowUserRequirementType.Permission => permissions.Contains(value),
            _ => groups.Contains(value) || permissions.Contains(value)
        };
    }
}

public sealed class AllowUserAuthorizationHandler(IUserAuthorizationEvaluator evaluator)
    : AuthorizationHandler<AllowUserRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AllowUserRequirement requirement)
    {
        if (await evaluator.IsAuthorizedAsync(context.User, requirement.Tokens))
        {
            context.Succeed(requirement);
        }
    }
}
