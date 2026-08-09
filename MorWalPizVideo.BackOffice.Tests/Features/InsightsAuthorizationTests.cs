using System.Net;
using System.Net.Http.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class InsightsAuthorizationTests : IClassFixture<BackOfficeWebApplicationFactory>
{
  private readonly BackOfficeWebApplicationFactory _factory;

  public InsightsAuthorizationTests(BackOfficeWebApplicationFactory factory)
  {
    _factory = factory;
  }

  [Theory]
  [InlineData(AuthorizationPermissionKeys.InsightsView, HttpStatusCode.OK)]
  [InlineData(AuthorizationPermissionKeys.InsightsManage, HttpStatusCode.OK)]
  [InlineData(AuthorizationPermissionKeys.BackofficeManageAll, HttpStatusCode.OK)]
  [InlineData(AuthorizationPermissionKeys.BackofficeAccess, HttpStatusCode.Forbidden)]
  public async Task Admin_topic_list_enforces_explicit_insights_permission(string permission, HttpStatusCode expectedStatus)
  {
    using var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Test-Permissions", permission);
    client.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);

    var response = await client.GetAsync("/api/Insights/topics/admin");

    Assert.Equal(expectedStatus, response.StatusCode);
  }

  [Fact]
  public async Task Existing_topic_list_remains_api_key_only()
  {
    using var cookieClient = _factory.CreateClient();
    cookieClient.DefaultRequestHeaders.Add("X-Test-Permissions", AuthorizationPermissionKeys.InsightsView);
    cookieClient.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);

    var response = await cookieClient.GetAsync("/api/Insights/topics");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Insights_manage_passes_scan_authorization()
  {
    using var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Test-Permissions", AuthorizationPermissionKeys.InsightsManage);
    client.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);

    var response = await client.PostAsync("/api/Insights/topics/missing-topic/scan-news", null);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Theory]
  [InlineData(AuthorizationPermissionKeys.InsightsScan, HttpStatusCode.NotFound)]
  [InlineData(AuthorizationPermissionKeys.InsightsView, HttpStatusCode.Forbidden)]
  public async Task Comment_analysis_requires_scan_permission(string permission, HttpStatusCode expectedStatus)
  {
    using var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Test-Permissions", permission);
    client.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);

    var response = await client.PostAsJsonAsync(
        "/api/Insights/topics/missing-topic/analyze-comments",
        new { sourceType = 2, videoId = "adhoc-video", commentsNumber = 20 });

    Assert.Equal(expectedStatus, response.StatusCode);
  }
}