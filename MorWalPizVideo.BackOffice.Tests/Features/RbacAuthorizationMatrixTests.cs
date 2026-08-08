using System.Net;
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
  public async Task Rbac_endpoint_allows_user_with_inherited_backoffice_access_permission()
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
          Permissions = [AuthorizationPermissionKeys.BackofficeAccess]
        });

    using var client = CreateClient(userId: userId);

    var response = await client.GetAsync("/api/Rbac/users");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
