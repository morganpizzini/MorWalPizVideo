using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Server.Models;
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
  public async Task AnalyzeVideoCommentsAsync_tags_derived_ideas_as_short_content()
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
        comments: comments);

    Assert.NotEmpty(results);
    Assert.All(results, item =>
    {
      Assert.Equal(InsightSourceKind.ShortContent, item.SourceKind);
      Assert.Equal("YouTube", item.PlatformSource);
      Assert.Equal("vid-1", item.PostId);
      Assert.StartsWith("https://www.youtube.com/watch?v=vid-1", item.SourceUrl);
    });
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
}
