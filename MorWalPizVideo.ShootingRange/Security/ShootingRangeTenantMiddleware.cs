using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Services;

namespace MorWalPizVideo.ShootingRange.Security;

public sealed class ShootingRangeTenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IShootingRangeRepositorySet repositories)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api/shooting-range") ||
            path.StartsWithSegments("/api/shooting-range/management") ||
            path.StartsWithSegments("/swagger") ||
            path.StartsWithSegments("/openapi"))
        {
            await next(context);
            return;
        }

        var header = context.Request.Headers["X-Shooting-Range"];
        if (header.Count != 1 || !ShootingRangeTenantProvisioningService.IsValidSlug(header[0]))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = "A valid shooting range tenant header is required." }, context.RequestAborted);
            return;
        }

        var slug = header[0]!.Trim().ToLowerInvariant();
        var tenant = (await repositories.Tenants.GetAllAsync(context.RequestAborted))
            .SingleOrDefault(item => item.Slug == slug && item.IsActive);
        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { message = "Shooting range is unavailable." }, context.RequestAborted);
            return;
        }

        if (context.Items.TryGetValue(ShootingRangeCorsMiddleware.OriginTenantItemKey, out var originTenantValue) &&
            originTenantValue is not null && !string.Equals((string)originTenantValue, tenant.Id, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "The request origin does not belong to this shooting range." }, context.RequestAborted);
            return;
        }

        using (repositories.TenantContext.BeginScope(tenant.Id))
            await next(context);
    }
}
