using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class ProductCategoryCacheTests : IClassFixture<ProductCategoryCacheFactory>
{
    private readonly ProductCategoryCacheFactory factory;

    public ProductCategoryCacheTests(ProductCategoryCacheFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Product_category_read_caches_miss_and_hits_on_second_read()
    {
        factory.Cache.Clear();
        var service = factory.Services.GetRequiredService<ProductCategoryCacheService>();

        await service.GetAllAsync("  Channel-A ");
        await service.GetAllAsync("channel-a");

        Assert.Equal(2, factory.Cache.GetCount);
        Assert.Equal(1, factory.Cache.SetCount);
        Assert.Contains($"{CacheKeys.ProductCategories}:channel-a", factory.Cache.Values.Keys);
    }

    [Fact]
    public async Task Product_category_cache_isolated_by_channel_key()
    {
        factory.Cache.Clear();
        var service = factory.Services.GetRequiredService<ProductCategoryCacheService>();

        await service.GetAllAsync("channel-a");
        await service.GetAllAsync("channel-b");

        Assert.Equal(
            [
                ProductCategoryCacheService.BuildCacheKey("channel-a"),
                ProductCategoryCacheService.BuildCacheKey("channel-b")
            ],
            factory.Cache.Values.Keys.OrderBy(key => key));
    }

    [Fact]
    public async Task Successful_create_update_and_delete_remove_category_cache_and_keep_product_invalidation()
    {
        factory.Cache.Clear();
        factory.CrossApiService.Clear();
        using var client = CreateClient();
        var title = $"Cache category {Guid.NewGuid():N}";

        await client.GetAsync("/api/ProductCategories");
        var createResponse = await client.PostAsJsonAsync("/api/ProductCategories", new
        {
            Title = title,
            Description = "Initial"
        });
        Assert.Equal(HttpStatusCode.NoContent, createResponse.StatusCode);

        var categories = await client.GetFromJsonAsync<ProductCategoryContract[]>("/api/ProductCategories");
        var category = Assert.Single(categories!, item => item.Title == title);

        var updateResponse = await client.PutAsJsonAsync($"/api/ProductCategories/{category.Id}", new
        {
            Title = $"{title} updated",
            Description = "Updated"
        });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/ProductCategories/{category.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        Assert.Equal(3, factory.Cache.RemoveCount);
        Assert.Equal(3, factory.CrossApiService.ResetKeys.Count(key => key == CacheKeys.Products));
        Assert.Equal(3, factory.CrossApiService.PurgedTags.Count(tag => tag == ApiTagCacheKeys.Products));
    }

    [Fact]
    public async Task Failed_create_does_not_remove_category_or_product_cache()
    {
        factory.Cache.Clear();
        factory.CrossApiService.Clear();
        using var client = CreateClient();
        var title = $"Duplicate category {Guid.NewGuid():N}";

        var firstCreate = await client.PostAsJsonAsync("/api/ProductCategories", new { Title = title, Description = "Initial" });
        Assert.Equal(HttpStatusCode.NoContent, firstCreate.StatusCode);
        await client.GetAsync("/api/ProductCategories");
        var removeCountAfterSuccessfulCreate = factory.Cache.RemoveCount;
        var resetCountAfterSuccessfulCreate = factory.CrossApiService.ResetKeys.Count;

        var duplicateCreate = await client.PostAsJsonAsync("/api/ProductCategories", new { Title = title, Description = "Duplicate" });

        Assert.Equal(HttpStatusCode.Conflict, duplicateCreate.StatusCode);
        Assert.Equal(removeCountAfterSuccessfulCreate, factory.Cache.RemoveCount);
        Assert.Equal(resetCountAfterSuccessfulCreate, factory.CrossApiService.ResetKeys.Count);
    }

    [Fact]
    public async Task Cache_failure_falls_back_to_data_service()
    {
        factory.Cache.Clear();
        factory.Cache.ThrowOnGet = true;
        factory.Cache.ThrowOnSet = true;
        using var client = CreateClient();

        var response = await client.GetAsync("/api/ProductCategories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateClient()
    {
        var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.ProductCategoriesManage);
        return client;
    }
}

public sealed class ProductCategoryCacheFactory : BackOfficeWebApplicationFactory
{
    public RecordingCache Cache => Services.GetRequiredService<RecordingCache>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMorWalPizCache>();
            services.AddSingleton<RecordingCache>();
            services.AddSingleton<IMorWalPizCache>(provider => provider.GetRequiredService<RecordingCache>());
        });
    }
}

public sealed class RecordingCache : IMorWalPizCache
{
    public Dictionary<string, object> Values { get; } = new(StringComparer.Ordinal);
    public int GetCount { get; private set; }
    public int SetCount { get; private set; }
    public int RemoveCount { get; private set; }
    public bool ThrowOnGet { get; set; }
    public bool ThrowOnSet { get; set; }

    public T? Get<T>(string key)
    {
        GetCount++;
        if (ThrowOnGet)
            throw new InvalidOperationException("simulated cache read failure");
        return Values.TryGetValue(key, out var value) ? (T)value : default;
    }

    public Task<T?> GetAsync<T>(string key) => Task.FromResult(Get<T>(key));

    public void Set<T>(string key, T value, DistributedCacheEntryOptions options)
    {
        SetCount++;
        if (ThrowOnSet)
            throw new InvalidOperationException("simulated cache write failure");
        Values[key] = value!;
    }

    public Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions options)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key) { }
    public Task RefreshAsync(string key) => Task.CompletedTask;

    public void Remove(string key)
    {
        RemoveCount++;
        Values.Remove(key);
    }

    public Task RemoveAsync(string key)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        Values.Clear();
        GetCount = 0;
        SetCount = 0;
        RemoveCount = 0;
        ThrowOnGet = false;
        ThrowOnSet = false;
    }
}