using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
  public async Task Channel_create_persists_isSHIT()
  {
    var channelId = $"UC{Guid.NewGuid():N}";
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, PrimaryScenario.ChannelId);

    var response = await client.PostAsJsonAsync("/api/Channels", new
    {
      channelName = "Shooting channel",
      yTChannelId = channelId,
      isSHIT = true
    });
    var created = (await _factory.YTChannelRepository!.GetItemsAsync(channel => channel.ChannelId == channelId)).Single();

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.True(created.IsSHIT);
  }

  [Fact]
  public async Task Channel_mutations_invalidate_all_public_shooting_ita_cache_tags()
  {
    var channelId = $"UC{Guid.NewGuid():N}";
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, PrimaryScenario.ChannelId);
    var expectedResetKeys = new[] { CacheKeys.Channels, CacheKeys.Matches, CacheKeys.QuickLinks, CacheKeys.ChannelNews };
    var expectedPurgedTags = new[] { CacheKeys.Channels, CacheKeys.Matches, CacheKeys.QuickLinks, ApiTagCacheKeys.ChannelNews };

    _factory.CrossApiService.Clear();
    var createResponse = await client.PostAsJsonAsync("/api/Channels", new
    {
      channelName = "Cache test channel",
      yTChannelId = channelId
    });
    Assert.Equal(HttpStatusCode.NoContent, createResponse.StatusCode);
    Assert.Equal(expectedResetKeys, _factory.CrossApiService.ResetKeys);
    Assert.Equal(expectedPurgedTags, _factory.CrossApiService.PurgedTags);

    _factory.CrossApiService.Clear();
    var updateResponse = await client.PutAsJsonAsync($"/api/Channels/{channelId}", new
    {
      channelName = "Cache test Shooting channel",
      isSHIT = true
    });
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    Assert.Equal(expectedResetKeys, _factory.CrossApiService.ResetKeys);
    Assert.Equal(expectedPurgedTags, _factory.CrossApiService.PurgedTags);

    _factory.CrossApiService.Clear();
    var deleteResponse = await client.DeleteAsync($"/api/Channels/{channelId}");
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    Assert.Equal(expectedResetKeys, _factory.CrossApiService.ResetKeys);
    Assert.Equal(expectedPurgedTags, _factory.CrossApiService.PurgedTags);
  }

  [Fact]
  public async Task Channel_logo_upload_remove_and_invalid_upload_preserve_channel_and_cache_contracts()
  {
    var channelId = $"channel-logo-{Guid.NewGuid():N}";
    await _factory.YTChannelRepository!.AddItemAsync(new YTChannel(channelId, "Logo channel"));
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, channelId);
    var expectedResetKeys = new[] { CacheKeys.Channels, CacheKeys.Matches, CacheKeys.QuickLinks, CacheKeys.ChannelNews };
    var expectedPurgedTags = new[] { CacheKeys.Channels, CacheKeys.Matches, CacheKeys.QuickLinks, ApiTagCacheKeys.ChannelNews };

    await using var png = await CreatePngAsync(1000, 600);
    using var upload = new MultipartFormDataContent();
    var pngContent = new ByteArrayContent(png.ToArray());
    pngContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
    upload.Add(pngContent, "logo", "channel-logo.png");

    _factory.CrossApiService.Clear();
    var uploadResponse = await client.PostAsync($"/api/Channels/{channelId}/logo", upload);
    var uploaded = (await _factory.YTChannelRepository.GetItemsAsync(channel => channel.ChannelId == channelId)).Single();

    Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
    Assert.NotNull(uploaded);
    Assert.NotEmpty(uploaded!.ChannelLogoStorageKey);
    Assert.StartsWith("mock://blob/channel-logos/", uploaded.ChannelLogoUrl, StringComparison.Ordinal);
    Assert.Equal(expectedResetKeys, _factory.CrossApiService.ResetKeys);
    Assert.Equal(expectedPurgedTags, _factory.CrossApiService.PurgedTags);

    _factory.CrossApiService.Clear();
    using var invalidUpload = new MultipartFormDataContent();
    var invalidContent = new ByteArrayContent("not-an-image"u8.ToArray());
    invalidContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
    invalidUpload.Add(invalidContent, "logo", "invalid.png");
    var invalidResponse = await client.PostAsync($"/api/Channels/{channelId}/logo", invalidUpload);
    var afterInvalidUpload = (await _factory.YTChannelRepository.GetItemsAsync(channel => channel.ChannelId == channelId)).Single();

    Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
    Assert.Equal(uploaded.ChannelLogoStorageKey, afterInvalidUpload!.ChannelLogoStorageKey);
    Assert.Equal(uploaded.ChannelLogoUrl, afterInvalidUpload.ChannelLogoUrl);
    Assert.Empty(_factory.CrossApiService.ResetKeys);
    Assert.Empty(_factory.CrossApiService.PurgedTags);

    _factory.CrossApiService.Clear();
    var removeResponse = await client.DeleteAsync($"/api/Channels/{channelId}/logo");
    var removed = (await _factory.YTChannelRepository.GetItemsAsync(channel => channel.ChannelId == channelId)).Single();

    Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
    Assert.Equal(string.Empty, removed!.ChannelLogoStorageKey);
    Assert.Equal(string.Empty, removed.ChannelLogoUrl);
    Assert.Equal(expectedResetKeys, _factory.CrossApiService.ResetKeys);
    Assert.Equal(expectedPurgedTags, _factory.CrossApiService.PurgedTags);
  }

  private static async Task<MemoryStream> CreatePngAsync(int width, int height)
  {
    var stream = new MemoryStream();
    using var image = new Image<Rgba32>(width, height);
    await image.SaveAsPngAsync(stream);
    stream.Position = 0;
    return stream;
  }

  [Fact]
  public async Task QuickLinks_create_uses_channel_scope_and_rejects_global_duplicate_slug()
  {
    var slug = $"links-{Guid.NewGuid():N}";
    using var primaryClient = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, PrimaryScenario.ChannelId);
    var firstResponse = await primaryClient.PostAsJsonAsync("/api/QuickLinks", new
    {
      title = "Primary links",
      url = slug,
      links = Array.Empty<object>()
    });
    var first = (await _factory.QuickLinksRepository!.GetItemsAsync()).Single(link => link.Url == slug);

    var secondChannelId = $"channel-{Guid.NewGuid():N}";
    await _factory.YTChannelRepository!.AddItemAsync(new YTChannel(secondChannelId, "Second channel"));
    using var secondClient = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, secondChannelId);
    var duplicateResponse = await secondClient.PostAsJsonAsync("/api/QuickLinks", new
    {
      title = "Duplicate links",
      url = slug,
      links = Array.Empty<object>()
    });

    Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
    Assert.Equal(PrimaryScenario.ChannelId, first.ChannelId);
    Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
  }

  [Fact]
  public async Task QuickLinks_create_allows_multiple_linktrees_for_one_channel()
  {
    using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, PrimaryScenario.ChannelId);
    var firstSlug = $"links-one-{Guid.NewGuid():N}";
    var secondSlug = $"links-two-{Guid.NewGuid():N}";

    var firstResponse = await client.PostAsJsonAsync("/api/QuickLinks", new
    {
      title = "First links",
      url = firstSlug,
      links = Array.Empty<object>()
    });
    var secondResponse = await client.PostAsJsonAsync("/api/QuickLinks", new
    {
      title = "Second links",
      url = secondSlug,
      links = Array.Empty<object>()
    });

    var linktrees = (await _factory.QuickLinksRepository!.GetItemsAsync())
      .Where(link => link.ChannelId == PrimaryScenario.ChannelId)
      .ToList();

    Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);
    Assert.Contains(linktrees, link => link.Url == firstSlug);
    Assert.Contains(linktrees, link => link.Url == secondSlug);
  }

  [Fact]
  public async Task QuickLinks_update_and_delete_require_the_persisted_channel_owner()
  {
    var slug = $"links-owner-{Guid.NewGuid():N}";
    using var ownerClient = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, PrimaryScenario.ChannelId);
    await ownerClient.PostAsJsonAsync("/api/QuickLinks", new
    {
      title = "Owned links",
      url = slug,
      links = Array.Empty<object>()
    });
    var linktree = (await _factory.QuickLinksRepository!.GetItemsAsync()).Single(link => link.Url == slug);

    var otherChannelId = $"channel-{Guid.NewGuid():N}";
    await _factory.YTChannelRepository!.AddItemAsync(new YTChannel(otherChannelId, "Other channel"));
    using var missingScopeClient = _factory.CreateClient();
    missingScopeClient.DefaultRequestHeaders.Add("X-Test-Permissions", AuthorizationPermissionKeys.BackofficeManageAll);
    using var otherChannelClient = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, otherChannelId);

    var missingScopeResponse = await missingScopeClient.DeleteAsync($"/api/QuickLinks/{linktree.Id}");
    var updateResponse = await otherChannelClient.PutAsJsonAsync($"/api/QuickLinks/{linktree.Id}", new
    {
      title = "Unauthorized update",
      url = $"updated-{Guid.NewGuid():N}",
      links = Array.Empty<object>()
    });
    var deleteResponse = await otherChannelClient.DeleteAsync($"/api/QuickLinks/{linktree.Id}");
    var persisted = await _factory.QuickLinksRepository.GetItemAsync(linktree.Id);

    Assert.Equal(HttpStatusCode.BadRequest, missingScopeResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    Assert.NotNull(persisted);
    Assert.Equal("Owned links", persisted!.Title);
    Assert.Equal(PrimaryScenario.ChannelId, persisted.ChannelId);
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