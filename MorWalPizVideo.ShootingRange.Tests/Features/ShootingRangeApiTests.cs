using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MorWalPizVideo.ShootingRange.Contracts;
using MorWalPizVideo.ShootingRange.Controllers;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Security;
using MorWalPizVideo.ShootingRange.Services;

namespace MorWalPizVideo.ShootingRange.Tests.Features;

[Trait("Category", "TestGroup:ShootingRange")]
public sealed class ShootingRangeApiTests
{
    private const string Origin = "https://range-spa-bjeqb5gwggf0hfaj.westeurope-01.azurewebsites.net";
    private const string Password = "test-password-only-123";

    [Fact]
    public async Task In_memory_database_flag_uses_the_ef_provider_and_isolates_factories()
    {
        await using var factory = new Factory(seedInMemoryAdmin: true);
        using var firstScope = factory.Services.CreateScope();
        var firstRepositories = firstScope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();
        firstScope.ServiceProvider.GetRequiredService<ShootingRangeDbContext>().Database.IsInMemory().Should().BeTrue();
        firstRepositories.Should().BeOfType<SqlServerShootingRangeRepositorySet>();
        (await firstRepositories.MechanismTypes.GetAllAsync()).Should().HaveCountGreaterThanOrEqualTo(3);
        (await firstRepositories.Bays.GetAllAsync()).Should().HaveCountGreaterThanOrEqualTo(4);
        (await firstRepositories.Users.GetAllAsync()).Should().Contain(user => user.Status == ShootingRangeAccountStatus.Pending);
        (await firstRepositories.Bookings.GetAllAsync()).Should().Contain(booking => booking.Status == ShootingRangeBookingStatus.Pending);
        (await firstRepositories.Bookings.GetAllAsync()).Should().Contain(booking => booking.Status == ShootingRangeBookingStatus.Approved);
        (await firstRepositories.Exceptions.GetAllAsync()).Should().Contain(closure => closure.BayId == null);
        (await firstRepositories.Exceptions.GetAllAsync()).Should().Contain(closure => closure.BayId != null);
        var sampleCounts = (
            Types: (await firstRepositories.MechanismTypes.GetAllAsync()).Count,
            Bays: (await firstRepositories.Bays.GetAllAsync()).Count,
            Users: (await firstRepositories.Users.GetAllAsync()).Count,
            Bookings: (await firstRepositories.Bookings.GetAllAsync()).Count,
            Closures: (await firstRepositories.Exceptions.GetAllAsync()).Count);
        await new InMemorySampleDataSeeder().SeedAsync(firstRepositories, CancellationToken.None);
        (await firstRepositories.MechanismTypes.GetAllAsync()).Should().HaveCount(sampleCounts.Types);
        (await firstRepositories.Bays.GetAllAsync()).Should().HaveCount(sampleCounts.Bays);
        (await firstRepositories.Users.GetAllAsync()).Should().HaveCount(sampleCounts.Users);
        (await firstRepositories.Bookings.GetAllAsync()).Should().HaveCount(sampleCounts.Bookings);
        (await firstRepositories.Exceptions.GetAllAsync()).Should().HaveCount(sampleCounts.Closures);
        var seededAdmin = await firstRepositories.Users.FindByUsernameAsync("admin");
        seededAdmin.Should().NotBeNull();
        seededAdmin!.Status.Should().Be(ShootingRangeAccountStatus.Approved);
        seededAdmin.IsAdmin.Should().BeTrue();
        seededAdmin.ForcePasswordChange.Should().BeTrue();
        var passwordParts = seededAdmin.PasswordHash.Split(':', 2);
        PasswordHashing.VerifyPassword(factory.InMemoryAdminPassword!, passwordParts[0], passwordParts[1]).Should().BeTrue();
        await firstRepositories.Users.InsertAsync(new() { Id = "memory-user", Username = "memory-user" });

        using var secondScope = factory.Services.CreateScope();
        var secondRepositories = secondScope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();
        (await secondRepositories.Users.GetAsync("memory-user")).Should().NotBeNull();

        await using var isolatedFactory = new Factory();
        using var isolatedScope = isolatedFactory.Services.CreateScope();
        var isolatedRepositories = isolatedScope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();
        (await isolatedRepositories.Users.GetAsync("memory-user")).Should().BeNull();
    }

