using System.Reflection;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.ServerAPI.Controllers;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public class OutputCachePurgeTests
{
    private sealed class CapturingOutputCacheStore : IOutputCacheStore
    {
        public List<string> EvictedTags { get; } = new();

        public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
        {
            EvictedTags.Add(tag);
            return ValueTask.CompletedTask;
        }

        public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
            => ValueTask.FromResult<byte[]?>(null);

        public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Purge_normalizes_tag_to_lowercase_invariant()
    {
        var store = new CapturingOutputCacheStore();
        var controller = new CacheController(_dataService: null!, _memoryCache: null!, _cache: store);

        var result = await controller.Index("Tag-CalendarEvents");

        Assert.Single(store.EvictedTags);
        Assert.Equal("tag-calendarevents", store.EvictedTags[0]);
    }

    [Fact]
    public void OutputCache_tag_attributes_are_lowercase()
    {
        var assembly = typeof(CacheController).Assembly;

        var offenders = new List<string>();

        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                foreach (var attr in method.GetCustomAttributes<OutputCacheAttribute>(inherit: false))
                {
                    if (attr.Tags == null) continue;
                    foreach (var tag in attr.Tags)
                    {
                        if (tag != tag.ToLowerInvariant())
                            offenders.Add($"{type.FullName}.{method.Name} → '{tag}'");
                    }
                }
            }

            foreach (var attr in type.GetCustomAttributes<OutputCacheAttribute>(inherit: false))
            {
                if (attr.Tags == null) continue;
                foreach (var tag in attr.Tags)
                {
                    if (tag != tag.ToLowerInvariant())
                        offenders.Add($"{type.FullName} → '{tag}'");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "Non-lowercase [OutputCache] tags detected: " + string.Join("; ", offenders));
    }
}
