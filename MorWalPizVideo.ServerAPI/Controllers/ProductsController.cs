using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    public class ProductsController : ApplicationController
    {
        public ProductsController(
            IGenericDataService _dataService, IMorWalPizCache _memoryCache) : base(_dataService, _memoryCache)
        {
        }

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.Products])]
        public async Task<IActionResult> Index()
        {
            var entities = await cache.GetOrCreateAsync(CacheKeys.Products, dataService.GetProducts);
            return Ok(entities);
        }
    }
}
