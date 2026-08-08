using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using MorWalPizVideo.BackOffice.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Authentication;

public sealed class ImpersonationClaimsTransformation(
    IHttpContextAccessor httpContextAccessor,
    IImpersonationService impersonationService) : IClaimsTransformation
{
    public static string GetEffectiveUserId(ClaimsPrincipal principal)
        => principal.FindFirstValue("target_user_id") ??
           principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.HasClaim("impersonation", "true"))
        {
            return principal;
        }

        if (principal.HasClaim(claim => claim.Type.Equals("ApiKeyId", StringComparison.OrdinalIgnoreCase)) ||
            principal.Identities.Any(identity => identity.AuthenticationType?.Equals("ApiKey", StringComparison.OrdinalIgnoreCase) == true))
        {
            return principal;
        }

        var httpContext = httpContextAccessor.HttpContext;
        var sessionToken = httpContext?.Request.Cookies[ImpersonationCookieNames.Session];
        var actorUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(sessionToken) || string.IsNullOrWhiteSpace(actorUserId))
        {
            return principal;
        }

        var sessionContext = await impersonationService.ResolveSessionAsync(sessionToken, actorUserId);
        if (sessionContext is null)
        {
            return principal;
        }

        var target = sessionContext.TargetProfile.User;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sessionContext.Session.TargetUserId),
            new(ClaimTypes.Name, target.Username),
            new(ClaimTypes.Email, target.Email),
            new("actor_user_id", sessionContext.Session.ActorUserId),
            new("target_user_id", sessionContext.Session.TargetUserId),
            new("impersonation_session_id", sessionContext.Session.Id),
            new("impersonation", "true")
        };

        var transformedPrincipal = new ClaimsPrincipal(principal);
        transformedPrincipal.AddIdentity(new ClaimsIdentity(claims, "Impersonation"));
        return transformedPrincipal;
    }
}

public static class ImpersonationCookieNames
{
    public const string Session = "impersonation_token";
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class BlockImpersonationAttribute : Attribute;

public sealed class ImpersonationHardBlockMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.HasClaim("impersonation", "true") &&
            context.GetEndpoint()?.Metadata.GetMetadata<BlockImpersonationAttribute>() is not null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "This operation is unavailable during impersonation." });
            return;
        }

        await next(context);
    }
}