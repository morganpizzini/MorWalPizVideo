using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using Xunit;
using MorWalPiz.Contracts.Contracts;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class ChannelTenancyAuthorizationTests : IClassFixture<BackOfficeWebApplicationFactory>
{
  private readonly BackOfficeWebApplicationFactory _factory;

  public ChannelTenancyAuthorizationTests(BackOfficeWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Scoped_requests_distinguish_missing_unknown_and_inaccessible_channel_headers()
  {
    using var missingHeaderClient = CreateClient(AuthorizationPermissionKeys.CompilationsView);
    using var unknownHeaderClient = CreateClient(AuthorizationPermissionKeys.CompilationsView, channelId: "missing-channel");
    var inaccessibleChannel = await AddChannelAsync($"inaccessible-{Guid.NewGuid():N}");
    using var inaccessibleHeaderClient = CreateClient(AuthorizationPermissionKeys.CompilationsView, channelId: inaccessibleChannel.ChannelId);

    Assert.Equal(HttpStatusCode.BadRequest, (await missingHeaderClient.GetAsync("/api/Compilations")).StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, (await unknownHeaderClient.GetAsync("/api/Compilations")).StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, (await inaccessibleHeaderClient.GetAsync("/api/Compilations")).StatusCode);
  }

  [Fact]
  public async Task Admin_can_select_and_manage_a_channel_other_than_the_default_channel()
  {
    var channel = await AddChannelAsync($"admin-channel-{Guid.NewGuid():N}");
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, channel.ChannelId);

    var response = await client.GetAsync($"/api/Channels/{channel.ChannelId}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task Channels_endpoint_returns_the_global_catalog_for_channels_admin_permission()
  {
    var firstChannel = await AddChannelAsync($"global-first-{Guid.NewGuid():N}");
    var secondChannel = await AddChannelAsync($"global-second-{Guid.NewGuid():N}");
    using var client = CreateClient(AuthorizationPermissionKeys.ChannelsAdmin);

    var response = await client.GetAsync("/api/Channels");
    var payload = await response.Content.ReadFromJsonAsync<List<ChannelContract>>();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(payload);
    Assert.Contains(payload!, channel => channel.ChannelId == firstChannel.ChannelId);
    Assert.Contains(payload!, channel => channel.ChannelId == secondChannel.ChannelId);
  }

  [Fact]
  public async Task Global_channels_endpoint_denies_channel_view_without_channel_admin_permission()
  {
    using var client = CreateClient(AuthorizationPermissionKeys.ChannelsView);

    var response = await client.GetAsync("/api/Channels");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task Accessible_channels_endpoint_returns_owned_channels_for_backoffice_access_only_users()
  {
    var ownedChannel = await AddChannelAsync($"owned-accessible-{Guid.NewGuid():N}");
    var unownedChannel = await AddChannelAsync($"unowned-accessible-{Guid.NewGuid():N}");
    await AddOwnerAsync("test-user-id", ownedChannel.ChannelId);
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeAccess);

    var response = await client.GetAsync("/api/Channels/accessible");
    var payload = await response.Content.ReadFromJsonAsync<List<ChannelContract>>();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(payload);
    Assert.Contains(payload!, channel => channel.ChannelId == ownedChannel.ChannelId);
    Assert.DoesNotContain(payload!, channel => channel.ChannelId == unownedChannel.ChannelId);
  }

  [Fact]
  public async Task Global_channels_endpoint_denies_backoffice_access_only_users()
  {
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeAccess);

    var response = await client.GetAsync("/api/Channels");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task Owner_can_read_update_and_delete_a_channel_without_the_selected_channel_header()
  {
    var ownedChannel = await AddChannelAsync($"owner-managed-{Guid.NewGuid():N}");
    await AddOwnerAsync("test-user-id", ownedChannel.ChannelId);
    using var client = CreateClient(AuthorizationPermissionKeys.ChannelsManage);

    var getResponse = await client.GetAsync($"/api/Channels/{ownedChannel.ChannelId}");
    Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

    var updateResponse = await client.PutAsJsonAsync(
        $"/api/Channels/{ownedChannel.ChannelId}",
        new { channelName = "Renamed by owner" });
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    var updated = await _factory.YTChannelRepository!.GetItemAsync(ownedChannel.Id);
    Assert.Equal("Renamed by owner", updated!.ChannelName);

    var deleteResponse = await client.DeleteAsync($"/api/Channels/{ownedChannel.ChannelId}");
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    var remaining = await _factory.YTChannelRepository.GetItemsAsync(channel => channel.ChannelId == ownedChannel.ChannelId);
    Assert.Empty(remaining);
  }

  [Fact]
  public async Task Owner_can_manage_a_channel_even_when_the_selected_channel_header_points_elsewhere()
  {
    var ownedChannel = await AddChannelAsync($"owner-header-mismatch-{Guid.NewGuid():N}");
    var otherChannel = await AddChannelAsync($"other-{Guid.NewGuid():N}");
    await AddOwnerAsync("test-user-id", ownedChannel.ChannelId);
    using var client = CreateClient(AuthorizationPermissionKeys.ChannelsManage, channelId: otherChannel.ChannelId);

    var response = await client.GetAsync($"/api/Channels/{ownedChannel.ChannelId}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task Non_owner_is_denied_channel_read_update_and_delete_without_disclosing_existence()
  {
    var channel = await AddChannelAsync($"unowned-{Guid.NewGuid():N}");
    using var client = CreateClient(AuthorizationPermissionKeys.ChannelsManage);

    var getResponse = await client.GetAsync($"/api/Channels/{channel.ChannelId}");
    var updateResponse = await client.PutAsJsonAsync($"/api/Channels/{channel.ChannelId}", new { channelName = "Not allowed" });
    var deleteResponse = await client.DeleteAsync($"/api/Channels/{channel.ChannelId}");

    Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
  }

  [Fact]
  public async Task Admin_can_reassign_an_api_key_to_any_existing_channel_but_owner_cannot()
  {
    var targetChannel = await AddChannelAsync($"key-target-{Guid.NewGuid():N}");
    var apiKey = await AddApiKeyAsync(new ApiKey
    {
      Id = $"key-{Guid.NewGuid():N}",
      Name = $"key-{Guid.NewGuid():N}",
      ChannelId = PrimaryScenario.ChannelId,
      Key = "hash"
    });

    using var adminClient = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, PrimaryScenario.ChannelId);
    var adminResponse = await adminClient.PutAsJsonAsync($"/api/ApiKeys/{apiKey.Id}", new { channelId = targetChannel.ChannelId });
    var updated = await _factory.Services.GetRequiredService<IApiKeyRepository>().GetItemAsync(apiKey.Id!);

    Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    Assert.Equal(targetChannel.ChannelId, updated!.ChannelId);

    var ownerKey = await AddApiKeyAsync(new ApiKey
    {
      Id = $"owner-key-{Guid.NewGuid():N}",
      Name = $"owner-key-{Guid.NewGuid():N}",
      ChannelId = PrimaryScenario.ChannelId,
      Key = "hash"
    });
    using var ownerClient = CreateClient(AuthorizationPermissionKeys.ApiKeysUpdate, PrimaryScenario.ChannelId);
    var ownerResponse = await ownerClient.PutAsJsonAsync($"/api/ApiKeys/{ownerKey.Id}", new { channelId = targetChannel.ChannelId });

    Assert.Equal(HttpStatusCode.NotFound, ownerResponse.StatusCode);
  }

  [Fact]
  public async Task Repository_backed_api_key_is_bound_to_its_channel_and_follows_admin_reassignment()
  {
    var boundChannel = await AddChannelAsync($"key-bound-{Guid.NewGuid():N}");
    var targetChannel = await AddChannelAsync($"key-reassigned-{Guid.NewGuid():N}");
    var plaintextKey = $"test-api-key-{Guid.NewGuid():N}";
    var apiKey = await AddApiKeyAsync(new ApiKey
    {
      Id = $"key-{Guid.NewGuid():N}",
      Name = $"key-{Guid.NewGuid():N}",
      Key = _factory.Services.GetRequiredService<IApiKeyService>().HashApiKey(plaintextKey),
      ChannelId = boundChannel.ChannelId
    });

    using var boundClient = CreateClient(AuthorizationPermissionKeys.ApiKeysView, boundChannel.ChannelId);
    boundClient.DefaultRequestHeaders.Add("X-Test-ApiKey", plaintextKey);
    using var boundResponse = await boundClient.GetAsync("/api/ApiKeys");

    using var mismatchClient = CreateClient(AuthorizationPermissionKeys.ApiKeysView, targetChannel.ChannelId);
    mismatchClient.DefaultRequestHeaders.Add("X-Test-ApiKey", plaintextKey);
    using var mismatchResponse = await mismatchClient.GetAsync("/api/ApiKeys");

    using var adminClient = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, boundChannel.ChannelId);
    using var reassignmentResponse = await adminClient.PutAsJsonAsync(
        $"/api/ApiKeys/{apiKey.Id}",
        new { channelId = targetChannel.ChannelId });

    using var targetClient = CreateClient(AuthorizationPermissionKeys.ApiKeysView, targetChannel.ChannelId);
    targetClient.DefaultRequestHeaders.Add("X-Test-ApiKey", plaintextKey);
    using var targetResponse = await targetClient.GetAsync("/api/ApiKeys");

    Assert.Equal(HttpStatusCode.OK, boundResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, mismatchResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, reassignmentResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, targetResponse.StatusCode);
  }

  [Fact]
  public async Task Collaborator_can_read_another_channel_video_but_cannot_mutate_it()
  {
    var collaboratorChannel = await AddChannelAsync($"collaborator-{Guid.NewGuid():N}");
    await AddOwnerAsync("test-user-id", collaboratorChannel.ChannelId);
    var videoId = $"collaborator-video-{Guid.NewGuid():N}";
    var match = await _factory.MatchRepository!.AddItemAsync(
        YouTubeContent.CreateSingleVideo(videoId, []) with
        {
          Id = $"match-{Guid.NewGuid():N}",
          OwnerChannelId = PrimaryScenario.ChannelId,
          CreatorUserId = "another-user",
          VideoRefs = [new VideoRef(videoId, [], channelIds: [collaboratorChannel.ChannelId])]
        });

    using var client = CreateClient(AuthorizationPermissionKeys.VideosView, collaboratorChannel.ChannelId);
    var readResponse = await client.GetAsync($"/api/Videos/{match.Id}");
    using var mutationClient = CreateClient(AuthorizationPermissionKeys.VideosUpdate, collaboratorChannel.ChannelId);
    var mutationResponse = await mutationClient.PutAsJsonAsync($"/api/Videos/{match.Id}", new
    {
      title = "not allowed",
      description = string.Empty,
      url = string.Empty,
      thumbnailVideoId = videoId,
      categories = Array.Empty<string>()
    });

    Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, mutationResponse.StatusCode);
  }

  [Fact]
  public async Task Collaborator_cannot_bulk_import_into_another_channel_video()
  {
    var collaboratorChannel = await AddChannelAsync($"bulk-collaborator-{Guid.NewGuid():N}");
    await AddOwnerAsync("test-user-id", collaboratorChannel.ChannelId);
    var videoId = $"bulk-collaborator-video-{Guid.NewGuid():N}";
    var match = await _factory.MatchRepository!.AddItemAsync(
        YouTubeContent.CreateSingleVideo(videoId, []) with
        {
          Id = $"bulk-match-{Guid.NewGuid():N}",
          OwnerChannelId = PrimaryScenario.ChannelId,
          CreatorUserId = "another-user",
          VideoRefs = [new VideoRef(videoId, [], channelIds: [collaboratorChannel.ChannelId])]
        });

    using var client = CreateClient(AuthorizationPermissionKeys.VideosImport, collaboratorChannel.ChannelId);
    var response = await client.PostAsJsonAsync("/api/Videos/bulk-import", new
    {
      items = new[]
      {
        new
        {
          videoId = $"new-bulk-video-{Guid.NewGuid():N}",
          categories = new[] { "300000000000000000000001" },
          target = match.ContentId
        }
      }
    });
    var body = await response.Content.ReadAsStringAsync();
    var updatedMatch = await _factory.MatchRepository.GetItemAsync(match.Id);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Contains("Target content was not found or is not accessible", body, StringComparison.Ordinal);
    Assert.Single(updatedMatch!.VideoRefs);
  }

  [Fact]
  public async Task Compilation_owner_can_include_a_video_readable_through_another_owned_channel()
  {
    var sourceChannel = await AddChannelAsync($"source-{Guid.NewGuid():N}");
    await AddOwnerAsync("test-user-id", sourceChannel.ChannelId);
    var videoId = $"cross-channel-video-{Guid.NewGuid():N}";
    await _factory.MatchRepository!.AddItemAsync(
        YouTubeContent.CreateSingleVideo(videoId, []) with
        {
          Id = $"match-{Guid.NewGuid():N}",
          OwnerChannelId = sourceChannel.ChannelId,
          CreatorUserId = "another-user",
          VideoRefs = [new VideoRef(videoId, [], channelIds: [sourceChannel.ChannelId])]
        });

    using var client = CreateClient(AuthorizationPermissionKeys.CompilationsCreate, PrimaryScenario.ChannelId);
    var response = await client.PostAsJsonAsync("/api/Compilations", new
    {
      title = "Cross-channel compilation",
      description = "Readable source video",
      url = $"cross-channel-{Guid.NewGuid():N}",
      videos = new[] { videoId }
    });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  private HttpClient CreateClient(string permission, string? channelId = null)
  {
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Test-Permissions", permission);
    if (channelId is not null)
    {
      client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
    }

    return client;
  }

  private async Task<YTChannel> AddChannelAsync(string channelId)
      => await _factory.YTChannelRepository!.AddItemAsync(new YTChannel(channelId, channelId));

  private async Task AddOwnerAsync(string userId, string channelId)
  {
    using var scope = _factory.Services.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IUserChannelOwnerRepository>();
    await repository.AddItemAsync(new UserChannelOwner
    {
      UserId = userId,
      ChannelId = channelId,
      IsActive = true
    });
  }

  private async Task<ApiKey> AddApiKeyAsync(ApiKey apiKey)
  {
    using var scope = _factory.Services.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
    return await repository.AddItemAsync(apiKey);
  }
}
