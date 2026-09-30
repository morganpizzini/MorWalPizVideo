using MorWalPizVideo.ShootingRange.Models;

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
    Task<T> ExecuteGuardedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
    IShootingRangeRepository<ShootingRangeConfig> Configs { get; }
    IShootingRangeRepository<ShootingRangeBay> Bays { get; }
    IShootingRangeRepository<ShootingRangeException> Exceptions { get; }
    IShootingRangeUserRepository Users { get; }
    IShootingRangeBookingRepository Bookings { get; }
    IShootingRangeRepository<ShootingRangeThread> Threads { get; }
    IShootingRangeRepository<ShootingRangeLocalSession> LocalSessions { get; }
}

internal static class ShootingRangeWriteValidation
{
    internal static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

    internal static T Prepare<T>(T item) where T : BaseEntity => item is ShootingRangeUser user
        ? (T)(BaseEntity)(user with { NormalizedUsername = NormalizeUsername(user.Username) }) : item;

    internal static void ValidateUsernamePreflight(IReadOnlyList<ShootingRangeUser> users)
    {
        if (users.Any(user => string.IsNullOrWhiteSpace(user.Username)) || users.GroupBy(user => NormalizeUsername(user.Username), StringComparer.Ordinal).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Normalized username preflight failed. Review duplicate or empty usernames before release; no accounts were modified.");
        if (users.Any(user => user.NormalizedUsername != NormalizeUsername(user.Username)))
            throw new InvalidOperationException("Normalized usernames require an approved additive backfill before index creation; no accounts were modified.");
    }

    internal static async Task ValidateAsync<T>(IShootingRangeRepositorySet repositories, T item, bool replacing, CancellationToken cancellationToken) where T : BaseEntity
    {
        if (item is ShootingRangeConfig config) Services.ShootingRangeService.ValidateConfig(config);
        if (item is ShootingRangeBay bay)
        {
            if (!Enum.IsDefined(bay.Status) || string.IsNullOrWhiteSpace(bay.Code) || bay.Partitions < 0 || bay.Plates < 0 || bay.Mechanisms is null || bay.WhitelistUserIds is null || bay.Mechanisms.Any(mechanism => string.IsNullOrWhiteSpace(mechanism.Type) || mechanism.Quantity <= 0)) throw new ArgumentException("Invalid bay.");
            foreach (var userId in bay.WhitelistUserIds)
                if (await repositories.Users.GetAsync(userId, cancellationToken) is null) throw new ArgumentException("Whitelist account not found.");
        }
        if (item is ShootingRangeUser user)
        {
            if (!Enum.IsDefined(user.Status) || string.IsNullOrWhiteSpace(user.Username)) throw new ArgumentException("Invalid account status or username.");
            if ((await repositories.Users.GetAllAsync(cancellationToken)).Any(existing => existing.Id != user.Id && NormalizeUsername(existing.Username) == user.NormalizedUsername))
                throw new InvalidOperationException("Username already exists.");
        }
        if (item is ShootingRangeException closure && closure.BayId is not null && await repositories.Bays.GetAsync(closure.BayId, cancellationToken) is null) throw new KeyNotFoundException("Bay not found.");
        if (item is not ShootingRangeBooking booking) return;
        if (!Enum.IsDefined(booking.Status) || booking.StartUtc.Kind != DateTimeKind.Utc || booking.EndUtc.Kind != DateTimeKind.Utc || booking.StartUtc >= booking.EndUtc) throw new ArgumentException("Invalid booking interval or status.");
        var previous = replacing ? await repositories.Bookings.GetAsync(booking.Id, cancellationToken) : null;
        if (replacing && previous is null) throw new KeyNotFoundException("Booking not found.");
        if (previous is not null && (previous.UserId != booking.UserId || previous.BayId != booking.BayId || previous.LocalDate != booking.LocalDate || previous.PeriodKey != booking.PeriodKey || previous.StartUtc != booking.StartUtc || previous.EndUtc != booking.EndUtc))
            throw new ArgumentException("Stored booking intervals and identity cannot be rewritten.");
        if (booking.Status is not ShootingRangeBookingStatus.Pending and not ShootingRangeBookingStatus.Approved) return;
        if (previous is null || previous.Status is ShootingRangeBookingStatus.Rejected or ShootingRangeBookingStatus.Cancelled)
            await Services.ShootingRangeService.ValidateAdmissionAsync(repositories, booking, cancellationToken);
        if ((await repositories.Bookings.GetAllAsync(cancellationToken)).Any(existing => existing.Id != booking.Id && Services.ShootingRangeService.BlocksBay(existing, booking.BayId, booking.StartUtc, booking.EndUtc)))
            throw new InvalidOperationException("The selected slot is already pending or booked.");
    }
}