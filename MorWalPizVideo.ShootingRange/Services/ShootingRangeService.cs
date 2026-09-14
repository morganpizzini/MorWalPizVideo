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
    public async Task<ShootingRangeUser> RegisterAsync(RegisterShootingRangeUserRequest request)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        if (await repositories.Users.FindByUsernameAsync(username) is not null) throw new InvalidOperationException("Username already exists.");
        var hash = PasswordHashing.HashPassword(request.Password, out var salt);
        return await repositories.Users.InsertAsync(new ShootingRangeUser { Id = ObjectId.GenerateNewId().ToString(), Username = username, FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), PasswordHash = $"{hash}:{salt}" });
    }

    public async Task<ShootingRangeUser?> AuthenticateAsync(string username, string password)
    {
        var user = await repositories.Users.FindByUsernameAsync(username.Trim().ToLowerInvariant());
        if (user is null || user.Status != ShootingRangeAccountStatus.Approved) return null;
        var parts = user.PasswordHash.Split(':', 2);
        return parts.Length == 2 && PasswordHashing.VerifyPassword(password, parts[0], parts[1]) ? user : null;
    }

    public async Task<AvailabilityDto> GetAvailabilityAsync(DateOnly localDate, string periodKey, CancellationToken cancellationToken)
    {
        var config = (await repositories.Configs.GetAllAsync(cancellationToken)).FirstOrDefault() ?? new ShootingRangeConfig { Id = "default" };
        var zone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone);
        var (start, end) = ResolvePeriod(config, localDate, periodKey);
        var exceptions = await repositories.Exceptions.GetAllAsync(cancellationToken);
        if (!config.OpeningDays.Contains(localDate.DayOfWeek) || exceptions.Any(x => x.LocalDate == localDate && x.IsClosed && x.BayId is null)) return new(localDate, periodKey, start, end, []);
        var bays = await repositories.Bays.GetAllAsync(cancellationToken);
        var dayExceptionBayIds = exceptions.Where(x => x.LocalDate == localDate && x.BayId is not null).Select(x => x.BayId!).ToHashSet();
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;
        var daysUntil = localDate.DayNumber - DateOnly.FromDateTime(nowLocal).DayNumber;
        var bookings = await repositories.Bookings.GetAllAsync(cancellationToken);
        var visible = bays.Where(b => b.Status == ShootingRangeBayStatus.Available && !dayExceptionBayIds.Contains(b.Id)).Where(b => !bookings.Any(x => x.BayId == b.Id && x.LocalDate == localDate && x.Status is not ShootingRangeBookingStatus.Rejected and not ShootingRangeBookingStatus.Cancelled && x.StartUtc < end && x.EndUtc > start)).Where(b => daysUntil <= config.ReservedReleaseDaysBefore || b.WhitelistUserIds.Count == 0).ToArray();
        return new(localDate, periodKey, start, end, visible);
    }

    public async Task<ShootingRangeBooking> CreateBookingAsync(string userId, BookingRequest request, CancellationToken cancellationToken)
    {
        var config = (await repositories.Configs.GetAllAsync(cancellationToken)).FirstOrDefault() ?? new ShootingRangeConfig { Id = "default" };
        var zone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone);
        var (start, end) = ResolvePeriod(config, request.LocalDate, request.PeriodKey);
        var bay = await repositories.Bays.GetAsync(request.BayId, cancellationToken) ?? throw new KeyNotFoundException("Bay not found.");
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;
        var daysUntil = request.LocalDate.DayNumber - DateOnly.FromDateTime(nowLocal).DayNumber;
        if (daysUntil > config.ReservedReleaseDaysBefore && bay.WhitelistUserIds.Count > 0 && !bay.WhitelistUserIds.Contains(userId)) throw new UnauthorizedAccessException("Bay is reserved until the release threshold.");
        var booking = new ShootingRangeBooking { Id = ObjectId.GenerateNewId().ToString(), UserId = userId, BayId = bay.Id, LocalDate = request.LocalDate, PeriodKey = request.PeriodKey, StartUtc = start, EndUtc = end, Request = request.Request.Trim() };
        return await repositories.Bookings.InsertIfAvailableAsync(booking, cancellationToken);
    }

    public static (DateTime StartUtc, DateTime EndUtc) ResolvePeriod(ShootingRangeConfig config, DateOnly date, string periodKey)
    {
        var localStart = periodKey.Equals("morning", StringComparison.OrdinalIgnoreCase) ? config.MorningStart : periodKey.Equals("afternoon", StringComparison.OrdinalIgnoreCase) ? config.AfternoonStart : TimeSpan.Parse(periodKey);
        var localEnd = periodKey.Equals("morning", StringComparison.OrdinalIgnoreCase) ? config.MorningEnd : periodKey.Equals("afternoon", StringComparison.OrdinalIgnoreCase) ? config.AfternoonEnd : localStart.Add(TimeSpan.FromMinutes(config.HourlyMinutes));
        var zone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone);
        var start = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.FromTimeSpan(localStart)), zone);
        var end = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.FromTimeSpan(localEnd)), zone);
        return (start, end);
    }
}
