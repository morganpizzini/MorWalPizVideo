using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Tests.Services;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class MorWalPizIndexedCacheStoreTests
{
    [Fact]
    public async Task Multi_key_reads_overlap_with_at_most_eight_in_flight()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstBatchStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = 0;
        var maximum = 0;
        var cache = new ControlledCache(async key =>
        {
            var current = Interlocked.Increment(ref active);
            int previous;
            do
            {
                previous = Volatile.Read(ref maximum);
            }
            while (current > previous && Interlocked.CompareExchange(ref maximum, current, previous) != previous);
            if (current == 8)
                firstBatchStarted.TrySetResult();
            try
            {
                await gate.Task;
                return key;
            }
            finally
            {
                Interlocked.Decrement(ref active);
            }
        });
        var store = new MorWalPizIndexedCacheStore(cache, true);
        var keys = Enumerable.Range(0, 25).Select(index => $"key-{index}").ToArray();

        var read = store.GetManyAsync<string>(keys);
        try
        {
            await firstBatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(read.IsCompleted);
            Assert.Equal(8, cache.ReadKeys.Count);
        }
        finally
        {
            gate.TrySetResult();
        }
        var values = await read.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(8, maximum);
        Assert.Equal(0, active);
        Assert.Equal(keys, values.Keys);
        Assert.Equal(keys, values.Values);
        Assert.Equal(keys.Length, cache.ReadKeys.Count);
    }

    [Fact]
    public async Task Completion_order_does_not_change_key_association_or_insertion_order()
    {
        var keys = Enumerable.Range(0, 8).Select(index => $"key-{index}").ToArray();
        var gates = keys.ToDictionary(key => key,
            _ => new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously));
        var cache = new ControlledCache(key => gates[key].Task);
        var store = new MorWalPizIndexedCacheStore(cache, true);

        var read = store.GetManyAsync<string>(keys);
        Assert.Equal(keys, cache.ReadKeys);
        foreach (var key in keys.Reverse())
            gates[key].SetResult(key == "key-3" ? null : $"value-{key}");
        var values = await read.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(keys, values.Keys);
        foreach (var key in keys)
            Assert.Equal(key == "key-3" ? null : $"value-{key}", values[key]);
    }

    [Fact]
    public async Task Duplicate_keys_keep_last_value_and_first_position_using_ordinal_comparison()
    {
        var counts = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
        var cache = new ControlledCache(key =>
            Task.FromResult<string?>($"{key}-{counts.AddOrUpdate(key, 1, (_, count) => count + 1)}"));
        var store = new MorWalPizIndexedCacheStore(cache, true);
        string[] keys = ["same", "Same", "first", "second", "third", "fourth", "fifth", "sixth", "same"];

        var values = await store.GetManyAsync<string>(keys);

        Assert.Equal(keys.Take(8), values.Keys);
        Assert.Equal("same-2", values["same"]);
        Assert.Equal("Same-1", values["Same"]);
        Assert.Equal(keys, cache.ReadKeys);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_batch_waits_for_started_reads_and_propagates_error(bool synchronousFailure)
    {
        var failure = new InvalidOperationException("Cache unavailable");
        var gate = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cache = new ControlledCache(key => key == "failed"
            ? synchronousFailure ? throw failure : Task.FromException<string?>(failure)
            : gate.Task);
        var store = new MorWalPizIndexedCacheStore(cache, true);
        string[] keys = ["failed", "pending", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "not-started"];

        var read = store.GetManyAsync<string>(keys);
        Assert.Equal(keys.Take(8), cache.ReadKeys);
        Assert.False(read.IsCompleted);
        gate.SetResult("finished");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await read.WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.Same(failure, exception);
        Assert.DoesNotContain("not-started", cache.ReadKeys);
    }

    [Fact]
    public async Task Empty_multi_key_read_does_not_access_the_provider()
    {
        var store = new MorWalPizIndexedCacheStore(null!, true);

        Assert.Empty(await store.GetManyAsync<string>([]));
    }

    private sealed class ControlledCache(Func<string, Task<string?>> read) : IMorWalPizCache
    {
        public ConcurrentQueue<string> ReadKeys { get; } = new();

        public Task<T?> GetAsync<T>(string key)
        {
            ReadKeys.Enqueue(key);
            return ConvertAsync<T>(read(key));
        }

        private static async Task<T?> ConvertAsync<T>(Task<string?> value)
            => await value is T typed ? typed : default;

        public T? Get<T>(string key) => throw new NotSupportedException();
        public void Set<T>(string key, T value, DistributedCacheEntryOptions options) => throw new NotSupportedException();
        public Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions options) => throw new NotSupportedException();
        public void Refresh(string key) => throw new NotSupportedException();
        public Task RefreshAsync(string key) => throw new NotSupportedException();
        public void Remove(string key) => throw new NotSupportedException();
        public Task RemoveAsync(string key) => throw new NotSupportedException();
    }
}