    [Fact]
    public async Task Mock_repository_does_not_receive_in_memory_sample_data()
    {
        await using var factory = new Factory(enableInMemoryDb: false, useMock: true);
        using var scope = factory.Services.CreateScope();
        var repositories = scope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();

        (await repositories.MechanismTypes.GetAllAsync()).Should().BeEmpty();
        (await repositories.Bays.GetAllAsync()).Should().BeEmpty();
        (await repositories.Bookings.GetAllAsync()).Should().BeEmpty();
        (await repositories.Exceptions.GetAllAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(Origin, true)]
    [InlineData("https://evil.example", false)]
    [InlineData(Origin + ".evil.example", false)]
    [InlineData("http://range-spa-bjeqb5gwggf0hfaj.westeurope-01.azurewebsites.net", false)]
    [InlineData(Origin + "/", false)]
    public async Task Cors_allows_only_exact_credentialed_origin(string origin, bool allowed)
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/shooting-range/bookings");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "X-CSRF-TOKEN,Content-Type");
        using var response = await client.SendAsync(request);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().Be(allowed);
        if (allowed)
        {
            response.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal(Origin);
            response.Headers.GetValues("Access-Control-Allow-Credentials").Should().Equal("true");
        }
    }

    [Fact]
    public async Task Admin_contract_preserves_safe_fields_and_requires_current_admin_and_csrf()
    {
        await using var factory = new Factory();
        using var client = await LoginAsync(factory, true);
        var users = await client.GetFromJsonAsync<JsonElement[]>("/api/shooting-range/admin/users");
        var adminDto = users!.Single(user => user.GetProperty("username").GetString() == "account");
        adminDto.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("id", "creationDateTime", "username", "firstName", "lastName", "status", "isAdmin", "forcePasswordChange");
        adminDto.GetProperty("status").GetInt32().Should().Be(1);
        using var withoutCsrf = await client.PutAsJsonAsync("/api/shooting-range/admin/config", new ShootingRangeConfig());
        withoutCsrf.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await SetCsrfAsync(client);
        var config = new ShootingRangeConfig { SessionMode = ShootingRangeSessionMode.HourlyContinuous, ContinuousStart = new(9, 30, 0), ContinuousEnd = new(13, 0, 0) };
        using var saved = await client.PutAsJsonAsync("/api/shooting-range/admin/config", config);
        saved.StatusCode.Should().Be(HttpStatusCode.OK);
        var loaded = await client.GetFromJsonAsync<ShootingRangeConfig>("/api/shooting-range/admin/config");
        loaded!.Id.Should().Be("default");
        loaded.SessionMode.Should().Be(ShootingRangeSessionMode.HourlyContinuous);
        using var invalid = await client.PutAsJsonAsync("/api/shooting-range/admin/config", config with { HourlyMinutes = 30 });
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var closureResponse = await client.PostAsJsonAsync("/api/shooting-range/admin/exceptions", new ShootingRangeException { LocalDate = new(2030, 1, 12), IsClosed = true, Reason = "test closure" });
        closureResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closure = await closureResponse.Content.ReadFromJsonAsync<ShootingRangeException>();
        using var reopened = await client.PutAsJsonAsync($"/api/shooting-range/admin/exceptions/{closure!.Id}", closure with { IsClosed = false });
        reopened.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetFromJsonAsync<ShootingRangeException[]>("/api/shooting-range/admin/exceptions"))!
            .Single(item => item.Id == closure.Id).IsClosed.Should().BeFalse();
        using var scope = factory.Services.CreateScope();
        var repositories = scope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();
        var admin = (await repositories.Users.GetAllAsync()).Single(user => user.Username == "account");
        await repositories.Users.ReplaceAsync(admin with { Status = ShootingRangeAccountStatus.Disabled });
        using var staleCookie = await client.PutAsJsonAsync("/api/shooting-range/admin/config", config);
        staleCookie.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_mechanism_catalogue_requires_admin_and_csrf_and_bays_use_numeric_pepper()
    {
        await using var factory = new Factory();
        using var client = await LoginAsync(factory, true);
        using var withoutCsrf = await client.PostAsJsonAsync(
            "/api/shooting-range/admin/mechanism-types",
            new CreateMechanismTypeRequest("Bersaglio dinamico"));
        withoutCsrf.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await SetCsrfAsync(client);
        using var created = await client.PostAsJsonAsync(
            "/api/shooting-range/admin/mechanism-types",
            new CreateMechanismTypeRequest("  Bersaglio dinamico  "));
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var mechanismType = await created.Content.ReadFromJsonAsync<MechanismTypeDto>();
        mechanismType!.Name.Should().Be("Bersaglio dinamico");
        using var duplicate = await client.PostAsJsonAsync(
            "/api/shooting-range/admin/mechanism-types",
            new CreateMechanismTypeRequest("bersaglio DINAMICO"));
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var bay = new ShootingRangeBay
        {
            Id = "type-test-bay",
            Code = "Type test",
            Pepper = 3,
            Targets = 4,
            Height = 1.5m,
            Width = 2.25m,
            Mechanisms = [new(mechanismType.Name, 2)]
        };
        using var saved = await client.PutAsJsonAsync($"/api/shooting-range/admin/bays/{bay.Id}", bay);
        saved.StatusCode.Should().Be(HttpStatusCode.OK);
        var savedBay = (await saved.Content.ReadFromJsonAsync<ShootingRangeBay>())!;
        savedBay.Pepper.Should().Be(3);
        savedBay.Targets.Should().Be(4);
        savedBay.Height.Should().Be(1.5m);
        savedBay.Width.Should().Be(2.25m);

        using var negative = await client.PutAsJsonAsync(
            $"/api/shooting-range/admin/bays/{bay.Id}",
            bay with { Pepper = -1 });
        negative.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var negativeTargets = await client.PutAsJsonAsync(
            $"/api/shooting-range/admin/bays/{bay.Id}",
            bay with { Targets = -1 });
        negativeTargets.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var incompleteDimensions = await client.PutAsJsonAsync(
            $"/api/shooting-range/admin/bays/{bay.Id}",
            bay with { Width = null });
        incompleteDimensions.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var unknown = await client.PutAsJsonAsync(
            $"/api/shooting-range/admin/bays/{bay.Id}",
            bay with { Mechanisms = [new("not in catalogue", 1)] });
        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var oldClientPayload = new StringContent(
            """{"id":"type-test-bay","code":"Type test","mechanisms":[],"partitions":0,"plates":0,"pepper":3,"status":0,"description":"","reservedUntilUtc":null,"whitelistUserIds":[]}""",
            System.Text.Encoding.UTF8,
            "application/json");
        using var rejectedOldPayload = await client.PutAsync(
            $"/api/shooting-range/admin/bays/{bay.Id}",
            oldClientPayload);
        rejectedOldPayload.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetFromJsonAsync<ShootingRangeBay[]>("/api/shooting-range/admin/bays"))!
            .Single(saved => saved.Id == bay.Id).Targets.Should().Be(4);
    }

    [Fact]
    public async Task Future_booking_cancellation_requires_csrf_retains_history_and_updates_admin_count()
    {
        await using var factory = new Factory();
        using var user = await LoginAsync(factory, false);
        using var scope = factory.Services.CreateScope();
        var repositories = scope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();
        await repositories.Bays.InsertAsync(new() { Id = "cancel-test-bay", Code = "Cancel test" });
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        while (date.DayOfWeek != DayOfWeek.Saturday) date = date.AddDays(1);
        using var admin = await LoginAsync(factory, true, username: "administrator");
        var baselineCount = (await admin.GetFromJsonAsync<PendingBookingCountDto>(
            "/api/shooting-range/admin/bookings/pending-count"))!.Count;

        await SetCsrfAsync(user);
        using var created = await user.PostAsJsonAsync(
            "/api/shooting-range/bookings",
            new BookingRequest("cancel-test-bay", date, "morning", "test"));
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var booking = (await created.Content.ReadFromJsonAsync<ShootingRangeBooking>())!;

        var pendingCount = await admin.GetFromJsonAsync<PendingBookingCountDto>(
            "/api/shooting-range/admin/bookings/pending-count");
        pendingCount!.Count.Should().Be(baselineCount + 1);

        user.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        using var missingCsrf = await user.PostAsync(
            $"/api/shooting-range/bookings/{booking.Id}/cancel",
            null);
        missingCsrf.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await SetCsrfAsync(user);
        using var cancelled = await user.PostAsync(
            $"/api/shooting-range/bookings/{booking.Id}/cancel",
            null);
        cancelled.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await user.GetFromJsonAsync<ShootingRangeBooking[]>("/api/shooting-range/bookings/mine"))!
            .Single(item => item.Id == booking.Id).Status.Should().Be(ShootingRangeBookingStatus.Cancelled);
        (await admin.GetFromJsonAsync<PendingBookingCountDto>(
            "/api/shooting-range/admin/bookings/pending-count"))!.Count.Should().Be(baselineCount);

        await SetCsrfAsync(user);
        using var repeated = await user.PostAsync(
            $"/api/shooting-range/bookings/{booking.Id}/cancel",
            null);
        repeated.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Normal_user_cannot_admin_but_can_discover_book_and_receive_conflict()
    {
        await using var factory = new Factory();
        using var client = await LoginAsync(factory, false);
        using var scope = factory.Services.CreateScope();
        var repositories = scope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        using var forbidden = await client.GetAsync("/api/shooting-range/admin/users");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var forbiddenTypes = await client.GetAsync("/api/shooting-range/admin/mechanism-types");
        forbiddenTypes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var forbiddenCount = await client.GetAsync("/api/shooting-range/admin/bookings/pending-count");
        forbiddenCount.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        while (date.DayOfWeek != DayOfWeek.Saturday) date = date.AddDays(1);
        var sessions = await client.GetFromJsonAsync<SessionsDto>($"/api/shooting-range/sessions?date={date:yyyy-MM-dd}");
        sessions!.Sessions.Select(session => session.PeriodKey).Should().Equal("morning", "afternoon");
        await SetCsrfAsync(client);
        var booking = new BookingRequest("bay-1", date, "morning", "test request");
        using var created = await client.PostAsJsonAsync("/api/shooting-range/bookings", booking);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("creationDateTime").ValueKind.Should().Be(JsonValueKind.String);
        body.GetProperty("status").GetInt32().Should().Be(0);
        using var conflict = await client.PostAsJsonAsync("/api/shooting-range/bookings", booking);
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var invalid = await client.PostAsJsonAsync("/api/shooting-range/bookings", booking with { PeriodKey = "09:00" });
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var missing = await client.PostAsJsonAsync("/api/shooting-range/bookings", booking with { BayId = "missing" });
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_requires_prelogin_csrf_and_refresh_after_identity_change()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var missing = await client.PostAsJsonAsync("/api/shooting-range/auth/login", new ShootingRangeLoginRequest("unknown", Password));
        missing.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var authenticated = await LoginAsync(factory, false, keepPreloginToken: true);
        using var withoutToken = await authenticated.PostAsync("/api/shooting-range/auth/logout", null);
        withoutToken.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await SetCsrfAsync(authenticated);
        using var logout = await authenticated.PostAsync("/api/shooting-range/auth/logout", null);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Csrf_accepts_https_forwarded_by_the_app_service_proxy()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("http://localhost") });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/shooting-range/csrf");
        request.Headers.Add("X-Forwarded-Proto", "https");

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Set-Cookie").Should().ContainSingle(cookie => cookie.Contains("Secure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task New_passwords_require_seven_characters_for_registration_change_and_reset()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var shortRegistration = new RegisterShootingRangeUserRequest("new-user", "123456", "New", "User");
        using var rejectedRegistration = await client.PostAsJsonAsync("/api/shooting-range/auth/register", shortRegistration);
        rejectedRegistration.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var acceptedRegistration = await client.PostAsJsonAsync("/api/shooting-range/auth/register", shortRegistration with { Password = "1234567" });
        acceptedRegistration.StatusCode.Should().Be(HttpStatusCode.OK);

        using var user = await LoginAsync(factory, false);
        await SetCsrfAsync(user);
        using var rejectedChange = await user.PostAsJsonAsync("/api/shooting-range/auth/password", new PasswordChangeRequest(Password, "123456"));
        rejectedChange.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var acceptedChange = await user.PostAsJsonAsync("/api/shooting-range/auth/password", new PasswordChangeRequest(Password, "1234567"));
        acceptedChange.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var admin = await LoginAsync(factory, true, username: "administrator");
        await SetCsrfAsync(admin);
        using var rejectedReset = await admin.PostAsJsonAsync("/api/shooting-range/admin/users/account/password", new AdminPasswordResetRequest("123456"));
        rejectedReset.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var acceptedReset = await admin.PostAsJsonAsync("/api/shooting-range/admin/users/account/password", new AdminPasswordResetRequest("1234567"));
        acceptedReset.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Login_is_throttled_with_generic_failure()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await SetCsrfAsync(client);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var rejected = await client.PostAsJsonAsync("/api/shooting-range/auth/login", new ShootingRangeLoginRequest("unknown", Password));
            rejected.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        using var throttled = await client.PostAsJsonAsync("/api/shooting-range/auth/login", new ShootingRangeLoginRequest("unknown", Password));
        throttled.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Logout_revokes_replayed_cookie()
    {
        await using var factory = new Factory();
        using var client = await LoginAsync(factory, false);
        using var replay = await ReplayAsync(factory, client);
        await SetCsrfAsync(client);
        using var logout = await client.PostAsync("/api/shooting-range/auth/logout", null);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var restored = await replay.GetAsync("/api/shooting-range/auth/session");
        restored.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Expired_sessions_and_legacy_cookies_require_reauthentication(bool legacy)
    {
        await using var factory = new Factory();
        using var client = await LoginAsync(factory, false);
        using var replay = await ReplayAsync(factory, client, includeSession: !legacy);
        if (!legacy)
        {
            using var scope = factory.Services.CreateScope();
            var repositories = scope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();
            var session = (await repositories.LocalSessions.GetAllAsync()).Single();
            await repositories.LocalSessions.ReplaceAsync(session with { ExpiresUtc = DateTime.UtcNow.AddMinutes(-1) });
        }
        using var rejected = await replay.GetAsync("/api/shooting-range/messages");
        rejected.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Forced_change_session_can_logout_before_changing_password()
    {
        await using var factory = new Factory();
        using var client = await LoginAsync(factory, false);
        using var scope = factory.Services.CreateScope();
        var repositories = scope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();
        var user = await repositories.Users.GetAsync("account");
        await repositories.Users.ReplaceAsync(user! with { ForcePasswordChange = true });
        await SetCsrfAsync(client);
        using var logout = await client.PostAsync("/api/shooting-range/auth/logout", null);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await repositories.LocalSessions.GetAllAsync()).Single().Revoked.Should().BeTrue();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Deployed_host_rejects_mock_before_resolving_storage(string environment)
    {
        using var factory = new Factory(enableInMemoryDb: false, useMock: true, environment: environment);
        Action start = () => factory.CreateClient();
        start.Should().Throw<InvalidOperationException>().WithMessage("*mock storage*");
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Deployed_host_rejects_in_memory_database_before_resolving_storage(string environment)
    {
        using var factory = new Factory(environment: environment);
        Action start = () => factory.CreateClient();
        start.Should().Throw<InvalidOperationException>().WithMessage("*in-memory database*");
    }

    [Fact]
    public void Sql_server_mode_requires_connection_configuration()
    {
        using var factory = new Factory(enableInMemoryDb: false);
        Action start = () => factory.CreateClient();
        start.Should().Throw<InvalidOperationException>()
            .WithMessage("*ShootingRangeDatabase:ConnectionString*");
    }

    [Theory]
    [InlineData("/api/shooting-range/auth/session")]
    [InlineData("/api/shooting-range/bookings/mine")]
    [InlineData("/api/shooting-range/messages")]
    public async Task Disabled_account_is_rejected_on_all_authenticated_reads(string path)
    {
        await using var factory = new Factory();
        using var client = await LoginAsync(factory, false);
        using var scope = factory.Services.CreateScope();
        var repositories = scope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();
        var user = await repositories.Users.GetAsync("account");
        await repositories.Users.ReplaceAsync(user! with { Status = ShootingRangeAccountStatus.Disabled });
        using var rejected = await client.GetAsync(path);
        rejected.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Demoted_admin_loses_admin_and_other_users_messages_immediately()
    {
        await using var factory = new Factory();
        using var client = await LoginAsync(factory, true);
        using var scope = factory.Services.CreateScope();
        var repositories = scope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();
        await repositories.Threads.InsertAsync(new() { Id = "private", UserId = "another-user", Subject = "private" });
        var user = await repositories.Users.GetAsync("account");
        await repositories.Users.ReplaceAsync(user! with { IsAdmin = false });
        using var rejected = await client.GetAsync("/api/shooting-range/admin/users");
        rejected.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetFromJsonAsync<ShootingRangeThread[]>("/api/shooting-range/messages"))!.Should().BeEmpty();
    }

    [Fact]
    public async Task Password_change_invalidates_other_sessions_and_renews_current_session()
    {
        await using var factory = new Factory();
        using var first = await LoginAsync(factory, false);
        using var oldCookie = await ReplayAsync(factory, first);
        using var second = await LoginAsync(factory, false, insert: false);
        await SetCsrfAsync(first);
        using var changed = await first.PostAsJsonAsync("/api/shooting-range/auth/password", new PasswordChangeRequest(Password, "new-test-password-123"));
        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await SetCsrfAsync(first);
        using var renewed = await first.GetAsync("/api/shooting-range/auth/session");
        renewed.StatusCode.Should().Be(HttpStatusCode.OK);
        using var rejected = await second.GetAsync("/api/shooting-range/bookings/mine");
        rejected.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var replayRejected = await oldCookie.GetAsync("/api/shooting-range/auth/session");
        replayRejected.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_reset_invalidates_sessions_and_forced_change_allows_only_recovery()
    {
        await using var factory = new Factory();
        using var target = await LoginAsync(factory, false);
        using var admin = await LoginAsync(factory, true, username: "administrator");
        await SetCsrfAsync(admin);
        using var reset = await admin.PostAsJsonAsync("/api/shooting-range/admin/users/account/password", new AdminPasswordResetRequest(Password));
        reset.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var stale = await target.GetAsync("/api/shooting-range/auth/session");
        stale.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var forced = await LoginAsync(factory, false, insert: false);
        (await forced.GetFromJsonAsync<AccountDto>("/api/shooting-range/auth/session"))!.ForcePasswordChange.Should().BeTrue();
        foreach (var path in new[] { "bookings/mine", "messages", "config", "sessions?date=2030-01-12", "availability?date=2030-01-12" })
        {
            using var forbidden = await forced.GetAsync("/api/shooting-range/" + path);
            forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        await SetCsrfAsync(forced);
        using var changed = await forced.PostAsJsonAsync("/api/shooting-range/auth/password", new PasswordChangeRequest(Password, "replacement-test-123"));
        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await SetCsrfAsync(forced);
        using var permitted = await forced.GetAsync("/api/shooting-range/messages");
        permitted.StatusCode.Should().Be(HttpStatusCode.OK);
        using var logout = await forced.PostAsync("/api/shooting-range/auth/logout", null);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<HttpClient> ReplayAsync(Factory factory, HttpClient client, bool includeSession = true)
    {
        var session = await client.GetFromJsonAsync<AccountDto>("/api/shooting-range/auth/session");
        session.Should().NotBeNull();
        using var scope = factory.Services.CreateScope();
        var repositories = scope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();
        var localSession = (await repositories.LocalSessions.GetAllAsync()).Single(session => session.UserId == "account" && !session.Revoked);
        var user = await repositories.Users.GetAsync("account");
        var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user!.Id),
            new(System.Security.Claims.ClaimTypes.Role, user.IsAdmin ? "admin" : "user"),
            new(ShootingRangeSessions.SessionClaim, localSession.Id)
        ], Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme));
        if (!includeSession) ((System.Security.Claims.ClaimsIdentity)principal.Identity!).RemoveClaim(principal.FindFirst(ShootingRangeSessions.SessionClaim)!);
        var options = factory.Services.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>>().Get("Cookies");
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, new() { ExpiresUtc = new DateTimeOffset(localSession.ExpiresUtc) }, "Cookies");
        var replay = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        replay.DefaultRequestHeaders.Add("Cookie", "shooting_range_auth=" + options.TicketDataFormat.Protect(ticket));
        return replay;
    }

    private static async Task<HttpClient> LoginAsync(Factory factory, bool admin, bool insert = true, string username = "account", bool keepPreloginToken = false)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var hash = PasswordHashing.HashPassword(Password, out var salt);
        if (insert)
        {
            using var scope = factory.Services.CreateScope();
            var repositories = scope.ServiceProvider.GetRequiredService<IShootingRangeRepositorySet>();
            await repositories.Users.InsertAsync(new() { Id = username, Username = username, PasswordHash = $"{hash}:{salt}", Status = ShootingRangeAccountStatus.Approved, IsAdmin = admin });
        }
        await SetCsrfAsync(client);
        using var login = await client.PostAsJsonAsync("/api/shooting-range/auth/login", new ShootingRangeLoginRequest(username, Password));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        if (!keepPreloginToken) client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        return client;
    }

    private static async Task SetCsrfAsync(HttpClient client)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/shooting-range/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
    }

    private sealed class Factory : WebApplicationFactory<ShootingRangeController>
    {
        private readonly bool enableInMemoryDb;
        private readonly bool useMock;
        private readonly string environment;
        private readonly string inMemoryDatabaseName = $"ShootingRange-{Guid.NewGuid():N}";
        public string? InMemoryAdminPassword { get; }

        public Factory(bool enableInMemoryDb = true, bool useMock = false, string environment = "Test", bool seedInMemoryAdmin = false)
        {
            this.enableInMemoryDb = enableInMemoryDb;
            this.useMock = useMock;
            this.environment = environment;
            InMemoryAdminPassword = seedInMemoryAdmin ? Guid.NewGuid().ToString("N") : null;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("FeatureManagement:UseMock", useMock.ToString());
            builder.UseSetting("FeatureManagement:EnableInMemoryDb", enableInMemoryDb.ToString());
            builder.UseSetting("FeatureManagement:EnableKeyVault", "false");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseMock"] = useMock.ToString(),
                ["FeatureManagement:EnableInMemoryDb"] = enableInMemoryDb.ToString(),
                ["FeatureManagement:EnableKeyVault"] = "false",
                ["ShootingRange:InMemoryAdminPassword"] = InMemoryAdminPassword,
                ["ShootingRangeDatabase:ConnectionString"] = null,
                ["Logging:LogLevel:Default"] = "Error"
            }));
            if (enableInMemoryDb)
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ShootingRangeDbContext>();
                    services.RemoveAll<DbContextOptions<ShootingRangeDbContext>>();
                    services.AddDbContext<ShootingRangeDbContext>(options =>
                        options.UseInMemoryDatabase(inMemoryDatabaseName));
                });
            }
        }
    }
}