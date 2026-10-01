using FluentAssertions;
using MorWalPizVideo.ShootingRange.Contracts;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Services;

namespace MorWalPizVideo.ShootingRange.Tests.Services;

[Trait("Category", "TestGroup:ShootingRange")]
public sealed class ShootingRangeServiceTests
{
    [Theory]
    [InlineData(ShootingRangeSessionMode.Periods, "morning,afternoon")]
    [InlineData(ShootingRangeSessionMode.Hourly, "09:00,10:00,11:00,12:00,14:00,15:00,16:00,17:00")]
    [InlineData(ShootingRangeSessionMode.HourlyContinuous, "09:30,10:30,11:30")]
    public void Sessions_follow_mode_and_exclude_break_and_incomplete_tail(ShootingRangeSessionMode mode, string expected)
    {
        var config = new ShootingRangeConfig { SessionMode = mode, ContinuousStart = new(9, 30, 0), ContinuousEnd = new(13, 0, 0) };
        ShootingRangeService.GenerateSessions(config, new(2030, 1, 12)).Select(session => session.PeriodKey).Should().Equal(expected.Split(','));
    }

    [Theory]
    [InlineData(ShootingRangeSessionMode.Periods, "09:00")]
    [InlineData(ShootingRangeSessionMode.Hourly, "morning")]
    [InlineData(ShootingRangeSessionMode.Hourly, "13:00")]
    public void ResolvePeriod_rejects_keys_outside_current_mode(ShootingRangeSessionMode mode, string key)
    {
        var act = () => ShootingRangeService.ResolvePeriod(new() { SessionMode = mode }, new(2030, 1, 12), key);
        act.Should().Throw<ArgumentException>();
    }

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
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new ShootingRangeBay { Id = "bay-1", Code = "01" });
        var service = new ShootingRangeService(repositories);
        var request = new BookingRequest("bay-1", new DateOnly(2030, 1, 12), "morning", "first request");
        await service.CreateBookingAsync("user-1", request, CancellationToken.None);
        var act = () => service.CreateBookingAsync("user-2", request, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The selected slot is already pending or booked.");
    }

    [Fact]
    public async Task CreateBooking_applies_reserved_bay_threshold_to_non_whitelist_user()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new ShootingRangeBay { Id = "bay-1", Code = "01", WhitelistUserIds = ["approved-user"] });
        var service = new ShootingRangeService(repositories);
        var request = new BookingRequest("bay-1", new DateOnly(2030, 1, 12), "morning", "request");
        var act = () => service.CreateBookingAsync("other-user", request, CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public void Bson_preserves_numeric_modes_object_ids_and_default_config_id()
    {
        var config = new ShootingRangeConfig { Id = "default" };
        var document = MongoDB.Bson.BsonExtensionMethods.ToBsonDocument(config);
        document["_id"].AsString.Should().Be("default");
        document["sessionMode"].AsInt32.Should().Be(0);
        MongoDB.Bson.Serialization.BsonSerializer.Deserialize<ShootingRangeConfig>(document).ContinuousStart.Should().BeNull();
        var id = MongoDB.Bson.ObjectId.GenerateNewId();
        var bayDocument = MongoDB.Bson.BsonExtensionMethods.ToBsonDocument(new ShootingRangeBay { Id = id.ToString() });
        bayDocument["_id"].AsObjectId.Should().Be(id);
        MongoDB.Bson.Serialization.BsonSerializer.Deserialize<ShootingRangeBay>(bayDocument).Id.Should().Be(id.ToString());
        ((int)ShootingRangeSessionMode.Hourly).Should().Be(1);
        ((int)ShootingRangeSessionMode.HourlyContinuous).Should().Be(2);
    }

    [Fact]
    public async Task Generic_insert_and_replace_cannot_bypass_overlap_or_reactivation_admission()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        var service = new ShootingRangeService(repositories);
        var first = await service.CreateBookingAsync("user-1", new("bay-1", new(2030, 1, 12), "morning", ""), default);
        var inactive = first with { Id = "inactive", UserId = "user-2", Status = ShootingRangeBookingStatus.Rejected, PeriodKey = "09:00", EndUtc = first.StartUtc.AddHours(1) };
        await repositories.Bookings.InsertAsync(inactive);
        await repositories.Configs.ReplaceAsync(new() { Id = "default", SessionMode = ShootingRangeSessionMode.Hourly });
        var insert = () => repositories.Bookings.InsertAsync(inactive with { Id = "second", Status = ShootingRangeBookingStatus.Pending });
        await insert.Should().ThrowAsync<InvalidOperationException>();
        var replace = () => repositories.Bookings.ReplaceAsync(inactive with { Status = ShootingRangeBookingStatus.Approved });
        await replace.Should().ThrowAsync<InvalidOperationException>();
        await repositories.Bookings.ReplaceAsync(first with { Status = ShootingRangeBookingStatus.Approved });
        (await repositories.Bookings.GetAsync(first.Id))!.EndUtc.Should().Be(first.EndUtc);
        await repositories.Bookings.ReplaceAsync(first with { Status = ShootingRangeBookingStatus.Rejected });
        await repositories.Bookings.ReplaceAsync(inactive with { Status = ShootingRangeBookingStatus.Approved });
        var oldPeriod = () => repositories.Bookings.ReplaceAsync(first with { Status = ShootingRangeBookingStatus.Approved });
        await oldPeriod.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Concurrent_pending_writes_have_one_winner_and_other_bays_remain_available()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        await repositories.Bays.InsertAsync(new() { Id = "bay-2", Code = "02" });
        var service = new ShootingRangeService(repositories);
        async Task<bool> TryBook(string user)
        {
            try { await service.CreateBookingAsync(user, new("bay-1", new(2030, 1, 12), "morning", ""), default); return true; }
            catch (InvalidOperationException) { return false; }
        }
        (await Task.WhenAll(Task.Run(() => TryBook("user-1")), Task.Run(() => TryBook("user-2")))).Count(success => success).Should().Be(1);
        await service.CreateBookingAsync("user-1", new("bay-2", new(2030, 1, 12), "morning", ""), default);
        await service.CreateBookingAsync("user-1", new("bay-1", new(2030, 1, 12), "afternoon", ""), default);
    }

    [Fact]
    public async Task Guard_rolls_back_mutations_on_failure()
    {
        var repositories = await CreateRepositoriesAsync();
        var action = () => repositories.ExecuteGuardedAsync<int>(async () =>
        {
            await repositories.Bays.InsertAsync(new() { Id = "temporary", Code = "01" });
            throw new ArgumentException("rollback");
        });
        await action.Should().ThrowAsync<ArgumentException>();
        (await repositories.Bays.GetAllAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("closed-day")]
    [InlineData("past")]
    [InlineData("field")]
    [InlineData("bay")]
    [InlineData("maintenance")]
    [InlineData("disabled")]
    [InlineData("password")]
    [InlineData("missing-user")]
    public async Task Admission_rejects_direct_requests(string reason)
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01", Status = reason == "maintenance" ? ShootingRangeBayStatus.Maintenance : ShootingRangeBayStatus.Available });
        if (reason is "field" or "bay") await repositories.Exceptions.InsertAsync(new() { Id = "closure", LocalDate = new(2030, 1, 12), IsClosed = true, BayId = reason == "bay" ? "bay-1" : null });
        if (reason is "disabled" or "password") await repositories.Users.ReplaceAsync((await repositories.Users.GetAsync("user-1"))! with { Status = reason == "disabled" ? ShootingRangeAccountStatus.Disabled : ShootingRangeAccountStatus.Approved, ForcePasswordChange = reason == "password" });
        var date = reason == "closed-day" ? new DateOnly(2030, 1, 11) : reason == "past" ? new DateOnly(2029, 12, 29) : new DateOnly(2030, 1, 12);
        var action = () => new ShootingRangeService(repositories).CreateBookingAsync(reason == "missing-user" ? "missing" : "user-1", new("bay-1", date, "morning", ""), default);
        await action.Should().ThrowAsync<Exception>();
        (await repositories.Bookings.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Non_closing_exception_does_not_hide_bay_and_release_boundary_allows_booking()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01", WhitelistUserIds = ["user-2"] });
        await repositories.Exceptions.InsertAsync(new() { Id = "note", LocalDate = new(2030, 1, 12), BayId = "bay-1", IsClosed = false });
        await repositories.Configs.ReplaceAsync(new() { Id = "default", ReservedReleaseDaysBefore = 11 });
        var service = new ShootingRangeService(repositories);
        (await service.GetAvailabilityAsync(new(2030, 1, 12), "morning", default)).Bays.Should().HaveCount(1);
        await service.CreateBookingAsync("user-1", new("bay-1", new(2030, 1, 12), "morning", ""), default);
    }

    [Theory]
    [InlineData(45, ShootingRangeSessionMode.Hourly)]
    [InlineData(60, ShootingRangeSessionMode.HourlyContinuous)]
    [InlineData(60, (ShootingRangeSessionMode)99)]
    public void Invalid_configuration_is_rejected(int minutes, ShootingRangeSessionMode mode)
    {
        var action = () => ShootingRangeService.ValidateConfig(new() { HourlyMinutes = minutes, SessionMode = mode });
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Changed_schedule_rejects_optional_expected_interval_but_preserves_legacy_requests()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        var (start, end) = ShootingRangeService.ResolvePeriod(new(), new(2030, 1, 12), "morning");
        await repositories.Configs.ReplaceAsync(new() { Id = "default", MorningStart = new(10, 0, 0) });
        var service = new ShootingRangeService(repositories);
        var action = () => service.CreateBookingAsync("user-1", new("bay-1", new(2030, 1, 12), "morning", "", start, end), default);
        await action.Should().ThrowAsync<ArgumentException>();
        var booking = await service.CreateBookingAsync("user-1", new("bay-1", new(2030, 1, 12), "morning", ""), default);
        booking.StartUtc.Should().Be(start.AddHours(1));
    }

    [Fact]
    public void Hourly_sessions_skip_ambiguous_or_invalid_DST_times()
    {
        var config = new ShootingRangeConfig { SessionMode = ShootingRangeSessionMode.HourlyContinuous, ContinuousStart = new(1, 0, 0), ContinuousEnd = new(5, 0, 0) };
        foreach (var date in new[] { new DateOnly(2030, 3, 31), new DateOnly(2030, 10, 27) })
            ShootingRangeService.GenerateSessions(config, date).Should().OnlyContain(session => session.EndUtc - session.StartUtc == TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task Adjacent_hourly_intervals_are_allowed_and_two_reactivations_have_one_winner()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        await repositories.Configs.ReplaceAsync(new() { Id = "default", SessionMode = ShootingRangeSessionMode.Hourly });
        var service = new ShootingRangeService(repositories);
        var first = await service.CreateBookingAsync("user-1", new("bay-1", new(2030, 1, 12), "09:00", ""), default);
        await service.CreateBookingAsync("user-2", new("bay-1", new(2030, 1, 12), "10:00", ""), default);
        await repositories.Bookings.ReplaceAsync(first with { Status = ShootingRangeBookingStatus.Rejected });
        var other = await repositories.Bookings.InsertAsync(first with { Id = "other", Status = ShootingRangeBookingStatus.Cancelled });
        async Task<bool> TryActivate(ShootingRangeBooking booking)
        {
            try { await repositories.Bookings.ReplaceAsync(booking with { Status = ShootingRangeBookingStatus.Approved }); return true; }
            catch (InvalidOperationException) { return false; }
        }
        (await Task.WhenAll(Task.Run(() => TryActivate(first)), Task.Run(() => TryActivate(other)))).Count(success => success).Should().Be(1);
    }

    [Theory]
    [InlineData(ShootingRangeAccountStatus.Disabled, true, false)]
    [InlineData(ShootingRangeAccountStatus.Approved, false, false)]
    [InlineData(ShootingRangeAccountStatus.Approved, true, true)]
    public async Task Admin_mutation_checks_current_actor_inside_guard(ShootingRangeAccountStatus status, bool admin, bool forced)
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Users.ReplaceAsync((await repositories.Users.GetAsync("user-1"))! with { Status = status, IsAdmin = admin, ForcePasswordChange = forced });
        var action = () => new ShootingRangeService(repositories).AdminMutationAsync("user-1", () => repositories.Configs.ReplaceAsync(new() { Id = "default" }), default);
        await action.Should().ThrowAsync<UnauthorizedAccessException>();
        (await repositories.Configs.GetAllAsync()).Should().BeEmpty();
    }

    private static async Task<MockShootingRangeRepositorySet> CreateRepositoriesAsync()
    {
        var repositories = new MockShootingRangeRepositorySet(new FixedClock());
        foreach (var id in new[] { "user-1", "user-2", "other-user", "approved-user" })
            await repositories.Users.InsertAsync(new ShootingRangeUser { Id = id, Username = id, Status = ShootingRangeAccountStatus.Approved });
        return repositories;
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
