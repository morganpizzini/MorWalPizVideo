using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Security;
using MorWalPizVideo.ShootingRange.Contracts;

namespace MorWalPizVideo.ShootingRange.Services;

public sealed class InMemorySampleDataSeeder
{
    private static readonly string[] MechanismTypeNames =
    [
        "Bersaglio basculante",
        "Pepper popper",
        "Piastra girevole"
    ];

    public static async Task<ShootingRangeTenant> EnsureTenantAsync(
        IShootingRangeRepositorySet repositories,
        string slug,
        string clientOrigin,
        string? configuredId,
        CancellationToken cancellationToken)
    {
        var existing = (await repositories.Tenants.GetAllAsync(cancellationToken))
            .SingleOrDefault(tenant => tenant.Slug == slug);
        if (existing is not null) return existing;

        return await repositories.Tenants.InsertAsync(new ShootingRangeTenant
        {
            Id = configuredId ?? ShootingRangeIds.NewId(),
            Slug = slug,
            Name = "Local Shooting Range",
            ClientOrigin = clientOrigin
        }, cancellationToken);
    }

    public async Task SeedAsync(IShootingRangeRepositorySet repositories, CancellationToken cancellationToken)
    {
        await repositories.ExecuteGuardedAsync(async () =>
        {
            var config = await repositories.Configs.GetAsync("default", cancellationToken);
            if (config is null)
            {
                config = await repositories.Configs.ReplaceAsync(new ShootingRangeConfig { Id = "default" }, cancellationToken);
            }

            foreach (var name in MechanismTypeNames)
            {
                var existing = (await repositories.MechanismTypes.GetAllAsync(cancellationToken))
                    .FirstOrDefault(item => item.NormalizedName == name.ToLowerInvariant());
                if (existing is null)
                    await repositories.MechanismTypes.InsertAsync(
                        new ShootingRangeMechanismType { Id = $"sample-type-{Slug(name)}", Name = name },
                        cancellationToken);
            }

            var users = new[]
            {
                await EnsureUserAsync(repositories, "sample-user-luca", "luca.demo", "Luca", "Rossi", ShootingRangeAccountStatus.Approved, cancellationToken),
                await EnsureUserAsync(repositories, "sample-user-sara", "sara.demo", "Sara", "Bianchi", ShootingRangeAccountStatus.Approved, cancellationToken),
                await EnsureUserAsync(repositories, "sample-user-pending", "marco.demo", "Marco", "Verdi", ShootingRangeAccountStatus.Pending, cancellationToken)
            };

            var bays = new[]
            {
                await EnsureBayAsync(repositories, "sample-bay-01", "Stage 01", "Postazione disponibile", [new(MechanismTypeNames[0], 2), new(MechanismTypeNames[2], 1)], 20, 10, 2, ShootingRangeBayStatus.Available, [], cancellationToken),
                await EnsureBayAsync(repositories, "sample-bay-02", "Stage 02", "Postazione riservata", [new(MechanismTypeNames[1], 4)], 16, 8, 1, ShootingRangeBayStatus.Available, [users[0].Id], cancellationToken),
                await EnsureBayAsync(repositories, "sample-bay-03", "Stage 03", "Postazione a piastre", [new(MechanismTypeNames[2], 3)], 12, 14, 0, ShootingRangeBayStatus.Available, [], cancellationToken),
                await EnsureBayAsync(repositories, "sample-bay-04", "Stage 04", "Postazione in manutenzione", [new(MechanismTypeNames[0], 1)], 10, 6, 1, ShootingRangeBayStatus.Maintenance, [], cancellationToken)
            };

            var futureSessions = GetFutureSessions(config, repositories.Clock.GetUtcNow(), 4);
            await EnsureClosureAsync(repositories, "sample-closure-field", futureSessions[0].Date, null, "Chiusura dimostrativa del campo", cancellationToken);
            await EnsureClosureAsync(repositories, "sample-closure-bay", futureSessions[1].Date, bays[2].Id, "Manutenzione programmata della postazione", cancellationToken);

            await EnsureBookingAsync(repositories, users[0], bays[0], futureSessions[2].Date, futureSessions[2].Session, "Prenotazione dimostrativa in attesa", cancellationToken);
            await EnsureBookingAsync(repositories, users[1], bays[2], futureSessions[3].Date, futureSessions[3].Session, "Prenotazione dimostrativa approvata", cancellationToken, ShootingRangeBookingStatus.Approved);
            return true;
        }, cancellationToken);
    }

