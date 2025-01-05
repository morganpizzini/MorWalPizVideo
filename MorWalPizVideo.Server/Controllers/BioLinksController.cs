using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    public class BioLinksController : ApplicationController
    {
        public BioLinksController(
            DataService _dataService, IExternalDataService _extDataService, MyMemoryCache _memoryCache) : base(_dataService, _extDataService, _memoryCache)
        {
        }

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.BioLinks])]
        public async Task<IActionResult> Index()
        {
            if (memoryCache.Cache.TryGetValue(CacheKeys.BioLinks, out IList<BioLink>? entities))
            {
                return Ok(entities);
            }
            entities = await dataService.GetBioLinks();
            memoryCache.Cache.Set(CacheKeys.BioLinks, entities, new MemoryCacheEntryOptions
            {
                Size = 1
            });
            return Ok(entities);
        }
    }
}
