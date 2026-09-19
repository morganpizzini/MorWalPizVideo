using MorWalPiz.Contracts.DTOs;
using MorWalPiz.InsightScanner.Models;
using System.Net;
using System.Net.Http;

namespace MorWalPiz.InsightScanner.Services;

public sealed class FakeBackOfficeInsightClient(ScannerAppSettings settings) : IBackOfficeInsightClient
{
    public Task<List<InsightTopicSummary>> GetTopicsAsync()
    {
        ThrowForFailure();
        return Task.FromResult(settings.FakeScenario.Equals("empty", StringComparison.OrdinalIgnoreCase)
            ? new List<InsightTopicSummary>()
            : new List<InsightTopicSummary> { new() { Id = "fake-topic", Title = "Fake topic" } });
    }

    public Task<ManualScanResponseDto> SubmitManualScanAsync(string topicId, ManualScanRequest request)
    {
        ThrowForFailure();
        return Task.FromResult(new ManualScanResponseDto
        {
            SourceSummaries = request.Sources.Select(source => new SourceScanSummaryDto
            {
                SourceUrl = source.SourceUrl,
                ProcessedCount = source.Posts.Count,
                CreatedCount = settings.FakeScenario.Equals("empty", StringComparison.OrdinalIgnoreCase) ? 0 : source.Posts.Count
            }).ToList()
        });
    }

    private void ThrowForFailure()
    {
        if (settings.FakeScenario.Equals("auth", StringComparison.OrdinalIgnoreCase))
            throw new HttpRequestException("Fake unauthorized", null, System.Net.HttpStatusCode.Unauthorized);
        if (settings.FakeScenario.Equals("transient", StringComparison.OrdinalIgnoreCase))
            throw new HttpRequestException("Fake transient failure", null, System.Net.HttpStatusCode.ServiceUnavailable);
        if (settings.FakeScenario.Equals("permanent", StringComparison.OrdinalIgnoreCase))
            throw new HttpRequestException("Fake permanent failure", null, System.Net.HttpStatusCode.BadRequest);
    }
}

public sealed class FakeSourceScanStrategy(ScannerAppSettings settings) : ISourceScanStrategy
{
    public bool CanHandle(string sourceUrl) => Uri.TryCreate(sourceUrl, UriKind.Absolute, out _);

    public Task<List<RawSocialPostDto>> CollectPostsAsync(string sourceUrl, int maxPosts, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (settings.FakeScenario.Equals("empty", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(new List<RawSocialPostDto>());
        if (settings.FakeScenario.Equals("transient", StringComparison.OrdinalIgnoreCase))
            throw new HttpRequestException("Fake transient scan failure");
        if (settings.FakeScenario.Equals("permanent", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Fake permanent scan failure");

        return Task.FromResult(Enumerable.Range(1, Math.Clamp(maxPosts, 0, 20))
            .Select(index => new RawSocialPostDto
            {
                PostId = $"fake-{index}",
                PostUrl = $"{sourceUrl.TrimEnd('/')}/fake-{index}",
                PlatformSource = "Fake",
                Author = "fake-author",
                Text = $"Fake post {index}",
                PublishedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(-index)
            }).ToList());
    }
}