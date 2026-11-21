using System.Text.RegularExpressions;
using MorWalPizVideo.ShootingRange.Contracts;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;

namespace MorWalPizVideo.ShootingRange.Services;

public sealed class ShootingRangeTenantProvisioningService(
    IShootingRangeRepositorySet repositories,
    ILogger<ShootingRangeTenantProvisioningService> logger)
{
    private static readonly Regex SlugPattern = new("^[a-z0-9](?:[a-z0-9-]{1,62}[a-z0-9])$", RegexOptions.CultureInvariant);

    public static bool IsValidSlug(string? slug) =>
        !string.IsNullOrWhiteSpace(slug) && SlugPattern.IsMatch(slug);

    public static bool TryNormalizeOrigin(string? value, out string origin)
    {
        origin = string.Empty;
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            uri.AbsolutePath != "/" ||
            uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))
            return false;

        origin = uri.GetLeftPart(UriPartial.Authority);
        return origin.Length <= 300;
    }

    public Task<ShootingRangeTenantDto> CreateAsync(
        CreateShootingRangeTenantRequest request,
        CancellationToken cancellationToken) =>
        repositories.ExecuteGuardedAsync(async () =>
        {
            var slug = request.Slug.Trim().ToLowerInvariant();
            if (!IsValidSlug(slug) || !TryNormalizeOrigin(request.ClientOrigin, out var origin))
                throw new ArgumentException("Tenant slug or client origin is invalid.");

            if ((await repositories.Tenants.GetAllAsync(cancellationToken)).Any(tenant =>
                tenant.Slug == slug || tenant.ClientOrigin == origin))
                throw new InvalidOperationException("Tenant slug or client origin already exists.");

            var tenant = await repositories.Tenants.InsertAsync(new ShootingRangeTenant
            {
                Id = ShootingRangeIds.NewId(),
                Slug = slug,
                Name = request.Name.Trim(),
                ClientOrigin = origin
            }, cancellationToken);

            using (repositories.TenantContext.BeginScope(tenant.Id))
            {
                await repositories.Configs.InsertAsync(new ShootingRangeConfig
                {
                    Id = ShootingRangeIds.NewId()
                }, cancellationToken);
            }

            logger.LogInformation("Provisioned Shooting Range tenant {TenantId} with slug {TenantSlug}.", tenant.Id, tenant.Slug);
            return new ShootingRangeTenantDto(tenant.Id, tenant.Slug, tenant.Name, tenant.ClientOrigin, tenant.IsActive);
        }, cancellationToken);
}
