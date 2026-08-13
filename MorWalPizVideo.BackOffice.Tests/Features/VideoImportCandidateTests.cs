using System.Net;
using System.Text.Json;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class VideoImportCandidateTests : IClassFixture<PaginatedCandidateWebApplicationFactory>
{
    private readonly PaginatedCandidateWebApplicationFactory _factory;

    public VideoImportCandidateTests(PaginatedCandidateWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Candidate_discovery_includes_boundary_and_paginated_videos_for_selected_channel()
    {
        using var client = _factory.CreateClientWithPermissions("videos.import");

        var response = await client.GetAsync(
            $"/api/Videos/import-candidates?channelId={PrimaryScenario.ChannelId}&startDate=2026-01-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var candidates = document.RootElement.EnumerateArray().ToArray();

        Assert.Equal(
            new[] { "boundary-video", "older-page-video" },
            candidates.Select(candidate => candidate.GetProperty("videoId").GetString()).ToArray());
        Assert.NotNull(_factory.YTService);
        Assert.Equal(PrimaryScenario.ChannelId, _factory.YTService.RequestedChannelId);
        Assert.Equal(DateTimeKind.Utc, _factory.YTService!.RequestedStartDateUtc.Kind);
        Assert.Equal(new DateTime(2026, 1, 1), _factory.YTService.RequestedStartDateUtc);
    }
}

public sealed class PaginatedCandidateWebApplicationFactory : BackOfficeWebApplicationFactory
{
    public PaginatedCandidateYTServiceMock? YTService => Services.GetService<IYTService>() as PaginatedCandidateYTServiceMock;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IYTService>();
            services.AddSingleton<IYTService, PaginatedCandidateYTServiceMock>();
        });
    }
}

public sealed class PaginatedCandidateYTServiceMock : YTServiceMock
{
    public string? RequestedChannelId { get; private set; }
    public DateTime RequestedStartDateUtc { get; private set; }

    public override Task<IList<SearchResult>> FetchVideosSince(
        string channelId,
        DateTime startDateUtc,
        bool showVideo = true)
    {
        RequestedChannelId = channelId;
        RequestedStartDateUtc = startDateUtc;

        var pages = new[]
        {
            new[]
            {
                CreateVideo("boundary-video", channelId, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                CreateVideo("other-channel-video", "another-channel", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc))
            },
            new[]
            {
                CreateVideo("older-page-video", channelId, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)),
                CreateVideo("before-start-video", channelId, new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc))
            }
        };

        var candidates = pages
            .SelectMany(page => page)
            .Where(video => string.Equals(video.Snippet?.ChannelId, channelId, StringComparison.Ordinal))
            .Where(video => video.Snippet?.PublishedAtDateTimeOffset?.UtcDateTime.Date >= startDateUtc.Date)
            .ToList();

        return Task.FromResult<IList<SearchResult>>(candidates);
    }

    private static SearchResult CreateVideo(string videoId, string channelId, DateTime publishedAt)
        => new()
        {
            Id = new ResourceId { Kind = "youtube#video", VideoId = videoId },
            Snippet = new SearchResultSnippet
            {
                ChannelId = channelId,
                Title = videoId,
                PublishedAtDateTimeOffset = publishedAt
            }
        };
}