using MorWalPiz.Contracts.DTOs;
using MorWalPiz.InsightScanner.Models;
using MorWalPiz.InsightScanner.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.ExceptionServices;

namespace MorWalPiz.InsightScanner.Tests;

public sealed class FakeInsightServicesTests
{
    [Fact]
    public void Host_resolves_main_window_with_fake_services_and_stops()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DOTNET_ENVIRONMENT"] = "Test",
                    ["Scanner:UseFake"] = "true",
                    ["Scanner:DefaultMaxPostsPerSource"] = "7"
                }).Build();
                using var host = App.CreateHost(configuration);
                host.StartAsync().GetAwaiter().GetResult();
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                Assert.IsType<FakeBackOfficeInsightClient>(host.Services.GetRequiredService<IBackOfficeInsightClient>());
                Assert.Equal(7, host.Services.GetRequiredService<ScannerAppSettings>().DefaultMaxPostsPerSource);
                var window = host.Services.GetRequiredService<MainWindow>();
                Assert.Same(window, host.Services.GetRequiredService<MainWindow>());
                Assert.False(window.IsVisible);
                window.Close();
                host.StopAsync().GetAwaiter().GetResult();
                Assert.True(lifetime.ApplicationStopping.IsCancellationRequested);
                Assert.True(lifetime.ApplicationStopped.IsCancellationRequested);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Desktop host test did not complete.");
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    [Fact]
    public void Host_rejects_production_fake_mode()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DOTNET_ENVIRONMENT"] = "Production",
            ["Scanner:UseFake"] = "true"
        }).Build();
        Assert.Throws<InvalidOperationException>(() => App.CreateHost(configuration));
    }

    [Fact]
    public async Task Fake_source_returns_reproducible_bounded_posts()
    {
        var settings = new ScannerAppSettings { FakeScenario = "success" };
        var strategy = new FakeSourceScanStrategy(settings);

        var first = await strategy.CollectPostsAsync("https://example.test/source", 3, CancellationToken.None);
        var second = await strategy.CollectPostsAsync("https://example.test/source", 3, CancellationToken.None);

        Assert.Equal(
            first.Select(post => new
            {
                post.PostUrl,
                post.PostId,
                post.PlatformSource,
                post.Author,
                post.Text,
                post.PublishedAt
            }),
            second.Select(post => new
            {
                post.PostUrl,
                post.PostId,
                post.PlatformSource,
                post.Author,
                post.Text,
                post.PublishedAt
            }));
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
                new SourceScanBatchDto
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