    private static async Task<ShootingRangeUser> EnsureUserAsync(
        IShootingRangeRepositorySet repositories,
        string id,
        string username,
        string firstName,
        string lastName,
        ShootingRangeAccountStatus status,
        CancellationToken cancellationToken)
    {
        if (await repositories.Users.GetAsync(id, cancellationToken) is { } existing) return existing;
        // var hash = PasswordHashing.HashPassword(Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)), out var salt);
        var hash = PasswordHashing.HashPassword(username, out var salt);
        return await repositories.Users.InsertAsync(new ShootingRangeUser
        {
            Id = id,
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = $"{hash}:{salt}",
            Status = status
        }, cancellationToken);
    }

    private static async Task<ShootingRangeBay> EnsureBayAsync(
        IShootingRangeRepositorySet repositories,
        string id,
        string code,
        string description,
        List<Mechanism> mechanisms,
        int partitions,
        int plates,
        int pepper,
        ShootingRangeBayStatus status,
        List<string> whitelistUserIds,
        CancellationToken cancellationToken)
    {
        if (await repositories.Bays.GetAsync(id, cancellationToken) is { } existing) return existing;
        return await repositories.Bays.InsertAsync(new ShootingRangeBay
        {
            Id = id,
            Code = code,
            Description = description,
            Mechanisms = mechanisms,
            Partitions = partitions,
            Plates = plates,
            Pepper = pepper,
            Status = status,
            WhitelistUserIds = whitelistUserIds
        }, cancellationToken);
    }

    private static async Task EnsureClosureAsync(
        IShootingRangeRepositorySet repositories,
        string id,
        DateOnly date,
        string? bayId,
        string reason,
        CancellationToken cancellationToken)
    {
        if (await repositories.Exceptions.GetAsync(id, cancellationToken) is not null) return;
        await repositories.Exceptions.InsertAsync(new ShootingRangeException
        {
            Id = id,
            LocalDate = date,
            BayId = bayId,
            IsClosed = true,
            Reason = reason
        }, cancellationToken);
    }

    private static async Task EnsureBookingAsync(
        IShootingRangeRepositorySet repositories,
        ShootingRangeUser user,
        ShootingRangeBay bay,
        DateOnly date,
        SessionDto session,
        string request,
        CancellationToken cancellationToken,
        ShootingRangeBookingStatus status = ShootingRangeBookingStatus.Pending)
    {
        var id = $"sample-booking-{bay.Id}-{date:yyyyMMdd}-{Slug(session.PeriodKey)}";
        if (await repositories.Bookings.GetAsync(id, cancellationToken) is not null) return;
        await repositories.Bookings.InsertAsync(new ShootingRangeBooking
        {
            Id = id,
            UserId = user.Id,
            BayId = bay.Id,
            LocalDate = date,
            PeriodKey = session.PeriodKey,
            StartUtc = session.StartUtc,
            EndUtc = session.EndUtc,
            Request = request,
            Status = status
        }, cancellationToken);
    }

    private static IReadOnlyList<(DateOnly Date, SessionDto Session)> GetFutureSessions(
        ShootingRangeConfig config,
        DateTimeOffset now,
        int count)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone);
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        var result = new List<(DateOnly Date, SessionDto Session)>();
        for (var offset = 1; offset <= 180 && result.Count < count; offset++)
        {
            var date = localToday.AddDays(offset);
            if (!config.OpeningDays.Contains(date.DayOfWeek)) continue;
            var session = ShootingRangeService.GenerateSessions(config, date)
                .FirstOrDefault(candidate => candidate.StartUtc > now.UtcDateTime);
            if (session is not null) result.Add((date, session));
        }

        if (result.Count < count)
            throw new InvalidOperationException("Could not find enough future field sessions to seed in-memory sample data.");
        return result;
    }

    private static string Slug(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();
}
