using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.ServerAPI.Controllers;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:ServerAPI")]
public sealed class PublicAccountEndpointTests : IClassFixture<ServerApiWebApplicationFactory>
{
    private readonly ServerApiWebApplicationFactory factory;

    public PublicAccountEndpointTests(ServerApiWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public void Account_reads_are_explicitly_excluded_from_output_cache()
    {
        var csrf = typeof(PublicAccountController).GetMethod(nameof(PublicAccountController.GetCsrf))!
            .GetCustomAttributes(typeof(OutputCacheAttribute), inherit: false)
            .Cast<OutputCacheAttribute>()
            .Single();
        var session = typeof(PublicAccountController).GetMethod(nameof(PublicAccountController.Session))!
            .GetCustomAttributes(typeof(OutputCacheAttribute), inherit: false)
            .Cast<OutputCacheAttribute>()
            .Single();
        var profile = typeof(PublicProfileController).GetMethod(nameof(PublicProfileController.Get))!
            .GetCustomAttributes(typeof(OutputCacheAttribute), inherit: false)
            .Cast<OutputCacheAttribute>()
            .Single();

        Assert.True(csrf.NoStore);
        Assert.True(session.NoStore);
        Assert.True(profile.NoStore);
    }

    [Fact]
    public async Task Csrf_bootstrap_returns_credentialed_cors_headers_for_the_public_origin()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/account/csrf");
        request.Headers.Add("Origin", "https://morwalpiz.com");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<PublicAccountCsrfContract>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal("https://morwalpiz.com", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith("public_account_csrf=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Csrf_bootstrap_does_not_allow_credentials_for_an_unsupported_origin()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/account/csrf");
        request.Headers.Add("Origin", "https://not-approved.example");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Registration_requires_csrf_and_restores_session_without_locking_public_pages()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var request = new PublicAccountRegistrationRequest
        {
            Username = $"user{Guid.NewGuid():N}"[..12],
            Email = $"{Guid.NewGuid():N}@example.test",
            Password = "A-strong-password-123",
            DisplayName = "Public User",
            TermsAccepted = true,
            PrivacyNoticeAcknowledged = true
        };

        var withoutCsrf = await client.PostAsJsonAsync("/api/v1/accounts", request);
        var csrfResponse = await client.GetAsync("/api/v1/account/csrf");
        var csrf = await csrfResponse.Content.ReadFromJsonAsync<PublicAccountCsrfContract>();
        var repeatedCsrf = await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf");
        using var registration = new HttpRequestMessage(HttpMethod.Post, "/api/v1/accounts")
        {
            Content = JsonContent.Create(request)
        };
        registration.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        var registered = await client.SendAsync(registration);
        var sessionResponse = await client.GetAsync("/api/v1/account/session");
        var session = await sessionResponse.Content.ReadFromJsonAsync<PublicAccountSessionContract>();
        var publicPage = await client.GetAsync("/api/navigation");

        Assert.Equal(HttpStatusCode.Forbidden, withoutCsrf.StatusCode);
    Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        Assert.Equal("no-store", csrfResponse.Headers.CacheControl?.ToString());
        Assert.Equal("no-store", sessionResponse.Headers.CacheControl?.ToString());
        Assert.NotNull(csrf);
        Assert.False(string.IsNullOrWhiteSpace(csrf.Token));
        Assert.Equal(csrf.Token, repeatedCsrf!.Token);
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        Assert.True(session!.Authenticated);
        Assert.Equal(request.Email, session.Account!.Email);
        Assert.NotEqual(HttpStatusCode.Unauthorized, publicPage.StatusCode);
    }

    [Fact]
    public async Task Login_with_invalid_credentials_returns_unauthorized_after_valid_csrf()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var csrfResponse = await client.GetAsync("/api/v1/account/csrf");
        var csrf = await csrfResponse.Content.ReadFromJsonAsync<PublicAccountCsrfContract>();
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/account/sessions")
        {
            Content = JsonContent.Create(new PublicAccountLoginRequest
            {
                Identifier = $"missing-{Guid.NewGuid():N}",
                Password = "incorrect-password"
            })
        };
        login.Headers.Add("X-CSRF-TOKEN", csrf!.Token);

        var response = await client.SendAsync(login);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_remember_me_sets_a_persistent_session_cookie()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var request = new PublicAccountRegistrationRequest
        {
            Username = $"user{Guid.NewGuid():N}"[..12],
            Email = $"{Guid.NewGuid():N}@example.test",
            Password = "A-strong-password-123",
            DisplayName = "Remembered User",
            TermsAccepted = true,
            PrivacyNoticeAcknowledged = true
        };
        var csrf = await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf");
        using var registration = new HttpRequestMessage(HttpMethod.Post, "/api/v1/accounts")
        {
            Content = JsonContent.Create(request)
        };
        registration.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        await client.SendAsync(registration);

        using var logout = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/account/sessions/current");
        logout.Headers.Add("X-CSRF-TOKEN", csrf.Token);
        await client.SendAsync(logout);

        csrf = await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf");
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/account/sessions")
        {
            Content = JsonContent.Create(new PublicAccountLoginRequest
            {
                Identifier = request.Username,
                Password = request.Password,
                RememberMe = true
            })
        };
        login.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        var response = await client.SendAsync(login);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            cookie => cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Registration_issues_a_cookie_safe_session_token_that_restores_immediately()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var request = new PublicAccountRegistrationRequest
        {
            Username = $"user{Guid.NewGuid():N}"[..12],
            Email = $"{Guid.NewGuid():N}@example.test",
            Password = "A-strong-password-123",
            DisplayName = "Cookie Safe User",
            TermsAccepted = true,
            PrivacyNoticeAcknowledged = true
        };
        var csrf = await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf");
        using var registration = new HttpRequestMessage(HttpMethod.Post, "/api/v1/accounts")
        {
            Content = JsonContent.Create(request)
        };
        registration.Headers.Add("X-CSRF-TOKEN", csrf!.Token);

        var registered = await client.SendAsync(registration);
        var sessionCookie = registered.Headers.GetValues("Set-Cookie")
            .Single(cookie => cookie.StartsWith("public_account_session=", StringComparison.Ordinal));
        var sessionCookieValue = sessionCookie.Split(';', 2)[0]
            ["public_account_session=".Length..];
        var session = await client.GetFromJsonAsync<PublicAccountSessionContract>("/api/v1/account/session");

        Assert.Equal(64, sessionCookieValue.Length);
        Assert.All(sessionCookieValue, character => Assert.True(char.IsAsciiHexDigit(character)));
        Assert.True(session!.Authenticated);
        Assert.Equal(request.Email, session.Account!.Email);
    }

