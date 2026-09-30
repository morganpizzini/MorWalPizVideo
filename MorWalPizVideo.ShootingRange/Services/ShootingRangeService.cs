using System.Security.Claims;
using System.Security.Cryptography;
using MorWalPizVideo.ShootingRange.Contracts;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Security;
using MongoDB.Bson;

namespace MorWalPizVideo.ShootingRange.Services;

public sealed class ShootingRangeService(IShootingRangeRepositorySet repositories)
{
    public Task<ShootingRangeUser> RegisterAsync(RegisterShootingRangeUserRequest request) => repositories.ExecuteGuardedAsync(async () =>
    {
        var username = request.Username.Trim().ToLowerInvariant();
        if (await repositories.Users.FindByUsernameAsync(username) is not null) throw new InvalidOperationException("Username already exists.");
        var hash = PasswordHashing.HashPassword(request.Password, out var salt);
        return await repositories.Users.InsertAsync(new ShootingRangeUser { Id = ObjectId.GenerateNewId().ToString(), Username = username, FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), PasswordHash = $"{hash}:{salt}" });
    });

    public async Task<ShootingRangeUser?> AuthenticateAsync(string username, string password)
    {
        var user = await repositories.Users.FindByUsernameAsync(username.Trim().ToLowerInvariant());
        if (user is null || user.Status != ShootingRangeAccountStatus.Approved) return null;
        var parts = user.PasswordHash.Split(':', 2);
        return parts.Length == 2 && PasswordHashing.VerifyPassword(password, parts[0], parts[1]) ? user : null;
    }

    public async Task<ShootingRangeConfig> GetConfigAsync(CancellationToken cancellationToken) => await LoadConfigAsync(repositories, cancellationToken);

    private static async Task<ShootingRangeConfig> LoadConfigAsync(IShootingRangeRepositorySet repositories, CancellationToken cancellationToken) => (await repositories.Configs.GetAllAsync(cancellationToken)).OrderByDescending(config => config.Id == "default").FirstOrDefault() ?? new ShootingRangeConfig { Id = "default" };

    public async Task<SessionsDto> GetSessionsAsync(DateOnly localDate, CancellationToken cancellationToken)
    {
        var config = await GetConfigAsync(cancellationToken);
        var sessions = GenerateSessions(config, localDate);
        var closures = await repositories.Exceptions.GetAllAsync(cancellationToken);
        return new(localDate, config.TimeZone, !config.OpeningDays.Contains(localDate.DayOfWeek) || closures.Any(closure => closure.LocalDate == localDate && closure.IsClosed && closure.BayId is null)
            ? [] : sessions.Where(session => session.StartUtc > repositories.Clock.GetUtcNow().UtcDateTime).ToArray());
    }

    public async Task<AvailabilityDto> GetAvailabilityAsync(DateOnly localDate, string periodKey, CancellationToken cancellationToken, string? userId = null)
    {
        var config = await GetConfigAsync(cancellationToken);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone);
        var (start, end) = ResolvePeriod(config, localDate, periodKey);
        var exceptions = await repositories.Exceptions.GetAllAsync(cancellationToken);
        if (start <= repositories.Clock.GetUtcNow().UtcDateTime || !config.OpeningDays.Contains(localDate.DayOfWeek) || exceptions.Any(x => x.LocalDate == localDate && x.IsClosed && x.BayId is null)) return new(localDate, periodKey, start, end, []);
        var bays = await repositories.Bays.GetAllAsync(cancellationToken);
        var dayExceptionBayIds = exceptions.Where(x => x.LocalDate == localDate && x.IsClosed && x.BayId is not null).Select(x => x.BayId!).ToHashSet();
        var nowLocal = TimeZoneInfo.ConvertTime(repositories.Clock.GetUtcNow(), zone).Date;
        var daysUntil = localDate.DayNumber - DateOnly.FromDateTime(nowLocal).DayNumber;
        var bookings = await repositories.Bookings.GetAllAsync(cancellationToken);
        var visible = bays.Where(b => b.Status == ShootingRangeBayStatus.Available && !dayExceptionBayIds.Contains(b.Id)).Where(b => !bookings.Any(booking => BlocksBay(booking, b.Id, start, end))).Where(b => daysUntil <= config.ReservedReleaseDaysBefore || b.WhitelistUserIds.Count == 0 || userId is not null && b.WhitelistUserIds.Contains(userId)).ToArray();
        return new(localDate, periodKey, start, end, visible);
    }

    public Task<T> AdminMutationAsync<T>(string actorId, Func<Task<T>> mutation, CancellationToken cancellationToken) => repositories.ExecuteGuardedAsync(async () =>
    {
        await RequireAdminAsync(actorId, cancellationToken);
        return await mutation();
    }, cancellationToken);

    public async Task RequireAdminAsync(string actorId, CancellationToken cancellationToken)
    {
        var actor = await repositories.Users.GetAsync(actorId, cancellationToken);
        if (actor is null || !actor.IsAdmin || actor.Status != ShootingRangeAccountStatus.Approved || actor.ForcePasswordChange)
            throw new UnauthorizedAccessException("A currently approved administrator is required.");
    }

    public Task<ShootingRangeBooking> DecideBookingAsync(string actorId, string id, bool approved, CancellationToken cancellationToken) => AdminMutationAsync(actorId, async () =>
    {
        var booking = await repositories.Bookings.GetAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Booking not found.");
        return await repositories.Bookings.ReplaceAsync(booking with { Status = approved ? ShootingRangeBookingStatus.Approved : ShootingRangeBookingStatus.Rejected }, cancellationToken);
    }, cancellationToken);

    public Task<ShootingRangeBooking> CreateBookingAsync(string userId, BookingRequest request, CancellationToken cancellationToken) => repositories.ExecuteGuardedAsync(async () =>
    {
        var config = await GetConfigAsync(cancellationToken);
        var (start, end) = ResolvePeriod(config, request.LocalDate, request.PeriodKey);
        if ((request.ExpectedStartUtc.HasValue || request.ExpectedEndUtc.HasValue) && (request.ExpectedStartUtc != start || request.ExpectedEndUtc != end))
            throw new ArgumentException("The schedule changed. Refresh sessions before booking.");
        var booking = new ShootingRangeBooking { Id = ObjectId.GenerateNewId().ToString(), UserId = userId, BayId = request.BayId, LocalDate = request.LocalDate, PeriodKey = request.PeriodKey, StartUtc = start, EndUtc = end, Request = request.Request.Trim() };
        return await repositories.Bookings.InsertIfAvailableAsync(booking, cancellationToken);
    }, cancellationToken);

    internal static async Task ValidateAdmissionAsync(IShootingRangeRepositorySet repositories, ShootingRangeBooking booking, CancellationToken cancellationToken)
    {
        var user = await repositories.Users.GetAsync(booking.UserId, cancellationToken);
        if (user is null || user.Status != ShootingRangeAccountStatus.Approved || user.ForcePasswordChange) throw new UnauthorizedAccessException("An approved account without a required password change is necessary.");
        var config = await LoadConfigAsync(repositories, cancellationToken);
        var (start, end) = ResolvePeriod(config, booking.LocalDate, booking.PeriodKey);
        if (start != booking.StartUtc || end != booking.EndUtc) throw new ArgumentException("Historical interval is not offered by the current schedule.");
        if (start <= repositories.Clock.GetUtcNow().UtcDateTime || !config.OpeningDays.Contains(booking.LocalDate.DayOfWeek)) throw new ArgumentException("The field is closed or the session has already started.");
        var bay = await repositories.Bays.GetAsync(booking.BayId, cancellationToken) ?? throw new KeyNotFoundException("Bay not found.");
        var closures = await repositories.Exceptions.GetAllAsync(cancellationToken);
        if (bay.Status != ShootingRangeBayStatus.Available || closures.Any(closure => closure.LocalDate == booking.LocalDate && closure.IsClosed && (closure.BayId is null || closure.BayId == bay.Id))) throw new ArgumentException("The field or bay is closed.");
        var nowLocal = TimeZoneInfo.ConvertTime(repositories.Clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone));
        var daysUntil = booking.LocalDate.DayNumber - DateOnly.FromDateTime(nowLocal.DateTime).DayNumber;
        if (daysUntil > config.ReservedReleaseDaysBefore && bay.WhitelistUserIds.Count > 0 && !bay.WhitelistUserIds.Contains(booking.UserId)) throw new UnauthorizedAccessException("Bay is reserved until the release threshold.");
    }

    public static (DateTime StartUtc, DateTime EndUtc) ResolvePeriod(ShootingRangeConfig config, DateOnly date, string periodKey)
    {
        var session = GenerateSessions(config, date).SingleOrDefault(session => session.PeriodKey.Equals(periodKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("Session is not offered by the current schedule.");
        return (session.StartUtc, session.EndUtc);
    }

    public static void ValidateConfig(ShootingRangeConfig config)
    {
        if (!Enum.IsDefined(config.SessionMode) || config.HourlyMinutes != 60 || config.ReservedReleaseDaysBefore < 0 || config.OpeningDays is null || config.OpeningDays.Count == 0 || config.OpeningDays.Any(day => !Enum.IsDefined(day)))
            throw new ArgumentException("Select a valid mode, opening days and fixed 60-minute duration.");
        try { TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone); }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException) { throw new ArgumentException("Invalid field time zone.", exception); }
        static bool ValidWindow(TimeSpan? start, TimeSpan? end) => start.HasValue && end.HasValue && start >= TimeSpan.Zero && end < TimeSpan.FromDays(1) && start < end && start.Value.Ticks % TimeSpan.TicksPerMinute == 0 && end.Value.Ticks % TimeSpan.TicksPerMinute == 0;
        if (config.SessionMode == ShootingRangeSessionMode.HourlyContinuous)
        {
            if (!ValidWindow(config.ContinuousStart, config.ContinuousEnd) || config.ContinuousEnd - config.ContinuousStart < TimeSpan.FromHours(1))
                throw new ArgumentException("Continuous opening must contain at least one full hour.");
        }
        else if (!ValidWindow(config.MorningStart, config.MorningEnd) || !ValidWindow(config.AfternoonStart, config.AfternoonEnd) || config.MorningEnd >= config.AfternoonStart ||
                 config.SessionMode == ShootingRangeSessionMode.Hourly && (config.MorningEnd - config.MorningStart < TimeSpan.FromHours(1) || config.AfternoonEnd - config.AfternoonStart < TimeSpan.FromHours(1)))
            throw new ArgumentException("Morning and afternoon must be ordered with a positive break.");
    }

    public static IReadOnlyList<SessionDto> GenerateSessions(ShootingRangeConfig config, DateOnly date)
    {
        ValidateConfig(config);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone);
        var sessions = new List<SessionDto>();
        void Add(string key, TimeSpan start, TimeSpan end)
        {
            var localStart = date.ToDateTime(TimeOnly.FromTimeSpan(start), DateTimeKind.Unspecified);
            var localEnd = date.ToDateTime(TimeOnly.FromTimeSpan(end), DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(localStart) || zone.IsInvalidTime(localEnd) || zone.IsAmbiguousTime(localStart) || zone.IsAmbiguousTime(localEnd)) return;
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, zone);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, zone);
            if (config.SessionMode != ShootingRangeSessionMode.Periods && endUtc - startUtc != TimeSpan.FromHours(1)) return;
            sessions.Add(new(key, startUtc, endUtc));
        }
        void AddHours(TimeSpan start, TimeSpan end)
        {
            for (var slotStart = start; slotStart.Add(TimeSpan.FromHours(1)) <= end; slotStart = slotStart.Add(TimeSpan.FromHours(1)))
                Add(slotStart.ToString(@"hh\:mm", System.Globalization.CultureInfo.InvariantCulture), slotStart, slotStart.Add(TimeSpan.FromHours(1)));
        }
        if (config.SessionMode == ShootingRangeSessionMode.Periods)
        {
            Add("morning", config.MorningStart, config.MorningEnd);
            Add("afternoon", config.AfternoonStart, config.AfternoonEnd);
        }
        else if (config.SessionMode == ShootingRangeSessionMode.Hourly)
        {
            AddHours(config.MorningStart, config.MorningEnd);
            AddHours(config.AfternoonStart, config.AfternoonEnd);
        }
        else AddHours(config.ContinuousStart!.Value, config.ContinuousEnd!.Value);
        return sessions;
    }

    internal static bool BlocksBay(ShootingRangeBooking booking, string bayId, DateTime start, DateTime end) => booking.BayId == bayId &&
        (booking.Status is ShootingRangeBookingStatus.Pending or ShootingRangeBookingStatus.Approved) &&
        (booking.StartUtc.Kind != DateTimeKind.Utc || booking.EndUtc.Kind != DateTimeKind.Utc || booking.StartUtc >= booking.EndUtc || booking.StartUtc < end && booking.EndUtc > start);
}
