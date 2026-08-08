using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class ImpersonationTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory factory;

    public ImpersonationTests(BackOfficeWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Manageall_operator_can_impersonate_with_target_scope_and_end_without_replacing_normal_auth()
    {
        var targetId = await AddTargetAsync();
        using var client = CreateCookieClient();
        client.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);
        await LoginAsync(client);

        var csrfToken = await GetCsrfTokenAsync(client);
        using var issueRequest = JsonRequest(HttpMethod.Post, "/api/impersonation/grants", new { targetUserId = targetId }, csrfToken);
        using var issueResponse = await client.SendAsync(issueRequest);

        Assert.Equal(HttpStatusCode.OK, issueResponse.StatusCode);
        var issue = await issueResponse.Content.ReadFromJsonAsync<GrantResponse>();
        Assert.NotNull(issue?.Grant);
        Assert.InRange(issue!.ExpiresAt - DateTime.UtcNow, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        if (issueResponse.Headers.TryGetValues("Set-Cookie", out var issueCookies))
        {
            Assert.DoesNotContain(issueCookies, value => value.StartsWith("auth_token=", StringComparison.Ordinal));
        }

        using var redeemRequest = JsonRequest(HttpMethod.Post, "/api/impersonation/sessions", new { grant = issue.Grant }, csrfToken);
        using var redeemResponse = await client.SendAsync(redeemRequest);
        Assert.Equal(HttpStatusCode.OK, redeemResponse.StatusCode);
        Assert.Contains(redeemResponse.Headers.GetValues("Set-Cookie"), value => value.StartsWith("impersonation_token=", StringComparison.Ordinal));
        Assert.DoesNotContain(redeemResponse.Headers.GetValues("Set-Cookie"), value => value.StartsWith("auth_token=", StringComparison.Ordinal));

        using var validateRequest = JsonRequest(HttpMethod.Post, "/api/auth/validate", new { }, csrfToken);
        using var validateResponse = await client.SendAsync(validateRequest);
        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
        var validation = await validateResponse.Content.ReadFromJsonAsync<ValidationResponse>();
        Assert.Equal(targetId, validation!.UserId);
        Assert.Equal(PrimaryScenario.AdminUserId, validation.ActorUserId);
        Assert.Equal(targetId, validation.TargetUserId);
        Assert.True(validation.IsImpersonating);

        using var contentResponse = await client.GetAsync("/api/Compilations");
        Assert.Equal(HttpStatusCode.OK, contentResponse.StatusCode);

        using var blockedResponse = await client.GetAsync("/api/User/me");
        Assert.Equal(HttpStatusCode.Forbidden, blockedResponse.StatusCode);

        using var replayRequest = JsonRequest(HttpMethod.Post, "/api/impersonation/sessions", new { grant = issue.Grant }, csrfToken);
        using var replayResponse = await client.SendAsync(replayRequest);
        Assert.Equal(HttpStatusCode.Forbidden, replayResponse.StatusCode);

        using var endRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/impersonation/sessions/current");
        endRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);
        using var endResponse = await client.SendAsync(endRequest);
        Assert.Equal(HttpStatusCode.NoContent, endResponse.StatusCode);

        using var normalValidationRequest = JsonRequest(HttpMethod.Post, "/api/auth/validate", new { }, csrfToken);
        using var normalValidationResponse = await client.SendAsync(normalValidationRequest);
        var normalValidation = await normalValidationResponse.Content.ReadFromJsonAsync<ValidationResponse>();
        Assert.Equal(PrimaryScenario.AdminUserId, normalValidation!.UserId);
        Assert.False(normalValidation.IsImpersonating);
    }

    [Fact]
    public async Task Cookie_impersonation_posts_require_csrf_and_api_key_or_non_operator_cannot_issue()
    {
        var targetId = await AddTargetAsync();
        using var cookieClient = CreateCookieClient();
        await LoginAsync(cookieClient);

        using var missingCsrfRequest = JsonRequest(HttpMethod.Post, "/api/impersonation/grants", new { targetUserId = targetId });
        using var missingCsrfResponse = await cookieClient.SendAsync(missingCsrfRequest);
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrfResponse.StatusCode);

        using var nonOperator = CreateTestClient("test-user-id", AuthorizationPermissionKeys.BackofficeAccess);
        using var nonOperatorRequest = JsonRequest(HttpMethod.Post, "/api/impersonation/grants", new { targetUserId = targetId });
        using var nonOperatorResponse = await nonOperator.SendAsync(nonOperatorRequest);
        Assert.Equal(HttpStatusCode.Forbidden, nonOperatorResponse.StatusCode);

        using var apiKeyClient = CreateTestClient(PrimaryScenario.AdminUserId, AuthorizationPermissionKeys.BackofficeManageAll);
        const string apiKeySecret = "impersonation-test-api-key";
        await AddApiKeyAsync(new ApiKey
        {
            Id = $"key-{Guid.NewGuid():N}",
            Name = $"key-{Guid.NewGuid():N}",
            Key = factory.Services.GetRequiredService<IApiKeyService>().HashApiKey(apiKeySecret),
            ChannelId = PrimaryScenario.ChannelId
        });
        apiKeyClient.DefaultRequestHeaders.Add("X-Test-ApiKey", apiKeySecret);
        using var apiKeyRequest = JsonRequest(HttpMethod.Post, "/api/impersonation/grants", new { targetUserId = targetId });
        using var apiKeyResponse = await apiKeyClient.SendAsync(apiKeyRequest);
        Assert.Equal(HttpStatusCode.Forbidden, apiKeyResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_security_and_inactive_targets_are_rejected()
    {
        var inactiveTargetId = await AddUserAsync(new User
        {
            Username = $"inactive-target-{Guid.NewGuid():N}",
            Email = $"inactive-target-{Guid.NewGuid():N}@example.test",
            IsActive = false,
            DirectPermissions = [AuthorizationPermissionKeys.BackofficeAccess]
        });
        var securityTargetId = await AddTargetAsync(isSecurityAccount: true);
        var operatorClient = CreateTestClient(PrimaryScenario.AdminUserId, AuthorizationPermissionKeys.BackofficeManageAll);

        foreach (var targetId in new[] { PrimaryScenario.AdminUserId, securityTargetId, inactiveTargetId })
        {
            using var request = JsonRequest(HttpMethod.Post, "/api/impersonation/grants", new { targetUserId = targetId });
            using var response = await operatorClient.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    private async Task<string> AddTargetAsync(bool isSecurityAccount = false)
    {
        var targetId = await AddUserAsync(new User
        {
            Username = $"impersonation-target-{Guid.NewGuid():N}",
            Email = $"impersonation-target-{Guid.NewGuid():N}@example.test",
            IsActive = true,
            IsSecurityAccount = isSecurityAccount,
            DirectPermissions =
            [
                AuthorizationPermissionKeys.BackofficeAccess,
                AuthorizationPermissionKeys.CompilationsView,
                AuthorizationPermissionKeys.CompilationsCreate
            ],
            GroupIds = [PrimaryScenario.ContributorGroupId]
        });

        using var scope = factory.Services.CreateScope();
        var ownerRepository = scope.ServiceProvider.GetRequiredService<IUserChannelOwnerRepository>();
        await ownerRepository.AddItemAsync(new UserChannelOwner
        {
            UserId = targetId,
            ChannelId = PrimaryScenario.ChannelId,
            IsActive = true
        });
        return targetId;
    }

    private async Task<string> AddUserAsync(User user)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var added = await repository.AddItemAsync(user);
        return added.Id!;
    }

    private async Task<ApiKey> AddApiKeyAsync(ApiKey apiKey)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
        return await repository.AddItemAsync(apiKey);
    }

    private HttpClient CreateCookieClient()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-Test-UserId", PrimaryScenario.AdminUserId);
        return client;
    }

    private HttpClient CreateTestClient(string userId, params string[] permissions)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(',', permissions));
        return client;
    }

    private static async Task LoginAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = PrimaryScenario.AdminUsername, password = PrimaryScenario.AdminPassword });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var payload = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        return Assert.IsType<string>(payload?.Token);
    }

    private static HttpRequestMessage JsonRequest(HttpMethod method, string path, object body, string? csrfToken = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        if (!string.IsNullOrWhiteSpace(csrfToken))
        {
            request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        }

        return request;
    }

    private sealed record CsrfResponse(string Token);
    private sealed record GrantResponse(string Grant, DateTime ExpiresAt);
    private sealed record ValidationResponse(string UserId, string? ActorUserId, string? TargetUserId, bool IsImpersonating);
}