    [Fact]
    public async Task Workshop_capability_guide_seen_requires_csrf_and_is_monotonic()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var request = new PublicAccountRegistrationRequest
        {
            Username = $"user{Guid.NewGuid():N}"[..12],
            Email = $"{Guid.NewGuid():N}@example.test",
            Password = "A-strong-password-123",
            DisplayName = "Workshop User",
            TermsAccepted = true,
            PrivacyNoticeAcknowledged = true
        };
        var csrf = await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf");
        using var registration = new HttpRequestMessage(HttpMethod.Post, "/api/v1/accounts")
        {
            Content = JsonContent.Create(request)
        };
        registration.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        var registered = await client.SendAsync(registration);
        var session = await registered.Content.ReadFromJsonAsync<PublicAccountSessionContract>();

        using var withoutCsrf = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workshop/capability-guide/seen")
        {
            Content = JsonContent.Create(new WorkshopCapabilityGuideSeenRequest { Version = 1 })
        };
        var forbidden = await client.SendAsync(withoutCsrf);
        using var markSeen = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workshop/capability-guide/seen")
        {
            Content = JsonContent.Create(new WorkshopCapabilityGuideSeenRequest { Version = 1 })
        };
        markSeen.Headers.Add("X-CSRF-TOKEN", csrf.Token);
        var first = await client.SendAsync(markSeen);
        var firstBody = await first.Content.ReadFromJsonAsync<WorkshopCapabilityGuideSeenResponse>();
        using var repeat = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workshop/capability-guide/seen")
        {
            Content = JsonContent.Create(new WorkshopCapabilityGuideSeenRequest { Version = 1 })
        };
        repeat.Headers.Add("X-CSRF-TOKEN", csrf.Token);
        var second = await client.SendAsync(repeat);

        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        Assert.Equal(0, session!.Account!.WorkshopCapabilityGuideSeenVersion);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(1, firstBody!.EffectiveVersion);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(1, (await second.Content.ReadFromJsonAsync<WorkshopCapabilityGuideSeenResponse>())!.EffectiveVersion);
    }

    [Fact]
    public async Task Profile_requires_account_session_and_logout_revokes_it()
    {
        using var anonymous = factory.CreateClient();
        var anonymousProfile = await anonymous.GetAsync("/api/v1/profile");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var csrf = await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf");
        var request = new PublicAccountRegistrationRequest
        {
            Username = $"user{Guid.NewGuid():N}"[..12],
            Email = $"{Guid.NewGuid():N}@example.test",
            Password = "A-strong-password-123",
            DisplayName = "Before Update",
            TermsAccepted = true,
            PrivacyNoticeAcknowledged = true
        };
        using var registration = new HttpRequestMessage(HttpMethod.Post, "/api/v1/accounts")
        {
            Content = JsonContent.Create(request)
        };
        registration.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        await client.SendAsync(registration);

        var profile = await client.GetAsync("/api/v1/profile");
        using var update = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile")
        {
            Content = JsonContent.Create(new PublicAccountProfileUpdateRequest
            {
                DisplayName = "After Update",
                Email = request.Email
            })
        };
        update.Headers.Add("X-CSRF-TOKEN", csrf.Token);
        var updated = await client.SendAsync(update);
        using var logout = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/account/sessions/current");
        logout.Headers.Add("X-CSRF-TOKEN", csrf.Token);
        var loggedOut = await client.SendAsync(logout);
        var afterLogout = await client.GetAsync("/api/v1/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousProfile.StatusCode);
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, loggedOut.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Registration_rejects_missing_legal_acknowledgements()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var csrf = await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf");
        var request = new PublicAccountRegistrationRequest
        {
            Username = $"user{Guid.NewGuid():N}"[..12],
            Email = $"{Guid.NewGuid():N}@example.test",
            Password = "A-strong-password-123",
            DisplayName = "Public User",
            TermsAccepted = false,
            PrivacyNoticeAcknowledged = false
        };
        using var registration = new HttpRequestMessage(HttpMethod.Post, "/api/v1/accounts")
        {
            Content = JsonContent.Create(request)
        };
        registration.Headers.Add("X-CSRF-TOKEN", csrf!.Token);

        var response = await client.SendAsync(registration);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
