using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Contracts;
using MorWalPizVideo.Server.Services;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public class ShortContentInsightTests
{
  [Fact]
  public void InsightNewsItem_defaults_to_content_source_kind_for_backward_compatibility()
  {
    var item = new InsightNewsItem(
        topicId: "topic-1",
        title: "Existing content item",
        summary: "summary",
        sourceUrl: "https://example.com/news/1",
        sourceName: "Example News");

    Assert.Equal(InsightSourceKind.Content, item.SourceKind);
    Assert.Equal(string.Empty, item.CommentExcerpt);
    Assert.Equal(string.Empty, item.Sentiment);
  }

  [Fact]
  public void Legacy_comment_insight_uses_post_id_as_video_id_fallback()
  {
    var item = new InsightNewsItem(
        topicId: "topic-1",
        title: "Legacy comment insight",
        summary: "summary",
        sourceUrl: "https://youtube.com/watch?v=video-1",
        sourceName: "YouTube",
        postId: "video-1",
        sourceKind: InsightSourceKind.ShortContent);

    Assert.Equal("video-1", item.EffectiveVideoId);
  }

  [Fact]
  public void Analyze_comments_request_defaults_to_excluding_uploader_comments()
  {
    var request = JsonSerializer.Deserialize<AnalyzeInsightCommentsRequest>("{\"sourceType\":2,\"videoId\":\"video-1\"}");

    Assert.True(request!.ExcludeUploaderComments);
  }

  [Fact]
  public void Comment_filter_excludes_uploader_and_applies_limit_after_filtering()
  {
    var comments = new[]
    {
      new CommentInfo { Author = "uploader", AuthorChannelId = "owner", Text = "self" },
      new CommentInfo { Author = "viewer-1", AuthorChannelId = "viewer-1", Text = "first" },
      new CommentInfo { Author = "unknown", Text = "missing identity" },
      new CommentInfo { Author = "viewer-2", AuthorChannelId = "viewer-2", Text = "second" }
    };

    var retained = InsightCommentFilter.Retain(comments, "owner", 2, excludeUploaderComments: true);

    Assert.Equal(new[] { "first", "missing identity" }, retained.Select(comment => comment.Text));
  }

  [Fact]
  public void Comment_filter_includes_uploader_when_exclusion_is_disabled()
  {
    var comments = new[]
    {
      new CommentInfo { Author = "uploader", AuthorChannelId = "owner", Text = "self" },
      new CommentInfo { Author = "viewer", AuthorChannelId = "viewer", Text = "viewer" }
    };

    var retained = InsightCommentFilter.Retain(comments, "owner", 2, excludeUploaderComments: false);

    Assert.Equal(new[] { "self", "viewer" }, retained.Select(comment => comment.Text));
  }

  [Fact]
  public void YouTube_insight_deduplication_predicate_renders_persisted_current_and_legacy_fields()
  {
    var predicate = InsightsService.BuildYouTubeInsightDeduplicationPredicate("topic", "channel", "video-1", "https://youtube.com/watch?v=video-1");
    var filter = new ExpressionFilterDefinition<InsightNewsItem>(predicate);
    var rendered = filter.Render(new RenderArgs<InsightNewsItem>(
        BsonSerializer.LookupSerializer<InsightNewsItem>(),
        BsonSerializer.SerializerRegistry));

    var renderedText = rendered.ToString();
    Assert.Contains("videoId", renderedText);
    Assert.Contains("postId", renderedText);
    Assert.DoesNotContain("EffectiveVideoId", renderedText);
  }

  [Fact]
  public async Task AnalyzeVideoCommentsAsync_preserves_fields_and_uses_selected_source_kind()
  {
    var service = new MockInsightAgentService();
    var topic = new InsightTopic(
        title: "Dynamic Sport Shooting",
        description: "IPSC/IDPA news and content",
        seedArguments: new[] { "IPSC" },
        preferredSources: Array.Empty<string>())
    {
      Id = "topic-1"
    };

    var comments = new List<VideoCommentDto>
        {
            new() { Author = "viewer1", Text = "You should cover the new holster rules", PublishedAt = DateTime.UtcNow }
        };

    var results = await service.AnalyzeVideoCommentsAsync(
        topic,
        videoId: "vid-1",
        videoTitle: "Match Recap",
        videoUrl: "https://www.youtube.com/watch?v=vid-1",
        channelName: "Test Channel",
        comments: comments,
        sourceKind: InsightSourceKind.Content);

    Assert.NotEmpty(results);
    Assert.All(results, item =>
    {
      Assert.Equal(InsightSourceKind.Content, item.SourceKind);
      Assert.Equal("YouTube", item.PlatformSource);
      Assert.Equal("vid-1", item.PostId);
      Assert.Equal("vid-1", item.VideoId);
      Assert.Equal("neutro", item.Sentiment);
      Assert.Equal("You should cover the new holster rules", item.CommentExcerpt);
      Assert.Equal(item.CommentExcerpt, item.AnalysisReason);
      Assert.InRange(item.AIRelevanceScore, 0.65, 0.85);
      Assert.StartsWith("https://www.youtube.com/watch?v=vid-1", item.SourceUrl);
    });
  }

  [Fact]
  public async Task AnalyzeVideoCommentsAsync_omitted_source_kind_defaults_to_short_content()
  {
    var service = new MockInsightAgentService();
    var topic = new InsightTopic("Topic", "Description", Array.Empty<string>(), Array.Empty<string>()) { Id = "topic-1" };
    var results = await service.AnalyzeVideoCommentsAsync(topic, "vid-1", "Title", "https://youtube.com/watch?v=vid-1", "Channel",
      new[] { new VideoCommentDto { Text = "Idea", PublishedAt = DateTime.UtcNow } });

    Assert.All(results, item => Assert.Equal(InsightSourceKind.ShortContent, item.SourceKind));
  }

  [Theory]
  [InlineData(null, 0.5)]
  [InlineData("0", 0)]
  [InlineData("1.5", 1)]
  [InlineData("-0.2", 0)]
  [InlineData("\"invalid\"", 0.5)]
  public void Relevance_score_is_fallback_or_clamped(string? jsonValue, double expected)
  {
    using var document = jsonValue == null ? null : JsonDocument.Parse(jsonValue);
    var value = document?.RootElement;

    Assert.Equal(expected, InsightAgentService.NormalizeRelevanceScore(value));
  }

  [Fact]
  public void Comments_prompt_is_accepted_by_semantic_kernel_parser()
  {
    var topic = new InsightTopic(
        title: "Dynamic Sport Shooting",
        description: "IPSC/IDPA news and content",
        seedArguments: new[] { "IPSC" },
        preferredSources: Array.Empty<string>());
    var comments = "[1] Autore: viewer\nCommento: \"Use {these braces}\"\nData: 2026-08-09T00:00:00.0000000Z\n\nNew line";

    var prompt = InsightAgentService.BuildCommentsPrompt(topic, "Match recap", "IPSC", comments);

    var function = KernelFunctionFactory.CreateFromPrompt(prompt);

    Assert.NotNull(function);
  }

  [Fact]
  public void Comments_prompt_includes_bounded_transient_video_description_context()
  {
    var topic = new InsightTopic("Topic", "Description", new[] { "IPSC" }, Array.Empty<string>());
    var description = new string('x', 4_100);

    var prompt = InsightAgentService.BuildCommentsPrompt(
        topic, "Match recap", "IPSC", "[1] Commento: idea", description, InsightSourceKind.Content);

    Assert.Contains("Descrizione video (contesto transitorio, non persistente):", prompt);
    Assert.Contains("Source kind: Content", prompt);
    Assert.Contains(new string('x', 4_000), prompt);
    Assert.DoesNotContain(new string('x', 4_001), prompt);
  }

  [Fact]
  public void Empty_video_description_keeps_comments_prompt_valid()
  {
    var topic = new InsightTopic("Topic", "Description", Array.Empty<string>(), Array.Empty<string>());

    var prompt = InsightAgentService.BuildCommentsPrompt(topic, "Title", "", "comments", "");

    var function = KernelFunctionFactory.CreateFromPrompt(prompt);

    Assert.NotNull(function);
  }
}
