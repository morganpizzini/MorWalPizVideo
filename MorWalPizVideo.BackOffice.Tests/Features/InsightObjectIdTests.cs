using MongoDB.Bson;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using System.Net.Http.Json;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class InsightObjectIdTests
{
  [Fact]
  public async Task Topic_creation_returns_an_object_id_string()
  {
    await using var factory = new BackOfficeWebApplicationFactory();
    using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.InsightsCreate);

    var response = await client.PostAsJsonAsync(
        "/api/Insights/topics",
        new
        {
          title = "ObjectId topic",
          description = "Topic creation regression",
          seedArguments = new[] { "IPSC" },
          preferredSources = Array.Empty<string>()
        });

    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

    Assert.NotNull(body);
    Assert.True(body.TryGetValue("id", out var topicId));
    AssertObjectId(topicId);
  }

  [Fact]
  public async Task Mock_insight_creation_paths_generate_object_ids()
  {
    var service = new MockInsightAgentService();
    var topic = new InsightTopic(
        title: "Dynamic Sport Shooting",
        description: "IPSC news and content",
        seedArguments: new[] { "IPSC" },
        preferredSources: Array.Empty<string>());

    var discoveredNews = await service.DiscoverNewsAsync(topic);
    var contentPlan = await service.GenerateContentPlanAsync(
        topic.Id,
        discoveredNews.Select(item => item.Id).ToList(),
        ContentPlanType.Article,
        new[] { "YouTube" });
    var commentNews = await service.AnalyzeVideoCommentsAsync(
        topic,
        "video-1",
        "Match recap",
        "https://www.youtube.com/watch?v=video-1",
        "Test Channel",
        new[]
        {
          new VideoCommentDto
          {
            Author = "viewer",
            Text = "Cover the new rules",
            PublishedAt = DateTime.UtcNow
          }
        });

    Assert.NotEmpty(discoveredNews);
    Assert.NotEmpty(commentNews);
    AssertObjectIds(discoveredNews.Select(item => item.Id));
    AssertObjectIds(commentNews.Select(item => item.Id));
    AssertObjectId(contentPlan.Id);
  }

  [Fact]
  public void Insight_entities_serialize_with_object_id_ids()
  {
    var topic = new InsightTopic(
        title: "Topic",
        description: "Description",
        seedArguments: Array.Empty<string>(),
        preferredSources: Array.Empty<string>())
    {
      Id = ObjectId.GenerateNewId().ToString()
    };
    var newsItem = new InsightNewsItem(
        topicId: topic.Id,
        title: "News",
        summary: "Summary",
        sourceUrl: "https://example.com/news",
        sourceName: "Example")
    {
      Id = ObjectId.GenerateNewId().ToString()
    };
    var contentPlan = new InsightContentPlan(
        topicId: topic.Id,
        title: "Plan",
        type: ContentPlanType.Article,
        outline: "Outline",
        generatedFromNewsItemIds: new[] { newsItem.Id },
        targetPlatforms: new[] { "YouTube" },
        generatedAt: DateTime.UtcNow)
    {
      Id = ObjectId.GenerateNewId().ToString()
    };
    var cursor = new InsightSourceCursor(topic.Id, "https://example.com/source")
    {
      Id = ObjectId.GenerateNewId().ToString()
    };

    AssertSerialized(topic);
    AssertSerialized(newsItem);
    AssertSerialized(contentPlan);
    AssertSerialized(cursor);
  }

  private static void AssertObjectIds(IEnumerable<string> ids)
  {
    Assert.All(ids, AssertObjectId);
  }

  private static void AssertObjectId(string id)
  {
    Assert.True(ObjectId.TryParse(id, out _), $"'{id}' is not a valid ObjectId.");
  }

  private static void AssertSerialized<T>(T entity)
  {
    var document = entity.ToBsonDocument();
    Assert.Equal(BsonType.ObjectId, document["_id"].BsonType);
  }
}