using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Controllers;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class UserControllerProfileAndSecurityTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory _factory;

    public UserControllerProfileAndSecurityTests(BackOfficeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Backoffice_access_alone_cannot_read_or_mutate_users()
    {
        var contributorUserId = await SeedUserAsync(isAdmin: false);
        var managedUserId = await SeedUserAsync(isAdmin: true);

        using var contributorClient = CreateClient(contributorUserId);

        var listResponse = await contributorClient.GetAsync("/api/User");
        var readResponse = await contributorClient.GetAsync($"/api/User/{managedUserId}");
        var createResponse = await contributorClient.PostAsJsonAsync("/api/User", new CreateUserRequest
        {
            Username = $"created-{Guid.NewGuid():N}",
            Email = $"created-{Guid.NewGuid():N}@example.test",
            Password = "Secret123!"
        });
        var updateResponse = await contributorClient.PutAsJsonAsync($"/api/User/{managedUserId}", new UpdateUserRequest
        {
            Username = "blocked-update"
        });
        var statusResponse = await contributorClient.PutAsJsonAsync($"/api/User/{managedUserId}/status", new UpdateUserStatusRequest
        {
            IsActive = false
        });
        var resetResponse = await contributorClient.PutAsJsonAsync($"/api/User/{managedUserId}/password/reset", new ResetUserPasswordRequest
        {
            NewPassword = "AnotherSecret123!"
        });
        var setResponse = await contributorClient.PutAsJsonAsync($"/api/User/{managedUserId}/password/set", new ResetUserPasswordRequest
        {
            NewPassword = "AnotherSecret123!"
        });
        var deleteResponse = await contributorClient.DeleteAsync($"/api/User/{managedUserId}");

        Assert.Equal(HttpStatusCode.Forbidden, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, statusResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, resetResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, setResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Permission_manager_without_lifecycle_leaves_cannot_administer_user_lifecycle()
    {
        var permissionManagerUserId = await SeedUserAsync(isAdmin: false);
        var targetUserId = await SeedUserAsync(isAdmin: false);
        using var client = CreateClient(
            permissionManagerUserId,
            AuthorizationPermissionKeys.UsersPermissionsManage);

        var listResponse = await client.GetAsync("/api/User");
        var updateResponse = await client.PutAsJsonAsync($"/api/User/{targetUserId}", new UpdateUserRequest
        {
            Username = "blocked-permission-manager-update"
        });
        var deleteResponse = await client.DeleteAsync($"/api/User/{targetUserId}");

        Assert.Equal(HttpStatusCode.Forbidden, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task User_without_group_or_permission_is_denied()
    {
        var userId = await SeedUserWithoutAccessAsync();
        using var client = CreateClient(userId);

        var response = await client.GetAsync("/api/User/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Profile_get_and_update_are_available_for_authenticated_backoffice_user()
    {
        var userId = await SeedUserAsync(isAdmin: false);
        using var client = CreateClient(userId);

        var getResponse = await client.GetAsync("/api/User/me");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync("/api/User/me", new UpdateOwnProfileRequest
        {
            Username = "updated-profile-user",
            Email = "updated-profile-user@example.test"
        });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var updatedUser = await userRepository.GetItemAsync(userId);

        Assert.NotNull(updatedUser);
        Assert.Equal("updated-profile-user", updatedUser!.Username);
        Assert.Equal("updated-profile-user@example.test", updatedUser.Email);
    }

    [Fact]
    public async Task Profile_password_change_requires_current_password_and_updates_hash()
    {
        var username = $"user-standard-{Guid.NewGuid():N}";
        var userId = await SeedUserAsync(isAdmin: false, username: username, password: "Start123!");
        using var client = CreateClient(userId);

        var invalidCurrentResponse = await client.PutAsJsonAsync("/api/User/me/password", new ChangeOwnPasswordRequest
        {
            CurrentPassword = "wrong-password",
            NewPassword = "Next456!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidCurrentResponse.StatusCode);

        var validChangeResponse = await client.PutAsJsonAsync("/api/User/me/password", new ChangeOwnPasswordRequest
        {
            CurrentPassword = "Start123!",
            NewPassword = "Next456!"
        });
        Assert.Equal(HttpStatusCode.NoContent, validChangeResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        Assert.NotNull(await userRepository.AuthenticateAsync(username, "Next456!"));
        Assert.Null(await userRepository.AuthenticateAsync(username, "Start123!"));
    }

    [Fact]
    public async Task Admin_reset_sets_target_user_password_directly()
    {
        var adminUserId = await SeedUserAsync(isAdmin: true);
        var targetUserId = await SeedUserAsync(isAdmin: false, username: "reset-target", password: "Original123!");

        using var adminClient = CreateClient(adminUserId);
        var resetResponse = await adminClient.PutAsJsonAsync($"/api/User/{targetUserId}/password/reset", new ResetUserPasswordRequest
        {
            NewPassword = "Reset999!"
        });

        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        Assert.NotNull(await userRepository.AuthenticateAsync("reset-target", "Reset999!"));
        Assert.Null(await userRepository.AuthenticateAsync("reset-target", "Original123!"));
    }

    [Fact]
    public async Task Admin_set_password_endpoint_updates_target_user_password()
    {
        var adminUserId = await SeedUserAsync(isAdmin: true);
        var targetUserId = await SeedUserAsync(isAdmin: false, username: "set-target", password: "Original123!");

        using var adminClient = CreateClient(adminUserId);
        var setResponse = await adminClient.PutAsJsonAsync($"/api/User/{targetUserId}/password/set", new ResetUserPasswordRequest
        {
            NewPassword = "Set777!"
        });

        Assert.Equal(HttpStatusCode.NoContent, setResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        Assert.NotNull(await userRepository.AuthenticateAsync("set-target", "Set777!"));
        Assert.Null(await userRepository.AuthenticateAsync("set-target", "Original123!"));
    }

    [Fact]
    public async Task User_create_and_update_passwords_are_compatible_with_authentication_verifier()
    {
        var adminUserId = await SeedUserAsync(isAdmin: true);
        using var adminClient = CreateClient(adminUserId);

        var username = $"created-user-{Guid.NewGuid():N}";
        var email = $"{username}@example.test";

        var createResponse = await adminClient.PostAsJsonAsync("/api/User", new CreateUserRequest
        {
            Username = username,
            Email = email,
            Password = "CreatePass123!"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdPayload = await createResponse.Content.ReadFromJsonAsync<UserContract>();
        Assert.NotNull(createdPayload);

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/User/{createdPayload!.Id}", new UpdateUserRequest
        {
            NewPassword = "UpdatedPass123!"
        });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        Assert.NotNull(await userRepository.AuthenticateAsync(username, "UpdatedPass123!"));
        Assert.Null(await userRepository.AuthenticateAsync(username, "CreatePass123!"));
    }

    private HttpClient CreateClient(string userId, string? permissions = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        if (!string.IsNullOrWhiteSpace(permissions))
        {
            client.DefaultRequestHeaders.Add("X-Test-Permissions", permissions);
        }

        return client;
    }

    private async Task<string> SeedUserAsync(bool isAdmin, string? username = null, string password = "Secret123!")
    {
        var userId = $"user-{Guid.NewGuid():N}";
        var resolvedUsername = username ?? (isAdmin ? $"admin-{Guid.NewGuid():N}" : $"user-standard-{Guid.NewGuid():N}");
        var groupId = isAdmin ? $"group-admin-{Guid.NewGuid():N}" : $"group-contributor-{Guid.NewGuid():N}";

        using var scope = _factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var groupRepository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();

        await groupRepository.AddItemAsync(new UserGroup
        {
            Id = groupId,
            Code = isAdmin ? AuthorizationGroupCodes.Admin : AuthorizationGroupCodes.Contributor,
            Name = isAdmin ? "Admins" : "Contributors",
            IsActive = true,
            Permissions = isAdmin
                ? [AuthorizationPermissionKeys.BackofficeAccess, AuthorizationPermissionKeys.UsersManage]
                : [AuthorizationPermissionKeys.BackofficeAccess]
        });

        var hash = PasswordHashing.HashPassword(password, out var salt);
        await userRepository.AddItemAsync(new User
        {
            Id = userId,
            Username = resolvedUsername,
            Email = $"{resolvedUsername}@example.test",
            PasswordHash = hash,
            Salt = salt,
            IsActive = true,
            GroupIds = [groupId]
        });

        return userId;
    }

    private async Task<string> SeedUserWithoutAccessAsync()
    {
        var userId = $"no-access-{Guid.NewGuid():N}";

        using var scope = _factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var hash = PasswordHashing.HashPassword("Secret123!", out var salt);

        await userRepository.AddItemAsync(new User
        {
            Id = userId,
            Username = $"no-access-{Guid.NewGuid():N}",
            Email = $"no-access-{Guid.NewGuid():N}@example.test",
            PasswordHash = hash,
            Salt = salt,
            IsActive = true
        });

        return userId;
    }
}
