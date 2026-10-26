using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:ServerAPI")]
public sealed class PublicAccountEndpointTests : IClassFixture<ServerApiWebApplicationFactory>
{
    private readonly ServerApiWebApplicationFactory factory;

    public PublicAccountEndpointTests(ServerApiWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Registration_requires_csrf_and_restores_session_without_locking_public_pages()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var request = new PublicAccountRegistrationRequest
        {
            Username = $"user{Guid.NewGuid():N}"[..12],
            Email = $"{Guid.NewGuid():N}@example.test",
            Password = "A-strong-password-123",
            DisplayName = "Public User"
        };

        var withoutCsrf = await client.PostAsJsonAsync("/api/v1/accounts", request);
        var csrf = await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf");
        using var registration = new HttpRequestMessage(HttpMethod.Post, "/api/v1/accounts")
        {
            Content = JsonContent.Create(request)
        };
        registration.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        var registered = await client.SendAsync(registration);
        var session = await client.GetFromJsonAsync<PublicAccountSessionContract>("/api/v1/account/session");
        var publicPage = await client.GetAsync("/api/navigation");

        Assert.Equal(HttpStatusCode.Forbidden, withoutCsrf.StatusCode);
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        Assert.True(session!.Authenticated);
        Assert.Equal(request.Email, session.Account!.Email);
        Assert.NotEqual(HttpStatusCode.Unauthorized, publicPage.StatusCode);
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
            DisplayName = "Before Update"
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
}
