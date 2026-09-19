using MorWalPiz.Contracts.DTOs;
using MorWalPiz.InsightScanner.Models;
using MorWalPiz.InsightScanner.Services;

namespace MorWalPiz.InsightScanner.Tests;

public sealed class FakeInsightServicesTests
{
    [Fact]
    public async Task Fake_source_returns_reproducible_bounded_posts()
    {
        var settings = new ScannerAppSettings { FakeScenario = "success" };
        var strategy = new FakeSourceScanStrategy(settings);

        var first = await strategy.CollectPostsAsync("https://example.test/source", 3, CancellationToken.None);
        var second = await strategy.CollectPostsAsync("https://example.test/source", 3, CancellationToken.None);

        Assert.Equal(first, second);
        Assert.Equal(["fake-1", "fake-2", "fake-3"], first.Select(post => post.PostId));
        Assert.Equal(new DateTime(2023, 12, 31, 23, 59, 0, DateTimeKind.Utc), first[0].PublishedAt);
    }

    [Fact]
    public async Task Fake_client_returns_deterministic_topics_and_scan_counts()
    {
        var settings = new ScannerAppSettings { FakeScenario = "success" };
        var client = new FakeBackOfficeInsightClient(settings);
        var posts = new List<RawSocialPostDto>
        {
            new() { PostId = "post-1", PostUrl = "https://example.test/post-1" }
        };

        var topics = await client.GetTopicsAsync();
        var result = await client.SubmitManualScanAsync("fake-topic", new ManualScanRequest
        {
            Sources =
            [
                new SourceScanRequestDto
                {
                    SourceUrl = "https://example.test/source",
                    Posts = posts
                }
            ]
        });

        Assert.Single(topics);
        Assert.Equal("fake-topic", topics[0].Id);
        Assert.Single(result.SourceSummaries);
        Assert.Equal(1, result.SourceSummaries[0].ProcessedCount);
        Assert.Equal(1, result.SourceSummaries[0].CreatedCount);
    }
}
