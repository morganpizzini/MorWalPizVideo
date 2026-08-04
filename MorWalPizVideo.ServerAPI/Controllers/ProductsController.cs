using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using Microsoft.Extensions.Caching.Memory;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [AllowAnonymous] // ADR-002: explicit public read access
    public class ProductsController : ApplicationController
    {
        private readonly IShopService _shopService;
        public ProductsController(
            IGenericDataService _dataService,
            IMorWalPizCache _memoryCache,
            IShopService shopService) : base(_dataService, _memoryCache)
        {
            _shopService = shopService;
        }

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.Products])]
        public async Task<IActionResult> Index(int skip = 0, int take = 200)
        {
            var entities = await _shopService.GetProductsAsync(skip, take);
            return Ok(entities.Select(ContractUtils.Convert));
        }
    }
}
