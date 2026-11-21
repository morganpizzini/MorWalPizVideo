using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Security;
using MorWalPizVideo.ShootingRange.Services;

namespace MorWalPizVideo.ShootingRange.Repositories;

public interface IShootingRangeRepository<T> where T : BaseEntity
{
    Task<T?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> InsertAsync(T item, CancellationToken cancellationToken = default);
    Task<T> ReplaceAsync(T item, CancellationToken cancellationToken = default);
}

public interface IShootingRangeUserRepository : IShootingRangeRepository<ShootingRangeUser>
{
    Task<ShootingRangeUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
}

public interface IShootingRangeBookingRepository : IShootingRangeRepository<ShootingRangeBooking>
{
    Task<bool> HasOverlapAsync(string bayId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
    Task<ShootingRangeBooking> InsertIfAvailableAsync(ShootingRangeBooking booking, CancellationToken cancellationToken = default);
}

public interface IShootingRangeRepositorySet
{
    TimeProvider Clock { get; }
    ShootingRangeTenantContext TenantContext { get; }
    Task<T> ExecuteGuardedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
    IShootingRangeRepository<ShootingRangeTenant> Tenants { get; }
    IShootingRangeRepository<ShootingRangeConfig> Configs { get; }
    IShootingRangeRepository<ShootingRangeBay> Bays { get; }
    IShootingRangeRepository<ShootingRangeMechanismType> MechanismTypes { get; }
    IShootingRangeRepository<ShootingRangeException> Exceptions { get; }
    IShootingRangeUserRepository Users { get; }
    IShootingRangeBookingRepository Bookings { get; }
    IShootingRangeRepository<ShootingRangeThread> Threads { get; }
    IShootingRangeRepository<ShootingRangeLocalSession> LocalSessions { get; }
}

internal static class ShootingRangeWriteValidation
{
    internal static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

    internal static T Prepare<T>(T item, string? tenantId) where T : BaseEntity
    {
        if (item is ShootingRangeTenant) return item;
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new InvalidOperationException("A resolved shooting range tenant is required for data writes.");

        var owned = item switch
        {
            ShootingRangeConfig config => (BaseEntity)(config with { TenantId = tenantId }),
            ShootingRangeBay bay => bay with { TenantId = tenantId },
            ShootingRangeMechanismType mechanismType => mechanismType with
            {
                TenantId = tenantId,
                NormalizedName = mechanismType.Name.Trim().ToLowerInvariant()
            },
            ShootingRangeException exception => exception with { TenantId = tenantId },
            ShootingRangeUser user => user with
            {
                TenantId = tenantId,
                NormalizedUsername = NormalizeUsername(user.Username)
            },
            ShootingRangeLocalSession session => session with { TenantId = tenantId },
            ShootingRangeBooking booking => booking with { TenantId = tenantId },
            ShootingRangeThread thread => thread with { TenantId = tenantId },
            _ => throw new InvalidOperationException($"Unsupported tenant-owned record type {typeof(T).Name}.")
        };
        return (T)owned;
    }

    internal static void ValidateUsernamePreflight(IReadOnlyList<ShootingRangeUser> users)
    {
        if (users.Any(user => string.IsNullOrWhiteSpace(user.Username)) || users.GroupBy(user => NormalizeUsername(user.Username), StringComparer.Ordinal).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Normalized username preflight failed. Review duplicate or empty usernames before release; no accounts were modified.");
        if (users.Any(user => user.NormalizedUsername != NormalizeUsername(user.Username)))
            throw new InvalidOperationException("Normalized usernames require an approved additive backfill before index creation; no accounts were modified.");
    }

    internal static async Task ValidateAsync<T>(IShootingRangeRepositorySet repositories, T item, bool replacing, CancellationToken cancellationToken) where T : BaseEntity
    {
        if (item is ShootingRangeTenant tenant)
        {
            var slug = NormalizeTenantSlug(tenant.Slug);
            if (string.IsNullOrWhiteSpace(slug) || slug.Length > 64 || slug != tenant.Slug ||
                string.IsNullOrWhiteSpace(tenant.Name) || tenant.Name.Length > 200 ||
                !ShootingRangeTenantProvisioningService.TryNormalizeOrigin(tenant.ClientOrigin, out var origin) ||
                origin != tenant.ClientOrigin)
                throw new ArgumentException("Invalid shooting range tenant.");
            if ((await repositories.Tenants.GetAllAsync(cancellationToken)).Any(existing =>
                existing.Id != tenant.Id && (existing.Slug == tenant.Slug || existing.ClientOrigin == tenant.ClientOrigin)))
                throw new InvalidOperationException("Shooting range tenant slug or origin already exists.");
            return;
        }

        var tenantId = repositories.TenantContext.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId) || item.TenantId != tenantId ||
            await repositories.Tenants.GetAsync(tenantId, cancellationToken) is not { IsActive: true })
            throw new InvalidOperationException("An active shooting range tenant is required for data writes.");

