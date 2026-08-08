using System.Net;
using System.Net.Http.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class AdminEditWorkflowTests : IClassFixture<BackOfficeWebApplicationFactory>
{
  private readonly BackOfficeWebApplicationFactory _factory;

  public AdminEditWorkflowTests(BackOfficeWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Channel_update_uses_existing_route_and_persists_name()
  {
    var channelId = $"channel-{Guid.NewGuid():N}";
    var channel = await _factory.YTChannelRepository!.AddItemAsync(new YTChannel(channelId, "Old name"));
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, channelId);

    var response = await client.PutAsJsonAsync($"/api/Channels/{channelId}", new { channelName = "Updated name" });
    var updated = await _factory.YTChannelRepository.GetItemAsync(channel.Id);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Equal("Updated name", updated!.ChannelName);
  }

  [Fact]
  public async Task Channel_create_honors_submitted_youtube_channel_id()
  {
    var channelId = $"UC{Guid.NewGuid():N}";
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, PrimaryScenario.ChannelId);

    var response = await client.PostAsJsonAsync("/api/Channels", new
    {
      channelName = "Created channel",
      yTChannelId = $"  {channelId}  "
    });
    var created = (await _factory.YTChannelRepository!.GetItemsAsync(channel => channel.ChannelId == channelId)).SingleOrDefault();

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.NotNull(created);
    Assert.Equal("Created channel", created!.ChannelName);
  }

  [Fact]
  public async Task Video_update_persists_video_references_submitted_by_edit_form()
  {
    var videoId = $"video-{Guid.NewGuid():N}";
    var match = YouTubeContent.CreateSingleVideo(videoId, []) with
    {
      CreatorUserId = "test-user-id",
      OwnerChannelId = PrimaryScenario.ChannelId,
      Title = "Original"
    };
    match = await _factory.MatchRepository!.AddItemAsync(match);
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, PrimaryScenario.ChannelId);

    var response = await client.PutAsJsonAsync($"/api/Videos/{match.Id}", new
    {
      title = "Updated",
      description = string.Empty,
      url = string.Empty,
      thumbnailVideoId = videoId,
      categories = Array.Empty<string>(),
      videoRefs = new[]
        {
                new { youtubeId = videoId, categories = Array.Empty<object>(), channelIds = new[] { "owned-channel" } }
            }
    });
    var updated = await _factory.MatchRepository.GetItemAsync(match.Id);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Equal("owned-channel", Assert.Single(updated!.VideoRefs).ChannelIds.Single());
  }

  private HttpClient CreateClient(string permission, string channelId)
  {
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Test-Permissions", permission);
    client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
    return client;
  }
}