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
    [InlineData(60, 22)]
    [InlineData(120, 8)]
    public void Mixed_mode_offers_whole_windows_and_every_contiguous_increment_block(int minutes, int expectedCount)
    {
        var config = new ShootingRangeConfig { SessionMode = ShootingRangeSessionMode.Mixed, HourlyMinutes = minutes };
        var sessions = ShootingRangeService.GenerateSessions(config, new(2030, 1, 12));
        sessions.Should().HaveCount(expectedCount);
        sessions.Should().Contain(session => session.PeriodKey == "morning");
        sessions.Should().Contain(session => session.PeriodKey == "afternoon");
        sessions.Should().Contain(session => session.PeriodKey == (minutes == 60 ? "09:00-12:00" : "09:00-13:00"));
        sessions.Should().NotContain(session => session.PeriodKey.Contains("13:00-14:00", StringComparison.Ordinal));
        sessions.Select(session => session.PeriodKey).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Mixed_mode_resolves_stable_interval_keys_and_rejects_stale_or_unsupported_duration()
    {
        var config = new ShootingRangeConfig { SessionMode = ShootingRangeSessionMode.Mixed, HourlyMinutes = 120 };
        var interval = ShootingRangeService.ResolvePeriod(config, new(2030, 1, 12), "09:00-13:00");
        (interval.EndUtc - interval.StartUtc).Should().Be(TimeSpan.FromHours(4));
        var stale = () => ShootingRangeService.ResolvePeriod(config with { MorningEnd = new(12, 0, 0) }, new(2030, 1, 12), "09:00-13:00");
        stale.Should().Throw<ArgumentException>();
        var invalid = () => ShootingRangeService.ValidateConfig(config with { HourlyMinutes = 90 });
        invalid.Should().Throw<ArgumentException>();
        var legacyInvalid = () => ShootingRangeService.ValidateConfig(new() { SessionMode = ShootingRangeSessionMode.Hourly, HourlyMinutes = 120 });
        legacyInvalid.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Mixed_bookings_require_current_expected_utc_bounds()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        await repositories.Configs.ReplaceAsync(new() { Id = "default", SessionMode = ShootingRangeSessionMode.Mixed });
        var service = new ShootingRangeService(repositories);
        var date = new DateOnly(2030, 1, 12);
        var missingBounds = () => service.CreateBookingAsync("user-1", new("bay-1", date, "09:00-11:00", ""), default);
        await missingBounds.Should().ThrowAsync<InvalidOperationException>();
        var (start, end) = ShootingRangeService.ResolvePeriod(
            new() { SessionMode = ShootingRangeSessionMode.Mixed }, date, "09:00-11:00");
        var staleBounds = () => service.CreateBookingAsync("user-1",
            new("bay-1", date, "09:00-11:00", "", start.AddHours(1), end.AddHours(1)), default);
        await staleBounds.Should().ThrowAsync<InvalidOperationException>();
        (await repositories.Bookings.GetAllAsync()).Should().BeEmpty();
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
    public async Task Exact_shared_booking_allows_three_members_when_each_joins_through_an_active_friend()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        await repositories.Bays.InsertAsync(new() { Id = "bay-2", Code = "02" });
        var service = new ShootingRangeService(repositories);
        var request = new BookingRequest("bay-1", new(2030, 1, 12), "morning", "private note");
        var anchor = await service.CreateBookingAsync("user-1", request, default);
        await service.CreateBookingAsync("user-1", request with { BayId = "bay-2" }, default);
        (await service.GetAvailabilityAsync(request.LocalDate, "morning", default, "user-2")).Bays.Should().BeEmpty();

        await AcceptFriendRequestAsync(service, "user-2", "user-1");
        var availability = await service.GetAvailabilityAsync(request.LocalDate, "morning", default, "user-2");
        availability.Bays.Select(bay => bay.Id).Should().Contain("bay-1", "bay-2");
        availability.FriendBookings.Should().BeEquivalentTo(new[]
        {
            new FriendBookingDto("bay-1", "user-1", ShootingRangeBookingStatus.Pending),
            new FriendBookingDto("bay-2", "user-1", ShootingRangeBookingStatus.Pending)
        });

        var second = await service.CreateBookingAsync("user-2", request, default);
        (await service.GetAvailabilityAsync(request.LocalDate, "morning", default, "other-user")).Bays.Should().BeEmpty();
        await AcceptFriendRequestAsync(service, "other-user", "user-2");
        (await service.GetAvailabilityAsync(request.LocalDate, "morning", default, "other-user")).Bays
            .Should().Contain(bay => bay.Id == "bay-1");
        var third = await service.CreateBookingAsync("other-user", request, default);
        await AcceptFriendRequestAsync(service, "approved-user", "other-user");
        var inactive = await repositories.Bookings.InsertAsync(new ShootingRangeBooking
        {
            Id = "generic-reactivation",
            UserId = "approved-user",
            BayId = "bay-1",
            LocalDate = request.LocalDate,
            PeriodKey = "morning",
            StartUtc = anchor.StartUtc,
            EndUtc = anchor.EndUtc,
            Status = ShootingRangeBookingStatus.Cancelled
        });
        await repositories.Bookings.ReplaceAsync(inactive with { Status = ShootingRangeBookingStatus.Pending });
        (await repositories.Bookings.GetAllAsync())
            .Where(booking => booking.BayId == "bay-1" && booking.StartUtc == anchor.StartUtc)
            .Should().HaveCount(4);
        second.Request.Should().Be("private note");
        third.Status.Should().Be(ShootingRangeBookingStatus.Pending);
    }

    [Fact]
    public async Task Shared_slots_reject_partial_overlap_duplicate_participant_and_generic_bypass()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        await repositories.Configs.ReplaceAsync(new() { Id = "default", SessionMode = ShootingRangeSessionMode.Mixed });
        var service = new ShootingRangeService(repositories);
        var date = new DateOnly(2030, 1, 12);
        var firstStart = new DateTime(2030, 1, 12, 8, 0, 0, DateTimeKind.Utc);
        var firstEnd = firstStart.AddHours(2);
        var anchor = await service.CreateBookingAsync("user-1", new("bay-1", date, "09:00-11:00", "", firstStart, firstEnd), default);
        await AcceptFriendRequestAsync(service, "user-2", "user-1");
        var partialStart = firstStart.AddHours(1);
        var partial = () => service.CreateBookingAsync("user-2", new("bay-1", date, "10:00-12:00", "", partialStart, partialStart.AddHours(2)), default);
        await partial.Should().ThrowAsync<InvalidOperationException>();
        var duplicate = () => repositories.Bookings.InsertAsync(anchor with { Id = "duplicate" });
        await duplicate.Should().ThrowAsync<InvalidOperationException>();

        var inactive = await repositories.Bookings.InsertAsync(anchor with { Id = "inactive", UserId = "user-2", Status = ShootingRangeBookingStatus.Cancelled });
        await service.RemoveFriendAsync("user-2", "user-1", default);
        var reactivate = () => repositories.Bookings.ReplaceAsync(inactive with { Status = ShootingRangeBookingStatus.Approved });
        await reactivate.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Approval_of_existing_pending_shared_reservation_survives_friend_removal()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        await repositories.Users.ReplaceAsync((await repositories.Users.GetAsync("user-1"))! with { IsAdmin = true });
        var service = new ShootingRangeService(repositories);
        var request = new BookingRequest("bay-1", new(2030, 1, 12), "morning", "");
        await service.CreateBookingAsync("user-1", request, default);
        await AcceptFriendRequestAsync(service, "user-2", "user-1");
        var join = await service.CreateBookingAsync("user-2", request, default);
        await service.RemoveFriendAsync("user-2", "user-1", default);
        (await service.DecideBookingAsync("user-1", join.Id, true, default)).Status.Should().Be(ShootingRangeBookingStatus.Approved);
    }

    [Fact]
    public async Task Friend_requests_require_recipient_consent_and_support_decline_and_cancel()
    {
        var repositories = await CreateRepositoriesAsync();
        var service = new ShootingRangeService(repositories);

        await service.RequestFriendAsync("user-1", "user-2", default);
        await service.RequestFriendAsync("user-1", " USER-2 ", default);
        (await repositories.Friendships.GetAllAsync()).Should().ContainSingle()
            .Which.Status.Should().Be(ShootingRangeFriendshipStatus.Pending);
        (await service.GetFriendsAsync("user-1", default)).Should().BeEmpty();

        var outgoing = await service.GetFriendRequestsAsync("user-1", default);
        var incoming = await service.GetFriendRequestsAsync("user-2", default);
        outgoing.Outgoing.Should().ContainSingle().Which.Username.Should().Be("user-2");
        outgoing.Incoming.Should().BeEmpty();
        incoming.Incoming.Should().ContainSingle().Which.Username.Should().Be("user-1");
        incoming.Outgoing.Should().BeEmpty();
        var requestId = incoming.Incoming.Single().Id;

        var requesterAccept = () => service.RespondToFriendRequestAsync("user-1", requestId, true, default);
        await requesterAccept.Should().ThrowAsync<InvalidOperationException>();
        var requesterDecline = () => service.RespondToFriendRequestAsync("user-1", requestId, false, default);
        await requesterDecline.Should().ThrowAsync<InvalidOperationException>();
        var recipientCancel = () => service.CancelFriendRequestAsync("user-2", requestId, default);
        await recipientCancel.Should().ThrowAsync<InvalidOperationException>();
        await service.RespondToFriendRequestAsync("user-2", requestId, false, default);
        (await repositories.Friendships.GetAllAsync()).Should().BeEmpty();

        await service.RequestFriendAsync("user-1", "user-2", default);
        var cancelledId = (await service.GetFriendRequestsAsync("user-1", default)).Outgoing.Single().Id;
        var recipientCancels = () => service.CancelFriendRequestAsync("user-2", cancelledId, default);
        await recipientCancels.Should().ThrowAsync<InvalidOperationException>();
        await service.CancelFriendRequestAsync("user-1", cancelledId, default);
        (await repositories.Friendships.GetAllAsync()).Should().BeEmpty();

        (await service.GetFriendsAsync("user-1", default)).Should().BeEmpty();
    }

    [Fact]
    public async Task Opposite_pending_request_does_not_auto_accept()
    {
        var repositories = await CreateRepositoriesAsync();
        var service = new ShootingRangeService(repositories);
        await service.RequestFriendAsync("user-1", "user-2", default);

        var oppositeRequest = () => service.RequestFriendAsync("user-2", "user-1", default);
        await oppositeRequest.Should().ThrowAsync<InvalidOperationException>();
        (await repositories.Friendships.GetAllAsync()).Should().ContainSingle()
            .Which.Status.Should().Be(ShootingRangeFriendshipStatus.Pending);
        (await service.GetFriendsAsync("user-2", default)).Should().BeEmpty();
    }

    [Fact]
    public async Task Friend_request_does_not_reveal_cross_tenant_usernames()
    {
        var repositories = await CreateRepositoriesAsync();
        const string secondTenantId = "second-test-tenant";
        await repositories.Tenants.InsertAsync(new ShootingRangeTenant
        {
            Id = secondTenantId,
            Slug = "second-test-range",
            Name = "Second test range",
            ClientOrigin = "https://second-test-range.example"
        });

        using (repositories.TenantContext.BeginScope(secondTenantId))
            await repositories.Users.InsertAsync(new ShootingRangeUser
            {
                Id = "second-only-user",
                Username = "second-only",
                Status = ShootingRangeAccountStatus.Approved
            });

        var service = new ShootingRangeService(repositories);
        (await repositories.Users.FindByUsernameAsync("second-only")).Should().BeNull();
        var unknownUser = () => service.RequestFriendAsync("user-1", "unknown", default);
        var crossTenantUser = () => service.RequestFriendAsync("user-1", "second-only", default);
        await unknownUser.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unable to create friend request.");
        await crossTenantUser.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unable to create friend request.");
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
    public void Sql_entities_preserve_numeric_modes_and_string_identifiers()
    {
        var config = new ShootingRangeConfig { Id = "default" };
        config.Id.Should().Be("default");
        config.SessionMode.Should().Be(ShootingRangeSessionMode.Periods);
        config.ContinuousStart.Should().BeNull();
        var id = "67f4b52d57af68ac3ec480c1";
        new ShootingRangeBay { Id = id }.Id.Should().Be(id);
        ShootingRangeIds.NewId().Should().MatchRegex("^[0-9a-f]{24}$");
        ((int)ShootingRangeSessionMode.Hourly).Should().Be(1);
        ((int)ShootingRangeSessionMode.HourlyContinuous).Should().Be(2);
        ((int)ShootingRangeSessionMode.Mixed).Should().Be(3);
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
        await action.Should().ThrowAsync<InvalidOperationException>();
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
    public void Mixed_sessions_skip_invalid_and_ambiguous_dst_windows()
    {
        var config = new ShootingRangeConfig
        {
            TimeZone = "Europe/Rome",
            SessionMode = ShootingRangeSessionMode.Mixed,
            MorningStart = new(2, 0, 0),
            MorningEnd = new(4, 0, 0),
            AfternoonStart = new(5, 0, 0),
            AfternoonEnd = new(8, 0, 0)
        };
        foreach (var date in new[] { new DateOnly(2030, 3, 31), new DateOnly(2030, 10, 27) })
        {
            var sessions = ShootingRangeService.GenerateSessions(config, date);
            sessions.Should().NotContain(session => session.PeriodKey == "morning");
            sessions.Should().NotContain(session => session.PeriodKey.StartsWith("02:00-", StringComparison.Ordinal));
        }
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

    [Fact]
    public async Task Message_status_can_be_corrected_and_guarded_replies_preserve_thread_history()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Users.ReplaceAsync((await repositories.Users.GetAsync("user-1"))! with { IsAdmin = true });
        var thread = await repositories.Threads.InsertAsync(new()
        {
            Id = "message-thread",
            UserId = "user-2",
            Subject = "Question",
            Messages = [new("user-2", false, "Original message", DateTime.UtcNow)]
        });
        var service = new ShootingRangeService(repositories);

        await service.DecideThreadStatusAsync("user-1", thread.Id, ShootingRangeThreadStatus.InProgress, default);
        await service.DecideThreadStatusAsync("user-1", thread.Id, ShootingRangeThreadStatus.Completed, default);
        await service.DecideThreadStatusAsync("user-1", thread.Id, ShootingRangeThreadStatus.Refused, default);
        await service.DecideThreadStatusAsync("user-1", thread.Id, ShootingRangeThreadStatus.InProgress, default);
        var replied = await service.ReplyToThreadAsync("user-2", thread.Id, "Additional details", default);

        replied.Status.Should().Be(ShootingRangeThreadStatus.InProgress);
        replied.Messages.Should().HaveCount(2);
        replied.Messages[0].Text.Should().Be("Original message");
        replied.Messages[1].AuthorUserId.Should().Be("user-2");
        replied.Messages[1].FromAdmin.Should().BeFalse();
        replied.Messages[1].Text.Should().Be("Additional details");

        var nonAdminDecision = () => service.DecideThreadStatusAsync(
            "user-2", thread.Id, ShootingRangeThreadStatus.Completed, default);
        await nonAdminDecision.Should().ThrowAsync<UnauthorizedAccessException>();
        var invalidStatus = () => service.DecideThreadStatusAsync("user-1", thread.Id, ShootingRangeThreadStatus.Pending, default);
        await invalidStatus.Should().ThrowAsync<ArgumentException>();
        var undefinedStatus = () => service.DecideThreadStatusAsync("user-1", thread.Id, (ShootingRangeThreadStatus)99, default);
        await undefinedStatus.Should().ThrowAsync<ArgumentException>();
        var missingThread = () => service.DecideThreadStatusAsync("user-1", "missing", ShootingRangeThreadStatus.Completed, default);
        await missingThread.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task User_can_cancel_only_their_own_active_booking_before_its_start_and_admin_can_restore_it()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        await repositories.Users.ReplaceAsync((await repositories.Users.GetAsync("user-1"))! with { IsAdmin = true });
        var service = new ShootingRangeService(repositories);
        var booking = await service.CreateBookingAsync("user-1", new("bay-1", new(2030, 1, 12), "morning", ""), default);

        var clock = (FixedClock)repositories.Clock;
        clock.SetUtcNow(booking.StartUtc);
        var started = () => service.CancelBookingAsync("user-1", booking.Id, default);
        await started.Should().ThrowAsync<InvalidOperationException>();

        clock.SetUtcNow(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var otherUser = () => service.CancelBookingAsync("user-2", booking.Id, default);
        await otherUser.Should().ThrowAsync<KeyNotFoundException>();

        var cancelled = await service.CancelBookingAsync("user-1", booking.Id, default);
        cancelled.Status.Should().Be(ShootingRangeBookingStatus.Cancelled);
        var rejectCancelled = () => service.DecideBookingAsync("user-1", booking.Id, false, default);
        await rejectCancelled.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A cancelled booking can only be restored by approval.");
        var restored = await service.DecideBookingAsync("user-1", booking.Id, true, default);
        restored.Status.Should().Be(ShootingRangeBookingStatus.Approved);
    }

    [Fact]
    public async Task Booking_lists_include_yesterday_in_field_time_but_exclude_older_dates()
    {
        var repositories = await CreateRepositoriesAsync();
        ((FixedClock)repositories.Clock).SetUtcNow(new DateTimeOffset(2029, 12, 31, 23, 30, 0, TimeSpan.Zero));
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        await repositories.Users.ReplaceAsync((await repositories.Users.GetAsync("user-1"))! with { IsAdmin = true });
        var service = new ShootingRangeService(repositories);
        foreach (var (id, date, userId) in new[]
        {
            ("today", new DateOnly(2030, 1, 1), "user-1"),
            ("yesterday", new DateOnly(2029, 12, 31), "user-1"),
            ("older", new DateOnly(2029, 12, 30), "user-1"),
            ("other-user", new DateOnly(2030, 1, 1), "user-2")
        })
        {
            var start = date.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc);
            await repositories.Bookings.InsertAsync(new()
            {
                Id = id,
                UserId = userId,
                BayId = "bay-1",
                LocalDate = date,
                PeriodKey = "morning",
                StartUtc = start,
                EndUtc = start.AddHours(1),
                Status = ShootingRangeBookingStatus.Cancelled
            });
        }

        (await service.GetUserBookingsAsync("user-1", default))
            .Select(booking => booking.Id)
            .Should().BeEquivalentTo("today", "yesterday");
        (await service.GetAdminBookingsAsync("user-1", default))
            .Select(booking => booking.Id)
            .Should().BeEquivalentTo("today", "yesterday", "other-user");
    }

    [Fact]
    public async Task Admin_pending_count_includes_only_future_pending_bookings()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        await repositories.Users.ReplaceAsync((await repositories.Users.GetAsync("user-1"))! with { IsAdmin = true });
        var service = new ShootingRangeService(repositories);
        var pending = await service.CreateBookingAsync("user-1", new("bay-1", new(2030, 1, 12), "morning", ""), default);
        await service.CreateBookingAsync("user-2", new("bay-1", new(2030, 1, 12), "afternoon", ""), default);
        (await service.GetPendingBookingCountAsync("user-1", default)).Should().Be(2);
        await service.DecideBookingAsync("user-1", pending.Id, true, default);
        (await service.GetPendingBookingCountAsync("user-1", default)).Should().Be(1);
    }

    [Fact]
    public async Task Admin_cannot_approve_a_booking_after_its_session_starts()
    {
        var repositories = await CreateRepositoriesAsync();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        await repositories.Users.ReplaceAsync((await repositories.Users.GetAsync("user-1"))! with { IsAdmin = true });
        var service = new ShootingRangeService(repositories);
        var booking = await service.CreateBookingAsync("user-1", new("bay-1", new(2030, 1, 12), "morning", ""), default);
        ((FixedClock)repositories.Clock).SetUtcNow(booking.StartUtc);

        var approve = () => service.DecideBookingAsync("user-1", booking.Id, true, default);
        await approve.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A booking cannot be approved after its session has started.");
    }

    [Fact]
    public async Task Bay_validation_requires_nonnegative_targets_and_paired_positive_dimensions()
    {
        var repositories = await CreateRepositoriesAsync();
        var bay = new ShootingRangeBay { Id = "bay-1", Code = "01", Targets = 2, Height = 1.5m, Width = 2.25m };
        await repositories.Bays.InsertAsync(bay);

        var negativeTargets = () => repositories.Bays.ReplaceAsync(bay with { Targets = -1 });
        await negativeTargets.Should().ThrowAsync<ArgumentException>();
        var oneDimension = () => repositories.Bays.ReplaceAsync(bay with { Width = null });
        await oneDimension.Should().ThrowAsync<ArgumentException>();
        var zeroHeight = () => repositories.Bays.ReplaceAsync(bay with { Height = 0 });
        await zeroHeight.Should().ThrowAsync<ArgumentException>();
        var excessivePrecision = () => repositories.Bays.ReplaceAsync(bay with { Height = 1.555m, Width = 2.25m });
        await excessivePrecision.Should().ThrowAsync<ArgumentException>();
    }

    private static async Task<MockShootingRangeRepositorySet> CreateRepositoriesAsync()
    {
        var repositories = new MockShootingRangeRepositorySet(new FixedClock());
        foreach (var id in new[] { "user-1", "user-2", "other-user", "approved-user" })
            await repositories.Users.InsertAsync(new ShootingRangeUser { Id = id, Username = id, Status = ShootingRangeAccountStatus.Approved });
        return repositories;
    }

    private static async Task AcceptFriendRequestAsync(
        ShootingRangeService service,
        string requesterId,
        string recipientId)
    {
        await service.RequestFriendAsync(requesterId, recipientId, default);
        var request = (await service.GetFriendRequestsAsync(recipientId, default)).Incoming.Single();
        await service.RespondToFriendRequestAsync(recipientId, request.Id, true, default);
    }

    private sealed class FixedClock : TimeProvider
    {
        private DateTimeOffset now = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        public void SetUtcNow(DateTimeOffset value) => now = value;
    }
}
