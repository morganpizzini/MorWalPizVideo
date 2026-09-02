using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.MvcHelpers.Authentication;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [InternalServiceAuth] // ADR-002: internal cache operations require an authenticated service identity
    public class CacheController : ApplicationController
    {
        private readonly IOutputCacheStore outputCache;
        private readonly IYouTubeContentIndexedCache? indexedCache;
        public CacheController(IGenericDataService _dataService, IMorWalPizCache _memoryCache,
                                IOutputCacheStore _cache,
                                IYouTubeContentIndexedCache? _indexedCache = null)
                                    : base(_dataService, _memoryCache)
        {
            outputCache = _cache;
            indexedCache = _indexedCache;
        }
        [HttpGet("purge")]
        public async Task<IActionResult> Index([FromQuery(Name = "k")] string tag){
            
            await outputCache.EvictByTagAsync(tag.ToLowerInvariant(), default);

            return NoContent();
        }

        [HttpGet("reset")]
        public IActionResult Reset([FromQuery(Name = "k")] string keys = "")
        {
            if (string.IsNullOrEmpty(keys))
                keys = $"{CacheKeys.Matches},{CacheKeys.Products},{CacheKeys.Sponsors},{CacheKeys.Pages},{CacheKeys.CalendarEvents},{CacheKeys.ShortLinks},{CacheKeys.ChannelNews}";

            foreach (var key in keys.ToLowerInvariant().Split(","))
                base.cache.Remove(key);

            return NoContent();
        }

        [HttpGet("refresh-video")]
        public async Task<IActionResult> RefreshVideo([FromQuery(Name = "id")] string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                return BadRequest("Video id is required.");

            if (indexedCache is not null)
            {
                await indexedCache.NotifyChangedAsync(entityId);
                var refresh = await indexedCache.DrainAsync();
                return Ok(new
                {
                    cacheStatus = refresh.Status,
                    failedRefreshes = refresh.FailedRefreshes,
                    retriedRefreshes = refresh.RetriedRefreshes
                });
            }

            return Ok(new { cacheStatus = "disabled" });
        }

    }
}
