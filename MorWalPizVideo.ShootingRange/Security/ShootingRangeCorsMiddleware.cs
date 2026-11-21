using MorWalPizVideo.ShootingRange.Repositories;

namespace MorWalPizVideo.ShootingRange.Security;

public sealed class ShootingRangeCorsMiddleware(RequestDelegate next)
{
    internal const string OriginTenantItemKey = "ShootingRange.CorsOriginTenantId";
    private static readonly HashSet<string> AllowedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "content-type",
        "x-csrf-token",
        "x-shooting-range"
    };
    private static readonly HashSet<string> AllowedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET",
        "POST",
        "PUT",
        "DELETE"
    };

    public async Task InvokeAsync(HttpContext context, IShootingRangeRepositorySet repositories)
    {
        if (!context.Request.Path.StartsWithSegments("/api/shooting-range") ||
            context.Request.Path.StartsWithSegments("/api/shooting-range/management") ||
            !context.Request.Headers.TryGetValue("Origin", out var originValues))
        {
            await next(context);
            return;
        }

        if (originValues.Count != 1 || !Uri.TryCreate(originValues[0], UriKind.Absolute, out var originUri) ||
            originUri.AbsolutePath != "/" || originUri.Query.Length != 0 || originUri.Fragment.Length != 0)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var origin = originUri.GetLeftPart(UriPartial.Authority);
        if (!string.Equals(originValues[0], origin, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        var tenants = await repositories.Tenants.GetAllAsync(context.RequestAborted);
        var matchingTenants = tenants.Where(item => item.IsActive && item.ClientOrigin == origin).ToArray();
        if (matchingTenants.Length != 1)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        context.Items[OriginTenantItemKey] = matchingTenants[0].Id;
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            if (!TryAddPreflightHeaders(context, origin))
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
            else
                context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.AccessControlAllowOrigin = origin;
            context.Response.Headers.AccessControlAllowCredentials = "true";
            context.Response.Headers.AppendCommaSeparatedValues("Vary", "Origin");
            return Task.CompletedTask;
        });
        await next(context);
    }

    private static bool TryAddPreflightHeaders(HttpContext context, string origin)
    {
        var requestedMethod = context.Request.Headers.AccessControlRequestMethod.ToString();
        if (!AllowedMethods.Contains(requestedMethod)) return false;

        var requestedHeaders = context.Request.Headers.AccessControlRequestHeaders
            .ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (requestedHeaders.Any(header => !AllowedHeaders.Contains(header))) return false;

        context.Response.Headers.AccessControlAllowOrigin = origin;
        context.Response.Headers.AccessControlAllowCredentials = "true";
        context.Response.Headers.AccessControlAllowMethods = string.Join(", ", AllowedMethods);
        context.Response.Headers.AccessControlAllowHeaders = string.Join(", ", AllowedHeaders);
        context.Response.Headers.AppendCommaSeparatedValues("Vary", "Origin", "Access-Control-Request-Method", "Access-Control-Request-Headers");
        return true;
    }
}
