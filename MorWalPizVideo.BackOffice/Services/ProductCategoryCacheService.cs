using Microsoft.Extensions.Caching.Distributed;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Services;

public sealed class ProductCategoryCacheService
{
    private readonly DataService dataService;
    private readonly IMorWalPizCache cache;
    private readonly ILogger<ProductCategoryCacheService> logger;

    public ProductCategoryCacheService(
        DataService dataService,
        IMorWalPizCache cache,
        ILogger<ProductCategoryCacheService> logger)
    {
        this.dataService = dataService;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<ProductCategoryContract[]> GetAllAsync(string channelId)
    {
        var cacheKey = BuildCacheKey(channelId);
        try
        {
            var cachedCategories = await cache.GetAsync<ProductCategoryContract[]>(cacheKey);
            if (cachedCategories is not null)
                return cachedCategories;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Product category cache read failed. CacheKey={CacheKey}", cacheKey);
        }

        var categories = (await dataService.FetchProductCategories(null, channelId))
            .Select(ContractUtils.Convert)
            .ToArray();

        try
        {
            await cache.SetAsync(
                cacheKey,
                categories,
                new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1)));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Product category cache write failed. CacheKey={CacheKey}", cacheKey);
        }

        return categories;
    }

    public async Task RemoveAsync(string channelId)
    {
        var cacheKey = BuildCacheKey(channelId);
        try
        {
            await cache.RemoveAsync(cacheKey);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Product category cache removal failed. CacheKey={CacheKey}", cacheKey);
        }
    }

    internal static string BuildCacheKey(string channelId)
        => $"{CacheKeys.ProductCategories}:{channelId.Trim().ToLowerInvariant()}";
}