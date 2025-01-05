using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    public class PagesController : ApplicationController
    {
        private readonly BlobStorageOptions blobOptions;
        public PagesController(
            DataService _dataService, IExternalDataService _extDataService, MyMemoryCache _memoryCache, IOptions<BlobStorageOptions> _blobOptions) : base(_dataService, _extDataService, _memoryCache)
        {
            blobOptions = _blobOptions.Value;
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
            var page = entities?.FirstOrDefault(x => x.Url == url);
            if(page == null)
                return NotFound();
            return Ok(page with { ThumbnailUrl = $"{blobOptions.Endpoint}/{blobOptions.PageContainerName}/{page.Url}/thumbnail.jpg" });
        }
    }
}
