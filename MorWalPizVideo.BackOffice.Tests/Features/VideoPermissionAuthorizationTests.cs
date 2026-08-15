using System.Net;
using System.Net.Http.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class VideoPermissionAuthorizationTests : IClassFixture<BackOfficeWebApplicationFactory>
{
  private readonly BackOfficeWebApplicationFactory _factory;

  public VideoPermissionAuthorizationTests(BackOfficeWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task Video_reads_require_authentication_and_view_permission()
  {
    using var anonymousClient = CreateClient(anonymous: true);
    using var deniedClient = CreateClient();
    using var viewClient = CreateClient(permissions: AuthorizationPermissionKeys.VideosView);

    Assert.Equal(HttpStatusCode.Unauthorized, (await anonymousClient.GetAsync("/api/Videos")).StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, (await deniedClient.GetAsync("/api/Videos")).StatusCode);
    Assert.Equal(HttpStatusCode.OK, (await viewClient.GetAsync("/api/Videos")).StatusCode);
  }

  [Theory]
  [InlineData(AuthorizationPermissionKeys.VideosUpdate)]
  [InlineData(AuthorizationPermissionKeys.VideosManage)]
  [InlineData(AuthorizationPermissionKeys.BackofficeManageAll)]
  public async Task Video_update_accepts_exact_manage_or_manageall_permission(string permission)
  {
    using var client = CreateClient(permissions: permission);

    var response = await client.PutAsJsonAsync("/api/Videos/missing-video", new
    {
      title = "Missing",
      description = string.Empty,
      url = string.Empty,
      thumbnailVideoId = string.Empty,
      categories = Array.Empty<string>()
    });

    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task Video_record_ownership_remains_required_after_permission_check()
  {
    var matchId = (await _factory.MatchRepository!.GetItemsAsync()).First().Id;
    using var client = CreateClient(
        userId: $"not-owner-{Guid.NewGuid():N}",
        permissions: AuthorizationPermissionKeys.VideosView);

    var response = await client.GetAsync($"/api/Videos/{matchId}");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task Backoffice_manageall_can_list_and_read_videos_owned_by_other_users()
  {
    var matchId = (await _factory.MatchRepository!.GetItemsAsync()).First().Id;
    using var client = CreateClient(
        userId: $"global-manager-{Guid.NewGuid():N}",
        permissions: AuthorizationPermissionKeys.BackofficeManageAll);

    var listResponse = await client.GetAsync("/api/Videos");
    var detailResponse = await client.GetAsync($"/api/Videos/{matchId}");

    Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
  }

  [Fact]
  public async Task Video_import_rejects_videos_create_but_accepts_documented_import_permission()
  {
    using var createClient = CreateClient(permissions: AuthorizationPermissionKeys.VideosCreate);
    using var importClient = CreateClient(permissions: AuthorizationPermissionKeys.VideosImport);
    var request = new { videoId = $"import-{Guid.NewGuid():N}", categories = Array.Empty<string>() };

    var deniedResponse = await createClient.PostAsJsonAsync("/api/Videos/ImportVideo", request);
    var allowedResponse = await importClient.PostAsJsonAsync("/api/Videos/ImportVideo", request);

    Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
    Assert.NotEqual(HttpStatusCode.Forbidden, allowedResponse.StatusCode);
    Assert.NotEqual(HttpStatusCode.Unauthorized, allowedResponse.StatusCode);
  }

  [Fact]
  public async Task Single_video_import_assigns_the_selected_channel_to_the_video_reference()
  {
    using var client = CreateClient(permissions: AuthorizationPermissionKeys.VideosImport);
    var videoId = $"import-{Guid.NewGuid():N}";

    var response = await client.PostAsJsonAsync(
        "/api/Videos/ImportVideo",
      new { videoId, categories = new[] { "300000000000000000000001" } });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    var importedMatch = (await _factory.MatchRepository!.GetItemsAsync())
        .Single(match => match.ContentId == videoId);

    Assert.Equal(PrimaryScenario.ChannelId, importedMatch.OwnerChannelId);
    Assert.Contains(PrimaryScenario.ChannelId, importedMatch.VideoRefs.Single().ChannelIds);
  }

  [Fact]
  public async Task Single_video_import_reports_existing_video_without_side_effects()
  {
    var videoId = $"already-imported-{Guid.NewGuid():N}";
    var existingMatch = await _factory.MatchRepository!.AddItemAsync(
        YouTubeContent.CreateSingleVideo(videoId, []) with
        {
          CreatorUserId = "test-user-id",
          OwnerChannelId = PrimaryScenario.ChannelId,
          VideoRefs = [new VideoRef(videoId, [], channelIds: [PrimaryScenario.ChannelId])]
        });
    using var client = CreateClient(
        userId: "test-user-id",
        permissions: AuthorizationPermissionKeys.VideosImport);

    var response = await client.PostAsJsonAsync(
        "/api/Videos/ImportVideo",
      new { videoId, categories = new[] { "300000000000000000000001" } });
    var matches = await _factory.MatchRepository.GetItemsAsync(match => match.ContentId == videoId);

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    Assert.Single(matches);
    Assert.Equal(existingMatch.Id, matches.Single().Id);
  }

  [Fact]
  public async Task Video_update_rejects_unknown_channel_assignments()
  {
    var videoId = $"invalid-channel-{Guid.NewGuid():N}";
    var match = await _factory.MatchRepository!.AddItemAsync(
        YouTubeContent.CreateSingleVideo(videoId, []) with
        {
          CreatorUserId = "test-user-id",
          OwnerChannelId = PrimaryScenario.ChannelId,
          VideoRefs = [new VideoRef(videoId, [], channelIds: [PrimaryScenario.ChannelId])]
        });
    using var client = CreateClient(
        userId: "test-user-id",
        permissions: AuthorizationPermissionKeys.VideosUpdate);

    var response = await client.PutAsJsonAsync($"/api/Videos/{match.Id}", new
    {
      title = "Updated",
      description = string.Empty,
      url = string.Empty,
      thumbnailVideoId = videoId,
      categories = Array.Empty<string>(),
      videoRefs = new[]
      {
        new { youtubeId = videoId, categories = Array.Empty<object>(), channelIds = new[] { "unknown-channel" } }
      }
    });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Bulk_video_import_endpoints_require_import_permission()
  {
    using var deniedClient = CreateClient();
    using var importClient = CreateClient(permissions: AuthorizationPermissionKeys.VideosImport);

    var deniedCandidates = await deniedClient.GetAsync($"/api/Videos/import-candidates?channelId={PrimaryScenario.ChannelId}&startDate=2026-01-01");
    var allowedTargets = await importClient.GetAsync("/api/Videos/import-targets");

    Assert.Equal(HttpStatusCode.Forbidden, deniedCandidates.StatusCode);
    Assert.NotEqual(HttpStatusCode.Forbidden, allowedTargets.StatusCode);
    Assert.NotEqual(HttpStatusCode.Unauthorized, allowedTargets.StatusCode);
  }

  [Fact]
  public async Task Candidate_discovery_cannot_cross_the_selected_channel_scope()
  {
    using var client = CreateClient(permissions: AuthorizationPermissionKeys.VideosImport);

    var response = await client.GetAsync("/api/Videos/import-candidates?channelId=another-channel&startDate=2026-01-01");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task Videos_manage_passes_specialized_operation_authorization()
  {
    using var client = CreateClient(permissions: AuthorizationPermissionKeys.VideosManage);

    var importResponse = await client.PostAsJsonAsync(
        "/api/Videos/ImportVideo",
        new { videoId = string.Empty, categories = Array.Empty<string>() });
    var translateResponse = await client.PostAsJsonAsync(
        "/api/Videos/Translate",
        new[] { "missing-video" });
    var publishResponse = await client.PostAsJsonAsync(
        "/api/Videos/missing-video/publish-social",
        new { });

    foreach (var response in new[] { importResponse, translateResponse, publishResponse })
    {
      Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
      Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
  }

  private HttpClient CreateClient(
      string? userId = null,
      string? permissions = null,
      bool anonymous = false)
  {
    var client = _factory.CreateClient();
    if (anonymous)
    {
      client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
    }

    if (!string.IsNullOrWhiteSpace(userId))
    {
      client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
    }

    if (!string.IsNullOrWhiteSpace(permissions))
    {
      client.DefaultRequestHeaders.Add("X-Test-Permissions", permissions);
    }

    client.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);

    return client;
  }
}