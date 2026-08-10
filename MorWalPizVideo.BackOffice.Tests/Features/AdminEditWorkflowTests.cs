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
  public async Task Channel_create_and_update_persist_normalized_short_link_url_and_socials()
  {
    var channelId = $"UC{Guid.NewGuid():N}";
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, PrimaryScenario.ChannelId);

    var createResponse = await client.PostAsJsonAsync("/api/Channels", new
    {
      channelName = "Created channel",
      yTChannelId = channelId,
      shortLinkUrl = " https://morwalpiz.com/sl/ ",
      socials = new[] { new { provider = "Instagram", handler = "@created" }, new { provider = "X", handler = "created" } }
    });
    var created = (await _factory.YTChannelRepository!.GetItemsAsync(channel => channel.ChannelId == channelId)).Single();

    var updateResponse = await client.PutAsJsonAsync($"/api/Channels/{channelId}", new
    {
      channelName = "Updated channel",
      shortLinkUrl = "https://morwalpiz.com/links/",
      socials = new[] { new { provider = "YouTube", handler = "updated" } }
    });
    var updated = await _factory.YTChannelRepository.GetItemAsync(created.Id);

    Assert.Equal(HttpStatusCode.NoContent, createResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    Assert.Equal("https://morwalpiz.com/links", updated!.ShortLinkUrl);
    Assert.Equal("youtube", Assert.Single(updated.Socials).Provider);
  }

  [Fact]
  public async Task Channel_rejects_unsafe_short_link_url()
  {
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, PrimaryScenario.ChannelId);

    var response = await client.PostAsJsonAsync("/api/Channels", new
    {
      channelName = "Unsafe channel",
      yTChannelId = $"UC{Guid.NewGuid():N}",
      shortLinkUrl = "javascript:alert(1)"
    });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Video_link_uses_channel_base_and_code_and_preserves_legacy_url_when_missing()
  {
    var channelId = $"channel-{Guid.NewGuid():N}";
    await _factory.YTChannelRepository!.AddItemAsync(new YTChannel(channelId, "Channel") { ShortLinkUrl = "https://morwalpiz.com/sl/" });
    var match = YouTubeContent.CreateSingleVideo($"video-{Guid.NewGuid():N}", []) with
    {
      OwnerChannelId = channelId,
      YouTubeVideoLinks = new[]
      {
        new YouTubeVideoLink { YouTubeVideoId = "video", ShortLink = new ShortLink("abc", "target", []), ShortLinkUrl = "legacy" },
        new YouTubeVideoLink { YouTubeVideoId = "missing-code", ShortLinkUrl = "legacy-missing-code" }
      }
    };
    match = await _factory.MatchRepository!.AddItemAsync(match);
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, PrimaryScenario.ChannelId);

    var response = await client.GetAsync($"/api/YouTubeVideoLinks/{match.Id}/links");
    var links = await response.Content.ReadFromJsonAsync<List<MorWalPizVideo.BackOffice.DTOs.YouTubeVideoLinkResponse>>();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("https://morwalpiz.com/sl/abc", links![0].ShortLinkUrl);
    Assert.Equal("legacy-missing-code", links[1].ShortLinkUrl);
  }

  [Fact]
  public async Task Video_link_returns_no_url_when_channel_or_base_is_missing()
  {
    var channelId = $"channel-{Guid.NewGuid():N}";
    await _factory.YTChannelRepository!.AddItemAsync(new YTChannel(channelId, "No base"));
    var match = YouTubeContent.CreateSingleVideo($"video-{Guid.NewGuid():N}", []) with
    {
      OwnerChannelId = channelId,
      YouTubeVideoLinks = new[]
      {
        new YouTubeVideoLink { YouTubeVideoId = "no-base", ShortLink = new ShortLink("base", "target", []) },
      }
    };
    match = await _factory.MatchRepository!.AddItemAsync(match);
    var missingChannelMatch = await _factory.MatchRepository.AddItemAsync(
      YouTubeContent.CreateSingleVideo($"video-{Guid.NewGuid():N}", []) with
      {
        OwnerChannelId = $"missing-{Guid.NewGuid():N}",
        YouTubeVideoLinks = new[]
        {
          new YouTubeVideoLink { YouTubeVideoId = "no-channel", ShortLink = new ShortLink("channel", "target", []) },
        }
      });
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, PrimaryScenario.ChannelId);

    var response = await client.GetAsync($"/api/YouTubeVideoLinks/{match.Id}/links");
    var links = await response.Content.ReadFromJsonAsync<List<MorWalPizVideo.BackOffice.DTOs.YouTubeVideoLinkResponse>>();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Null(Assert.Single(links!).ShortLinkUrl);

    var missingChannelResponse = await client.GetAsync($"/api/YouTubeVideoLinks/{missingChannelMatch.Id}/links");
    var missingChannelLinks = await missingChannelResponse.Content.ReadFromJsonAsync<List<MorWalPizVideo.BackOffice.DTOs.YouTubeVideoLinkResponse>>();
    Assert.Null(Assert.Single(missingChannelLinks!).ShortLinkUrl);
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