        if (item is ShootingRangeConfig config) Services.ShootingRangeService.ValidateConfig(config);
        if (item is ShootingRangeBay bay)
        {
            static bool InvalidDimension(decimal? value) =>
                value.HasValue && (value.Value <= 0 || decimal.Round(value.Value, 2) != value.Value);
            if (!Enum.IsDefined(bay.Status) || string.IsNullOrWhiteSpace(bay.Code) || bay.Targets < 0 || bay.Partitions < 0 || bay.Plates < 0 || bay.Pepper < 0 ||
                bay.Height.HasValue != bay.Width.HasValue || InvalidDimension(bay.Height) || InvalidDimension(bay.Width) ||
                bay.Mechanisms is null || bay.WhitelistUserIds is null ||
                bay.Mechanisms.Any(mechanism => mechanism is null || string.IsNullOrWhiteSpace(mechanism.Type) || mechanism.Quantity <= 0) ||
                bay.Mechanisms.Select(mechanism => mechanism.Type.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != bay.Mechanisms.Count)
                throw new ArgumentException("Invalid bay.");
            var mechanismTypes = await repositories.MechanismTypes.GetAllAsync(cancellationToken);
            if (bay.Mechanisms.Any(mechanism => mechanism is null || !mechanismTypes.Any(type => string.Equals(type.Name, mechanism.Type, StringComparison.Ordinal))))
                throw new ArgumentException("A bay uses an unknown mechanism type.");
            foreach (var userId in bay.WhitelistUserIds)
                if (await repositories.Users.GetAsync(userId, cancellationToken) is null) throw new ArgumentException("Whitelist account not found.");
        }
        if (item is ShootingRangeMechanismType mechanismType)
        {
            var normalizedName = mechanismType.Name.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName) || normalizedName.Length > 80 || mechanismType.Name != normalizedName)
                throw new ArgumentException("Invalid mechanism type name.");
            if (replacing && await repositories.MechanismTypes.GetAsync(mechanismType.Id, cancellationToken) is { } existingType && existingType.Name != mechanismType.Name)
                throw new InvalidOperationException("Mechanism type names cannot be changed.");
            if ((await repositories.MechanismTypes.GetAllAsync(cancellationToken)).Any(existing =>
                existing.Id != mechanismType.Id &&
                existing.NormalizedName == normalizedName.ToLowerInvariant()))
                throw new InvalidOperationException("Mechanism type already exists.");
        }
        if (item is ShootingRangeUser user)
        {
            if (!Enum.IsDefined(user.Status) || string.IsNullOrWhiteSpace(user.Username)) throw new ArgumentException("Invalid account status or username.");
            if ((await repositories.Users.GetAllAsync(cancellationToken)).Any(existing => existing.Id != user.Id && NormalizeUsername(existing.Username) == user.NormalizedUsername))
                throw new InvalidOperationException("Username already exists.");
        }
        if (item is ShootingRangeThread thread)
        {
            if (await repositories.Users.GetAsync(thread.UserId, cancellationToken) is null)
                throw new ArgumentException("Message users must belong to this shooting range.");
            foreach (var message in thread.Messages)
                if (await repositories.Users.GetAsync(message.AuthorUserId, cancellationToken) is null)
                    throw new ArgumentException("Message users must belong to this shooting range.");
        }
        if (item is ShootingRangeException closure && closure.BayId is not null && await repositories.Bays.GetAsync(closure.BayId, cancellationToken) is null) throw new KeyNotFoundException("Bay not found.");
        if (item is ShootingRangeLocalSession session &&
            await repositories.Users.GetAsync(session.UserId, cancellationToken) is null)
            throw new ArgumentException("Session user must belong to this shooting range.");
        if (item is not ShootingRangeBooking booking) return;
        if (!Enum.IsDefined(booking.Status) || booking.StartUtc.Kind != DateTimeKind.Utc || booking.EndUtc.Kind != DateTimeKind.Utc || booking.StartUtc >= booking.EndUtc) throw new ArgumentException("Invalid booking interval or status.");
        var previous = replacing ? await repositories.Bookings.GetAsync(booking.Id, cancellationToken) : null;
        if (replacing && previous is null) throw new KeyNotFoundException("Booking not found.");
        if (previous is not null && (previous.UserId != booking.UserId || previous.BayId != booking.BayId || previous.LocalDate != booking.LocalDate || previous.PeriodKey != booking.PeriodKey || previous.StartUtc != booking.StartUtc || previous.EndUtc != booking.EndUtc))
            throw new ArgumentException("Stored booking intervals and identity cannot be rewritten.");
        if (await repositories.Users.GetAsync(booking.UserId, cancellationToken) is null)
            throw new ArgumentException("Booking user must belong to this shooting range.");
        if (await repositories.Bays.GetAsync(booking.BayId, cancellationToken) is null)
            throw new KeyNotFoundException("Bay not found.");
        if (booking.Status is not ShootingRangeBookingStatus.Pending and not ShootingRangeBookingStatus.Approved) return;
        if (previous is null || previous.Status is ShootingRangeBookingStatus.Rejected or ShootingRangeBookingStatus.Cancelled)
            await Services.ShootingRangeService.ValidateAdmissionAsync(repositories, booking, cancellationToken);
        if ((await repositories.Bookings.GetAllAsync(cancellationToken)).Any(existing => existing.Id != booking.Id && Services.ShootingRangeService.BlocksBay(existing, booking.BayId, booking.StartUtc, booking.EndUtc)))
            throw new InvalidOperationException("The selected slot is already pending or booked.");
    }

    internal static string NormalizeTenantSlug(string slug) => slug.Trim().ToLowerInvariant();
}