using System.Net;
using System.Net.Http.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;

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