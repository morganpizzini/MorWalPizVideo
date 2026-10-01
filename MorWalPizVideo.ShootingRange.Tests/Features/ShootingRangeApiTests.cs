using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.ShootingRange.Contracts;
using MorWalPizVideo.ShootingRange.Controllers;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Security;

namespace MorWalPizVideo.ShootingRange.Tests.Features;

[Trait("Category", "TestGroup:ShootingRange")]
public sealed class ShootingRangeApiTests
{
    private const string Origin = "https://range-spa-bjeqb5gwggf0hfaj.westeurope-01.azurewebsites.net";
    private const string Password = "test-password-only-123";

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
        var users = await client.GetFromJsonAsync<JsonElement>("/api/shooting-range/admin/users");
        users[0].EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("id", "creationDateTime", "username", "firstName", "lastName", "status", "isAdmin", "forcePasswordChange");
        users[0].GetProperty("status").GetInt32().Should().Be(1);
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
        (await client.GetFromJsonAsync<ShootingRangeException[]>("/api/shooting-range/admin/exceptions"))!.Single().IsClosed.Should().BeFalse();
        var repositories = factory.Services.GetRequiredService<IShootingRangeRepositorySet>();
        var admin = (await repositories.Users.GetAllAsync()).Single();
        await repositories.Users.ReplaceAsync(admin with { Status = ShootingRangeAccountStatus.Disabled });
        using var staleCookie = await client.PutAsJsonAsync("/api/shooting-range/admin/config", config);
        staleCookie.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Normal_user_cannot_admin_but_can_discover_book_and_receive_conflict()
    {
        await using var factory = new Factory();
        using var client = await LoginAsync(factory, false);
        var repositories = factory.Services.GetRequiredService<IShootingRangeRepositorySet>();
        await repositories.Bays.InsertAsync(new() { Id = "bay-1", Code = "01" });
        using var forbidden = await client.GetAsync("/api/shooting-range/admin/users");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
            var repositories = factory.Services.GetRequiredService<IShootingRangeRepositorySet>();
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
        var repositories = factory.Services.GetRequiredService<IShootingRangeRepositorySet>();
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
        using var parent = new Factory();
        using var factory = parent.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("FeatureManagement:EnableMock", "true");
            builder.UseSetting("FeatureManagement:EnableKeyVault", "false");
        });
        Action start = () => factory.CreateClient();
        start.Should().Throw<InvalidOperationException>().WithMessage("*mock storage*");
    }

    [Theory]
    [InlineData("/api/shooting-range/auth/session")]
    [InlineData("/api/shooting-range/bookings/mine")]
    [InlineData("/api/shooting-range/messages")]
    public async Task Disabled_account_is_rejected_on_all_authenticated_reads(string path)
    {
        await using var factory = new Factory();
        using var client = await LoginAsync(factory, false);
        var repositories = factory.Services.GetRequiredService<IShootingRangeRepositorySet>();
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
        var repositories = factory.Services.GetRequiredService<IShootingRangeRepositorySet>();
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
        var repositories = factory.Services.GetRequiredService<IShootingRangeRepositorySet>();
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
        if (insert) await factory.Services.GetRequiredService<IShootingRangeRepositorySet>().Users.InsertAsync(new() { Id = username, Username = username, PasswordHash = $"{hash}:{salt}", Status = ShootingRangeAccountStatus.Approved, IsAdmin = admin });
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
        static Factory()
        {
            Environment.SetEnvironmentVariable("FeatureManagement__EnableMock", "true");
            Environment.SetEnvironmentVariable("FeatureManagement__EnableKeyVault", "false");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:EnableMock"] = "true",
                ["FeatureManagement:EnableKeyVault"] = "false",
                ["Logging:LogLevel:Default"] = "Error"
            }));
        }
    }
}