using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    public class ProductsController : ApplicationController
    {
        public ProductsController(
            DataService _dataService, IExternalDataService _extDataService, MyMemoryCache _memoryCache) : base(_dataService, _extDataService, _memoryCache)
        {
        }

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.Product])]
        public async Task<IActionResult> Index()
        {
            if (memoryCache.Cache.TryGetValue(CacheKeys.Product, out IList<Product>? entities))
                return Ok(entities);
            entities = await dataService.GetProducts();
            memoryCache.Cache.Set(CacheKeys.Product, entities, new MemoryCacheEntryOptions
            {
                Size = 1
            });
            return Ok(entities);
        }
    }
}
