using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    public class PagesController : ApplicationController
    {
        public PagesController(
            DataService _dataService, IExternalDataService _extDataService, MyMemoryCache _memoryCache) : base(_dataService, _extDataService, _memoryCache)
        {
        }

        [HttpGet("{url}")]
        [OutputCache(Tags = [CacheKeys.Pages], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> Detail(string url) {
            if (!memoryCache.Cache.TryGetValue(CacheKeys.Pages, out IList<Page>? entities))
            {
                entities = await dataService.GetPages();

                memoryCache.Cache.Set(CacheKeys.Pages, entities.OrderByDescending(x => x.CreationDateTime), new MemoryCacheEntryOptions
                {
                    Size = 1
                });
            }
            return Ok(entities?.FirstOrDefault(x => x.Url == url));
        }
    }
}
