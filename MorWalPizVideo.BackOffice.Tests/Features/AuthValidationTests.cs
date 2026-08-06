using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class AuthValidationTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory _factory;

    public AuthValidationTests(BackOfficeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Validate_returns_effective_permissions_from_the_cookie_session()
    {
        var userId = $"validation-user-{Guid.NewGuid():N}";
        var groupId = $"validation-group-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var groupRepository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
            await groupRepository.AddItemAsync(new UserGroup
            {
                Id = groupId,
                Code = "backoffice-users",
                Name = "BackOffice Users",
                IsActive = true,
                Permissions = ["canaccessbackoffice"]
            });
            await userRepository.AddItemAsync(new User
            {
                Id = userId,
                Username = "validation-user",
                Email = "validation-user@example.test",
                PasswordHash = "hash",
                Salt = "salt",
                IsActive = true,
                GroupIds = [groupId]
            });
        }

        using var scopeForToken = _factory.Services.CreateScope();
        var jwtService = scopeForToken.ServiceProvider.GetRequiredService<IJwtService>();
        var token = jwtService.GenerateToken(new User
        {
            Id = userId,
            Username = "validation-user",
            Email = "validation-user@example.test",
            IsActive = true
        });

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add("Cookie", $"auth_token={token}");
        var csrfResponse = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfResponse!.Token);

        var response = await client.PostAsJsonAsync("/api/auth/validate", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var validation = await response.Content.ReadFromJsonAsync<AuthValidationResponse>();
        Assert.Equal(userId, validation!.UserId);
        Assert.Contains("canaccessbackoffice", validation.EffectivePermissions);
    }

    private sealed record CsrfResponse(string Token);

    private sealed record AuthValidationResponse(string UserId, string[] EffectivePermissions);
}