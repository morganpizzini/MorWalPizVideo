using MorWalPizVideo.YouTubeUtilities;
using Xunit;

namespace MorWalPizVideo.YouTubeUtilities.Tests;

public sealed class YouTubeReliabilityTests
{
    [Fact]
    public void Caller_cancellation_is_classified_as_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Equal(YouTubeErrorKind.Cancellation,
            YouTubeErrorClassifier.Classify(new OperationCanceledException(), cancellation.Token));
    }

    [Fact]
    public void Operation_cancellation_without_caller_cancellation_is_timeout()
    {
        Assert.Equal(YouTubeErrorKind.Timeout,
            YouTubeErrorClassifier.Classify(new OperationCanceledException()));
    }

    [Fact]
    public async Task Executor_uses_bounded_exponential_delays()
    {
        var attempts = 0;
        var executor = new YouTubeOperationExecutor(new YouTubeRetryOptions
        {
            MaxAttempts = 3,
            InitialDelay = TimeSpan.FromMilliseconds(20),
            MaxDelay = TimeSpan.FromMilliseconds(25),
            Timeout = TimeSpan.FromSeconds(1)
        });

        var result = await executor.ExecuteAsync("retry-test", _ =>
        {
            attempts++;
            if (attempts < 3)
                throw new IOException("transient");
            return Task.FromResult("ok");
        });

        Assert.Equal("ok", result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Durable_intent_store_claims_once_and_preserves_unknown_state()
    {
        var directory = Path.Combine(Path.GetTempPath(), "youtube-intents-" + Guid.NewGuid());
        try
        {
            var store = new DurableYouTubeIntentStore(directory);
            Assert.True(store.TryBegin("tenant:file", out var started));
            Assert.Equal(YouTubeIntentState.InProgress, started.State);
            Assert.False(store.TryBegin("tenant:file", out var duplicate));
            Assert.Equal(YouTubeIntentState.InProgress, duplicate.State);

            store.MarkUnknown("tenant:file");
            Assert.False(store.TryBegin("tenant:file", out var unknown));
            Assert.Equal(YouTubeIntentState.Unknown, unknown.State);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task Durable_intent_store_allows_only_one_concurrent_claim()
    {
        var directory = Path.Combine(Path.GetTempPath(), "youtube-intents-" + Guid.NewGuid());
        try
        {
            var store = new DurableYouTubeIntentStore(directory);
            var claims = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
                Task.Run(() => store.TryBegin("tenant:concurrent-file", out YouTubeIntent _))));

            Assert.Single(claims, claim => claim);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Durable_intent_store_treats_corrupt_state_as_unknown()
    {
        var directory = Path.Combine(Path.GetTempPath(), "youtube-intents-" + Guid.NewGuid());
        try
        {
            var store = new DurableYouTubeIntentStore(directory);
            Assert.True(store.TryBegin("tenant:corrupt-file", out _));

            var stateFile = Directory.GetFiles(directory, "*.json").Single();
            File.WriteAllText(stateFile, "{");

            Assert.False(store.TryBegin("tenant:corrupt-file", out var intent));
            Assert.Equal(YouTubeIntentState.Unknown, intent.State);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
