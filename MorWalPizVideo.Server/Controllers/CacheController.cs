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
        public async Task<IActionResult> Index([FromQuery(Name = "k")] string tag){
            
            await cache.EvictByTagAsync(tag, default);

            return NoContent();
        }

        [HttpGet("reset")]
        public IActionResult Reset([FromQuery(Name = "k")] string keys = "")
        {
            if (string.IsNullOrEmpty(keys))
                keys = $"{CacheKeys.Matches},{CacheKeys.Products},{CacheKeys.Sponsors},{CacheKeys.Pages},{CacheKeys.CalendarEvents},{CacheKeys.BioLinks},{CacheKeys.ShortLinks}";

            foreach (var key in keys.ToLower().Split(","))
                memoryCache.Cache.Remove(key);

            return NoContent();
        }

    }
}
