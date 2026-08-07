using System.Net;
using System.Net.Http.Json;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class RbacCrudAssignmentTests : IClassFixture<BackOfficeWebApplicationFactory>
{
  private readonly BackOfficeWebApplicationFactory _factory;

  public RbacCrudAssignmentTests(BackOfficeWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Rbac_crud_assignments_support_multi_group_membership_and_direct_permissions()
  {
    var managedUserId = $"rbac-managed-{Guid.NewGuid():N}";
    await SeedUserAsync(new User
    {
      Id = managedUserId,
      Username = "rbac-managed-user",
      Email = "rbac-managed-user@example.test",
      PasswordHash = "hash",
      Salt = "salt",
      IsActive = true
    });

    using var adminClient = CreateClient(permissions: "canaccessbackoffice");

    var createGroupAResponse = await adminClient.PostAsJsonAsync("/api/Rbac/groups", new UpsertRbacGroupRequestContract
    {
      Code = "authors",
      Name = "Authors",
      Description = "Can write articles",
      IsActive = true,
      Permissions = ["articles.write"]
    });

    var createGroupBResponse = await adminClient.PostAsJsonAsync("/api/Rbac/groups", new UpsertRbacGroupRequestContract
    {
      Code = "moderators",
      Name = "Moderators",
      Description = "Can moderate comments",
      IsActive = true,
      Permissions = ["comments.moderate"]
    });

    Assert.Equal(HttpStatusCode.Created, createGroupAResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Created, createGroupBResponse.StatusCode);

    var groupA = await createGroupAResponse.Content.ReadFromJsonAsync<RbacGroupContract>();
    var groupB = await createGroupBResponse.Content.ReadFromJsonAsync<RbacGroupContract>();
    Assert.NotNull(groupA);
    Assert.NotNull(groupB);

    var assignGroupsResponse = await adminClient.PutAsJsonAsync(
        $"/api/Rbac/users/{managedUserId}/groups",
        new UpdateUserGroupMembershipsRequestContract
        {
          GroupIds = [groupA!.Id, groupB!.Id]
        });
    Assert.Equal(HttpStatusCode.NoContent, assignGroupsResponse.StatusCode);

    var assignDirectPermissionsResponse = await adminClient.PutAsJsonAsync(
        $"/api/Rbac/users/{managedUserId}/permissions",
        new UpdateUserDirectPermissionsRequestContract
        {
          Permissions = ["canAccessBackOffice", "reports.read"]
        });
    Assert.Equal(HttpStatusCode.NoContent, assignDirectPermissionsResponse.StatusCode);

    var usersResponse = await adminClient.GetAsync("/api/Rbac/users");
    Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);
    var users = await usersResponse.Content.ReadFromJsonAsync<List<RbacUserSummaryContract>>();
    var managedUser = Assert.Single(users!, user => user.Id == managedUserId);

    Assert.Contains(groupA.Id, managedUser.GroupIds);
    Assert.Contains(groupB.Id, managedUser.GroupIds);
    Assert.Contains("articles.write", managedUser.EffectivePermissions);
    Assert.Contains("comments.moderate", managedUser.EffectivePermissions);
    Assert.Contains("reports.read", managedUser.EffectivePermissions);
    Assert.Contains("canaccessbackoffice", managedUser.EffectivePermissions);
    Assert.True(managedUser.CanAccessBackoffice);

    var deleteGroupResponse = await adminClient.DeleteAsync($"/api/Rbac/groups/{groupA.Id}");
    Assert.Equal(HttpStatusCode.NoContent, deleteGroupResponse.StatusCode);

    var usersAfterDeleteResponse = await adminClient.GetAsync("/api/Rbac/users");
    Assert.Equal(HttpStatusCode.OK, usersAfterDeleteResponse.StatusCode);
    var usersAfterDelete = await usersAfterDeleteResponse.Content.ReadFromJsonAsync<List<RbacUserSummaryContract>>();
    var managedUserAfterDelete = Assert.Single(usersAfterDelete!, user => user.Id == managedUserId);

    Assert.DoesNotContain(groupA.Id, managedUserAfterDelete.GroupIds);
    Assert.Contains(groupB.Id, managedUserAfterDelete.GroupIds);
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

  private async Task SeedUserAsync(User user)
  {
    using var scope = _factory.Services.CreateScope();
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    await userRepository.AddItemAsync(user);
  }
}
