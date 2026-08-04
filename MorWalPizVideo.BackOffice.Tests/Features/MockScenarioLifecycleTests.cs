using Microsoft.Extensions.Configuration;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public class MockScenarioLifecycleTests
{
    [Fact]
    public void Startup_configuration_selects_named_scenario_and_fixture_can_override_it()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MockScenario"] = MockScenarioNames.Empty
            })
            .Build();
        var lifecycle = new MockScenarioLifecycle(configuration);

        Assert.Equal(MockScenarioNames.Empty, lifecycle.Name);
        Assert.Empty(lifecycle.Read<YouTubeContent>("matches"));

        lifecycle.Select(MockScenarioNames.Authorization);

        Assert.Equal(MockScenarioNames.Authorization, lifecycle.Name);
        Assert.Contains(lifecycle.Read<User>("users"), user => user.Username == "inactive-user");
    }

    [Fact]
    public void Reset_restores_selected_baseline_and_reinitialize_replaces_the_scenario_instance()
    {
        var lifecycle = new MockScenarioLifecycle(new ConfigurationBuilder().Build());
        lifecycle.Select(MockScenarioNames.Empty);
        lifecycle.Add("pages", new Page("", "Fixture", "", "/fixture", "") { Id = "fixture-page" });

        lifecycle.Reset();
        Assert.DoesNotContain(lifecycle.Read<Page>("pages"), page => page.Id == "fixture-page");

        lifecycle.Reinitialize();
        Assert.DoesNotContain(lifecycle.Read<Page>("pages"), page => page.Id == "fixture-page");
        Assert.Equal(MockScenarioNames.Empty, lifecycle.Name);
    }

    [Fact]
    public void Separate_lifecycles_are_isolated()
    {
        var configuration = new ConfigurationBuilder().Build();
        var first = new MockScenarioLifecycle(configuration);
        var second = new MockScenarioLifecycle(configuration);

        first.Add("pages", new Page("", "First", "", "/first", "") { Id = "first-only" });

        Assert.DoesNotContain(second.Read<Page>("pages"), page => page.Id == "first-only");
    }

    [Fact]
    public async Task Blob_mock_supports_storage_metadata_and_configured_outcomes()
    {
        var blob = new BlobServiceMock();
        await blob.UploadImageAsync("images/test.png", new MemoryStream([1, 2, 3]), "uploads");

        var result = await blob.DownloadWithMetadataAsync("images/test.png");
        Assert.True(result.IsSuccess);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal("010203", Convert.ToHexString(await ReadBytes(result.Content!)));
        Assert.NotNull(result.Metadata!["sha256"]);

        blob.Outcome = BlobMockOutcome.MalformedResponse;
        Assert.Equal(BlobDownloadStatus.ChecksumMismatch,
            (await blob.DownloadWithMetadataAsync("images/test.png")).Status);

        blob.Outcome = BlobMockOutcome.TransientFailure;
        await Assert.ThrowsAsync<Azure.RequestFailedException>(() =>
            blob.DownloadWithMetadataAsync("images/test.png"));
    }

    private static async Task<byte[]> ReadBytes(Stream stream)
    {
        await using (stream)
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            return buffer.ToArray();
        }
    }
}
