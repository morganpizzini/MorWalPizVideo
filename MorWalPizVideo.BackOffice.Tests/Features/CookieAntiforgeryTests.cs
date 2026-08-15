using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class CookieAntiforgeryTests : IClassFixture<BackOfficeWebApplicationFactory>
{
  private const string AdminSpaOrigin = "https://morwalpiz-admin-spa.azurewebsites.net";
  private readonly BackOfficeWebApplicationFactory _factory;

  public CookieAntiforgeryTests(BackOfficeWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Production_cors_allows_only_the_accepted_admin_spa_origin()
  {
    using var client = CreateClient();

    using var allowedRequest = CreatePreflight(AdminSpaOrigin);
    using var allowedResponse = await client.SendAsync(allowedRequest);
    Assert.Equal(HttpStatusCode.NoContent, allowedResponse.StatusCode);
    Assert.Equal(AdminSpaOrigin, allowedResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());
    Assert.Equal("true", allowedResponse.Headers.GetValues("Access-Control-Allow-Credentials").Single());

    using var rejectedRequest = CreatePreflight("https://unsupported.example");
    using var rejectedResponse = await client.SendAsync(rejectedRequest);
    Assert.False(rejectedResponse.Headers.Contains("Access-Control-Allow-Origin"));
    Assert.False(rejectedResponse.Headers.Contains("Access-Control-Allow-Credentials"));
  }

  [Fact]
  public async Task Login_preserves_response_shape_and_sets_secure_cross_origin_cookie()
  {
    using var client = CreateClient();

    using var response = await LoginAsync(client);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.Equal(["user"], payload.RootElement.EnumerateObject().Select(property => property.Name));
    Assert.Equal(
        PrimaryScenario.AdminUsername,
        payload.RootElement.GetProperty("user").GetProperty("username").GetString());
    var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("auth_token=", StringComparison.Ordinal));
    Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("samesite=none", cookie, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public async Task Login_without_backoffice_permission_is_rejected_before_setting_cookie()
  {
    var username = $"no-backoffice-{Guid.NewGuid():N}";
    var password = "Secret123!";
    var userId = $"user-{Guid.NewGuid():N}";

    using (var scope = _factory.Services.CreateScope())
    {
      var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
      var groupRepository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
      var groupId = $"group-{Guid.NewGuid():N}";

      await groupRepository.AddItemAsync(new UserGroup
      {
        Id = groupId,
        Code = "basic-users",
        Name = "Basic Users",
        IsActive = true,
        Permissions = []
      });

      var passwordHash = UserRepository.HashPassword(password, out var salt);
      await userRepository.AddItemAsync(new User
      {
        Id = userId,
        Username = username,
        Email = $"{username}@example.test",
        PasswordHash = passwordHash,
        Salt = salt,
        IsActive = true,
        GroupIds = [groupId]
      });
    }

    using var client = CreateClient();
    using var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.DoesNotContain(response.Headers, header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public async Task Cookie_authenticated_unsafe_requests_require_a_valid_csrf_token()
  {
    using var client = CreateClient();
    using var loginResponse = await LoginAsync(client);
    Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

    using var missingResponse = await client.PostAsJsonAsync("/api/auth/validate", new { });
    Assert.Equal(HttpStatusCode.BadRequest, missingResponse.StatusCode);

    var token = await IssueTokenAsync(client);
    using var forgedRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/validate")
    {
      Content = JsonContent.Create(new { })
    };
    forgedRequest.Headers.Add("X-CSRF-TOKEN", token + "forged");
    using var forgedResponse = await client.SendAsync(forgedRequest);
    Assert.Equal(HttpStatusCode.BadRequest, forgedResponse.StatusCode);

    using var validRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/validate")
    {
      Content = JsonContent.Create(new { })
    };
    validRequest.Headers.Add("X-CSRF-TOKEN", token);
    using var validResponse = await client.SendAsync(validRequest);
    Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
    var payload = await validResponse.Content.ReadFromJsonAsync<ValidatePayload>();
    Assert.False(string.IsNullOrWhiteSpace(payload?.UserId));
  }

  [Fact]
  public async Task Api_key_endpoint_with_auth_cookie_skips_cookie_csrf_but_remains_authorized()
  {
    using var client = CreateClient();
    client.DefaultRequestHeaders.Add("Cookie", "auth_token=stale-token");

    using var response = await client.PostAsJsonAsync(
        "/api/mongoindexes/apply",
        new { approvalToken = "apply-approved-indexes", approvedKeys = new[] { "pages_url" } });

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Theory]
  [InlineData("Authorization", "Bearer machine-token")]
  [InlineData("X-API-Key", "machine-key")]
  public async Task Bearer_and_api_key_requests_are_not_subject_to_cookie_csrf(
      string headerName,
      string headerValue)
  {
    using var client = CreateClient();
    using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/validate")
    {
      Content = JsonContent.Create(new { })
    };
    request.Headers.Add(headerName, headerValue);

    using var response = await client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Theory]
  [InlineData("Authorization", "Bearer forged-token")]
  [InlineData("X-API-Key", "forged-key")]
  public async Task Machine_auth_headers_cannot_bypass_cookie_csrf_on_logout(
      string headerName,
      string headerValue)
  {
    using var client = CreateClient();
    using var loginResponse = await LoginAsync(client);
    Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

    using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
    {
      Content = JsonContent.Create(new { })
    };
    request.Headers.Add(headerName, headerValue);

    using var response = await client.SendAsync(request);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Anonymous_login_does_not_require_csrf_and_logout_does()
  {
    using var client = CreateClient();
    using var loginResponse = await LoginAsync(client);
    Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

    using var repeatedLoginResponse = await LoginAsync(client);
    Assert.Equal(HttpStatusCode.OK, repeatedLoginResponse.StatusCode);

    using var missingLogoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new { });
    Assert.Equal(HttpStatusCode.BadRequest, missingLogoutResponse.StatusCode);

    var token = await IssueTokenAsync(client);
    using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
    {
      Content = JsonContent.Create(new { })
    };
    logoutRequest.Headers.Add("X-CSRF-TOKEN", token);
    using var logoutResponse = await client.SendAsync(logoutRequest);
    Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
    var payload = await logoutResponse.Content.ReadFromJsonAsync<LogoutPayload>();
    Assert.Equal("Logged out successfully", payload?.Message);
    var cookie = logoutResponse.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("auth_token=", StringComparison.Ordinal));
    Assert.StartsWith("auth_token=;", cookie, StringComparison.Ordinal);
    Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("samesite=none", cookie, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public async Task Health_probe_remains_available_when_an_auth_cookie_is_present()
  {
    using var client = CreateClient();
    client.DefaultRequestHeaders.Add("Cookie", "auth_token=not-a-valid-jwt");

    using var response = await client.GetAsync("/health/live");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
  {
    BaseAddress = new Uri("https://localhost"),
    HandleCookies = true,
    AllowAutoRedirect = false
  });

  private static HttpRequestMessage CreatePreflight(string origin)
  {
    var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
    request.Headers.Add("Origin", origin);
    request.Headers.Add("Access-Control-Request-Method", "POST");
    request.Headers.Add("Access-Control-Request-Headers", "content-type,x-csrf-token");
    return request;
  }

  private static Task<HttpResponseMessage> LoginAsync(HttpClient client) => client.PostAsJsonAsync(
      "/api/auth/login",
      new { username = PrimaryScenario.AdminUsername, password = PrimaryScenario.AdminPassword });

  private static async Task<string> IssueTokenAsync(HttpClient client)
  {
    var payload = await client.GetFromJsonAsync<CsrfPayload>("/api/auth/csrf");
    return Assert.IsType<string>(payload?.Token);
  }

  private sealed record CsrfPayload(string Token);
  private sealed record ValidatePayload(string UserId);
  private sealed record LogoutPayload(string Message);
}