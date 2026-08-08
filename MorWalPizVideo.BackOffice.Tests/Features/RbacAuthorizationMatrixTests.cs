using System.Net;
using System.Net.Http.Json;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class RbacAuthorizationMatrixTests : IClassFixture<BackOfficeWebApplicationFactory>
{
  private readonly BackOfficeWebApplicationFactory _factory;

  public RbacAuthorizationMatrixTests(BackOfficeWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Rbac_endpoint_allows_user_with_inherited_users_manage_permission()
  {
    var userId = $"rbac-inherited-{Guid.NewGuid():N}";
    var groupId = $"rbac-group-{Guid.NewGuid():N}";

    await SeedUserAndGroupAsync(
        user: new User
        {
          Id = userId,
          Username = "rbac-inherited-user",
          Email = "rbac-inherited-user@example.test",
          PasswordHash = "hash",
          Salt = "salt",
          IsActive = true,
          GroupIds = [groupId]
        },
        group: new UserGroup
        {
          Id = groupId,
          Code = "rbac-inherited",
          Name = "Rbac Inherited",
          IsActive = true,
          Permissions = [AuthorizationPermissionKeys.UsersManage]
        });

    using var client = CreateClient(userId: userId);

    var response = await client.GetAsync("/api/Rbac/users");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Theory]
  [InlineData(AuthorizationPermissionKeys.UsersPermissionsManage, HttpStatusCode.OK)]
  [InlineData(AuthorizationPermissionKeys.UsersManage, HttpStatusCode.OK)]
  [InlineData(AuthorizationPermissionKeys.BackofficeManageAll, HttpStatusCode.OK)]
  [InlineData(AuthorizationPermissionKeys.UsersView, HttpStatusCode.OK)]
  [InlineData(AuthorizationPermissionKeys.BackofficeAccess, HttpStatusCode.Forbidden)]
  public async Task Rbac_endpoint_enforces_permission_management_capability(
      string permission,
      HttpStatusCode expectedStatus)
  {
    using var client = CreateClient(permissions: permission);

    var response = await client.GetAsync("/api/Rbac/users");

    Assert.Equal(expectedStatus, response.StatusCode);
  }

  [Fact]
  public async Task Users_view_can_read_lifecycle_details_but_cannot_mutate_grants_or_groups()
  {
    using var client = CreateClient(permissions: AuthorizationPermissionKeys.UsersView);

    var usersResponse = await client.GetAsync("/api/Rbac/users");
    var userId = (await usersResponse.Content.ReadFromJsonAsync<List<RbacUserSummaryContract>>())!.First().Id;
    var detailResponse = await client.GetAsync($"/api/Rbac/users/{userId}");
    var grantResponse = await client.PutAsJsonAsync(
        $"/api/Rbac/users/{userId}/permissions",
        new UpdateUserDirectPermissionsRequestContract());
    var groupsResponse = await client.GetAsync("/api/Rbac/groups");

    Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, grantResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, groupsResponse.StatusCode);
  }

  [Fact]
  public async Task Users_manage_holder_can_assign_any_direct_permission()
  {
    var targetUserId = $"rbac-target-{Guid.NewGuid():N}";
    await SeedUserAndGroupAsync(new User
    {
      Id = targetUserId,
      Username = "rbac-target-user",
      Email = "rbac-target-user@example.test",
      PasswordHash = "hash",
      Salt = "salt",
      IsActive = true
    });
    using var client = CreateClient(permissions: AuthorizationPermissionKeys.UsersManage);

    var response = await client.PutAsJsonAsync(
        $"/api/Rbac/users/{targetUserId}/permissions",
        new UpdateUserDirectPermissionsRequestContract
        {
          Permissions = [AuthorizationPermissionKeys.BackofficeManageAll]
        });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    using var scope = _factory.Services.CreateScope();
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var updatedUser = await userRepository.GetItemAsync(targetUserId);
    Assert.Contains(AuthorizationPermissionKeys.BackofficeManageAll, updatedUser!.DirectPermissions);

    var summaryResponse = await client.GetAsync($"/api/Rbac/users/{targetUserId}");
    var summary = await summaryResponse.Content.ReadFromJsonAsync<RbacUserSummaryContract>();
    Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
    Assert.Contains(AuthorizationPermissionKeys.BackofficeAccess, summary!.EffectivePermissions);
  }

  [Fact]
  public async Task Rbac_summary_expands_direct_manage_permission_to_specialized_leaf()
  {
    var targetUserId = $"rbac-insights-{Guid.NewGuid():N}";
    await SeedUserAndGroupAsync(new User
    {
      Id = targetUserId,
      Username = "rbac-insights-user",
      Email = "rbac-insights-user@example.test",
      PasswordHash = "hash",
      Salt = "salt",
      IsActive = true,
      DirectPermissions = [AuthorizationPermissionKeys.InsightsManage]
    });
    using var client = CreateClient(permissions: AuthorizationPermissionKeys.UsersView);

    var response = await client.GetAsync($"/api/Rbac/users/{targetUserId}");
    var summary = await response.Content.ReadFromJsonAsync<RbacUserSummaryContract>();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Contains(AuthorizationPermissionKeys.InsightsManage, summary!.EffectivePermissions);
    Assert.Contains(AuthorizationPermissionKeys.InsightsScan, summary.EffectivePermissions);
  }

  [Fact]
  public async Task AllowUser_manageall_permission_allows_group_restricted_endpoint()
  {
    using var client = CreateClient(permissions: AuthorizationPermissionKeys.BackofficeManageAll);

    var response = await client.GetAsync("/api/Diagnostics");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task Rbac_endpoint_denies_authenticated_user_without_matching_group_or_permission()
  {
    var userId = $"rbac-denied-{Guid.NewGuid():N}";

    await SeedUserAndGroupAsync(
        user: new User
        {
          Id = userId,
          Username = "rbac-denied-user",
          Email = "rbac-denied-user@example.test",
          PasswordHash = "hash",
          Salt = "salt",
          IsActive = true
        });

    using var client = CreateClient(userId: userId);

    var response = await client.GetAsync("/api/Rbac/users");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  private HttpClient CreateClient(string? userId = null, string? permissions = null)
  {
    var client = _factory.CreateClient();
    if (!string.IsNullOrWhiteSpace(userId))
    {
      client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
    }

    if (!string.IsNullOrWhiteSpace(permissions))
    {
      client.DefaultRequestHeaders.Add("X-Test-Permissions", permissions);
    }

    return client;
  }

  private async Task SeedUserAndGroupAsync(User user, UserGroup? group = null)
  {
    using var scope = _factory.Services.CreateScope();
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var groupRepository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();

    if (group is not null)
    {
      await groupRepository.AddItemAsync(group);
    }

    await userRepository.AddItemAsync(user);
  }
}
