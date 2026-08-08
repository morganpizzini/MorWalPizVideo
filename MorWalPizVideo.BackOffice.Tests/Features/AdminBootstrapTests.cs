using System.Net;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class AdminBootstrapTests : IClassFixture<BackOfficeWebApplicationFactory>
{
  private readonly BackOfficeWebApplicationFactory _factory;

  public AdminBootstrapTests(BackOfficeWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Bootstrap_admin_rejects_missing_secret_before_user_lookup()
  {
    using var client = _factory.CreateClient();

    var response = await client.PostAsync("/api/User/bootstrap-admin/any-user", content: null);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Bootstrap_admin_rejects_invalid_secret()
  {
    using var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Bootstrap-Secret", "wrong-secret");

    var response = await client.PostAsync("/api/User/bootstrap-admin/any-user", content: null);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Bootstrap_admin_is_idempotent_and_repairs_required_admin_permissions()
  {
    using var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Bootstrap-Secret", "test-bootstrap-secret");

    var firstResponse = await client.PostAsync("/api/User/bootstrap-admin/MorWalPiz", content: null);
    Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

    using (var scope = _factory.Services.CreateScope())
    {
      var groupRepository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
      var adminGroup = await groupRepository.GetByCodeAsync(AuthorizationGroupCodes.Admin);

      Assert.NotNull(adminGroup);
      Assert.True(adminGroup.IsActive);
      Assert.Contains(AuthorizationPermissionKeys.BackofficeAccess, adminGroup.Permissions, StringComparer.OrdinalIgnoreCase);
      Assert.Contains(AuthorizationPermissionKeys.BackofficeManageAll, adminGroup.Permissions, StringComparer.OrdinalIgnoreCase);
      Assert.Contains(AuthorizationPermissionKeys.UsersManage, adminGroup.Permissions, StringComparer.OrdinalIgnoreCase);
    }

    var secondResponse = await client.PostAsync("/api/User/bootstrap-admin/MorWalPiz", content: null);
    Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
  }
}
