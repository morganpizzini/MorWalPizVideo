using FluentAssertions;
using MorWalPizVideo.ShootingRange.Contracts;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Services;

namespace MorWalPizVideo.BackOffice.Tests.Services;

public sealed class ShootingRangeServiceTests
{
    [Fact]
    public void ResolvePeriod_uses_field_timezone_and_returns_utc_instants()
    {
        var config = new ShootingRangeConfig { TimeZone = "Europe/Rome", MorningStart = new(9, 0, 0), MorningEnd = new(13, 0, 0) };
        var result = ShootingRangeService.ResolvePeriod(config, new DateOnly(2026, 1, 15), "morning");
        result.StartUtc.Should().Be(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc));
        result.EndUtc.Should().Be(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task CreateBooking_rejects_second_pending_overlap()
    {
        var repositories = new MockShootingRangeRepositorySet();
        await repositories.Bays.InsertAsync(new ShootingRangeBay { Id = "bay-1", Code = "01" });
        var service = new ShootingRangeService(repositories);
        var request = new BookingRequest("bay-1", new DateOnly(2026, 1, 15), "morning", "first request");
        await service.CreateBookingAsync("user-1", request, CancellationToken.None);
        var act = () => service.CreateBookingAsync("user-2", request, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The selected slot is already pending or booked.");
    }

    [Fact]
    public async Task CreateBooking_applies_reserved_bay_threshold_to_non_whitelist_user()
    {
        var repositories = new MockShootingRangeRepositorySet();
        await repositories.Bays.InsertAsync(new ShootingRangeBay { Id = "bay-1", Code = "01", WhitelistUserIds = ["approved-user"] });
        var service = new ShootingRangeService(repositories);
        var request = new BookingRequest("bay-1", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), "morning", "request");
        var act = () => service.CreateBookingAsync("other-user", request, CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
