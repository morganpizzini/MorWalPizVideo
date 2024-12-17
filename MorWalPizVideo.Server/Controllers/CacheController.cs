using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    public class CacheController : ApplicationController
    {
        private readonly IOutputCacheStore cache;
        public CacheController(DataService _dataService, IExternalDataService _extDataService, MyMemoryCache _memoryCache,
                                IOutputCacheStore _cache)
                                    : base(_dataService, _extDataService, _memoryCache)
        {
            cache = _cache;
        }
        [HttpGet("purge")]
        public ValueTask Index([FromQuery(Name = "k")] string tag) => cache.EvictByTagAsync(tag, default);

        [HttpGet("reset")]
        public IActionResult Reset([FromQuery(Name = "k")] string keys)
        {
            if (string.IsNullOrEmpty(keys))
                keys = $"{CacheKeys.Match},{CacheKeys.Product},{CacheKeys.Sponsor},{CacheKeys.Pages},{CacheKeys.CalendarEvents}";

            foreach (var key in keys.ToLower().Split(","))
                memoryCache.Cache.Remove(key);

            return NoContent();
        }

    }
}
