using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class RbacChannelAssignmentTests : IClassFixture<BackOfficeWebApplicationFactory>
{
  private readonly BackOfficeWebApplicationFactory _factory;

  public RbacChannelAssignmentTests(BackOfficeWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Manageall_holder_can_assign_replace_and_unassign_channels()
  {
    var managedUserId = $"rbac-channels-{Guid.NewGuid():N}";
    await SeedUserAsync(new User
    {
      Id = managedUserId,
      Username = "rbac-channel-user",
      Email = "rbac-channel-user@example.test",
      PasswordHash = "hash",
      Salt = "salt",
      IsActive = true
    });
    var channelA = await AddChannelAsync($"rbac-channel-a-{Guid.NewGuid():N}");
    var channelB = await AddChannelAsync($"rbac-channel-b-{Guid.NewGuid():N}");
    using var client = CreateClient(permissions: AuthorizationPermissionKeys.BackofficeManageAll);

    var assignResponse = await client.PutAsJsonAsync(
        $"/api/Rbac/users/{managedUserId}/channels",
        new UpdateUserChannelAssignmentsRequestContract { ChannelIds = [channelA.ChannelId, channelB.ChannelId] });
    Assert.Equal(HttpStatusCode.NoContent, assignResponse.StatusCode);

    var detailAfterAssign = await (await client.GetAsync($"/api/Rbac/users/{managedUserId}"))
        .Content.ReadFromJsonAsync<RbacUserSummaryContract>();
    Assert.Equal(2, detailAfterAssign!.ChannelIds.Count);
    Assert.Contains(channelA.ChannelId, detailAfterAssign.ChannelIds);
    Assert.Contains(channelB.ChannelId, detailAfterAssign.ChannelIds);

    var replaceResponse = await client.PutAsJsonAsync(
        $"/api/Rbac/users/{managedUserId}/channels",
        new UpdateUserChannelAssignmentsRequestContract { ChannelIds = [channelB.ChannelId] });
    Assert.Equal(HttpStatusCode.NoContent, replaceResponse.StatusCode);

    var detailAfterReplace = await (await client.GetAsync($"/api/Rbac/users/{managedUserId}"))
        .Content.ReadFromJsonAsync<RbacUserSummaryContract>();
    Assert.Equal([channelB.ChannelId], detailAfterReplace!.ChannelIds);

    using var scope = _factory.Services.CreateScope();
    var ownerRepository = scope.ServiceProvider.GetRequiredService<IUserChannelOwnerRepository>();
    var allRecords = await ownerRepository.GetItemsAsync(owner => owner.UserId == managedUserId);
    var channelARecord = Assert.Single(allRecords, owner => owner.ChannelId == channelA.ChannelId);
    Assert.False(channelARecord.IsActive);

    var unassignResponse = await client.PutAsJsonAsync(
        $"/api/Rbac/users/{managedUserId}/channels",
        new UpdateUserChannelAssignmentsRequestContract { ChannelIds = [] });
    Assert.Equal(HttpStatusCode.NoContent, unassignResponse.StatusCode);

    var detailAfterUnassign = await (await client.GetAsync($"/api/Rbac/users/{managedUserId}"))
        .Content.ReadFromJsonAsync<RbacUserSummaryContract>();
    Assert.Empty(detailAfterUnassign!.ChannelIds);
  }

  [Fact]
  public async Task Assignment_rejects_channel_ids_that_do_not_exist()
  {
    var managedUserId = $"rbac-channels-invalid-{Guid.NewGuid():N}";
    await SeedUserAsync(new User
    {
      Id = managedUserId,
      Username = "rbac-invalid-channel-user",
      Email = "rbac-invalid-channel-user@example.test",
      PasswordHash = "hash",
      Salt = "salt",
      IsActive = true
    });
    using var client = CreateClient(permissions: AuthorizationPermissionKeys.BackofficeManageAll);

    var response = await client.PutAsJsonAsync(
        $"/api/Rbac/users/{managedUserId}/channels",
        new UpdateUserChannelAssignmentsRequestContract { ChannelIds = ["does-not-exist"] });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Theory]
  [InlineData(AuthorizationPermissionKeys.UsersPermissionsManage)]
  [InlineData(AuthorizationPermissionKeys.UsersManage)]
  public async Task Assignment_is_denied_for_permissions_other_than_manageall(string permission)
  {
    var managedUserId = $"rbac-channels-denied-{Guid.NewGuid():N}";
    await SeedUserAsync(new User
    {
      Id = managedUserId,
      Username = "rbac-denied-channel-user",
      Email = "rbac-denied-channel-user@example.test",
      PasswordHash = "hash",
      Salt = "salt",
      IsActive = true
    });
    var channel = await AddChannelAsync($"rbac-channel-denied-{Guid.NewGuid():N}");
    using var client = CreateClient(permissions: permission);

    var response = await client.PutAsJsonAsync(
        $"/api/Rbac/users/{managedUserId}/channels",
        new UpdateUserChannelAssignmentsRequestContract { ChannelIds = [channel.ChannelId] });

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

  private async Task SeedUserAsync(User user)
  {
    using var scope = _factory.Services.CreateScope();
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    await userRepository.AddItemAsync(user);
  }

  private async Task<YTChannel> AddChannelAsync(string channelId)
      => await _factory.YTChannelRepository!.AddItemAsync(new YTChannel(channelId, channelId));
}
