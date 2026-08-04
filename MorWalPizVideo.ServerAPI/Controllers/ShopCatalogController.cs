using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPiz.Contracts;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [Route("api/shop")]
    [AllowAnonymous]
    public class ShopCatalogController : ApplicationController
    {
        private readonly IDigitalProductRepository _productRepository;
        private readonly IDigitalProductCategoryRepository _categoryRepository;

        public ShopCatalogController(
            IGenericDataService dataService,
            IMorWalPizCache memoryCache,
            IDigitalProductRepository productRepository,
            IDigitalProductCategoryRepository categoryRepository) : base(dataService, memoryCache)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        [HttpGet("products")]
        [OutputCache(Tags = [CacheKeys.DigitalProducts])]
        public async Task<IActionResult> GetProducts(int skip = 0, int take = 200)
        {
            var safeSkip = Math.Max(0, skip);
            var safeTake = Math.Clamp(take, 1, 500);
            var products = await _productRepository.GetPublicCatalogAsync(safeSkip, safeTake);
            return Ok(products.Select(ContractUtils.Convert));
        }

        [HttpGet("products/{id}")]
        [OutputCache(Tags = [CacheKeys.DigitalProducts], VaryByRouteValueNames = ["id"])]
        public async Task<IActionResult> GetProduct(string id)
        {
            var product = await _productRepository.GetItemAsync(id);
            return product == null ? NotFound() : Ok(ContractUtils.Convert(product));
        }

        [HttpGet("categories")]
        [OutputCache(Tags = [CacheKeys.DigitalProductCategories])]
        public async Task<IActionResult> GetCategories(int skip = 0, int take = 200)
        {
            var safeSkip = Math.Max(0, skip);
            var safeTake = Math.Clamp(take, 1, 500);
            var categories = await _categoryRepository.GetOrderedAsync(safeSkip, safeTake);
            return Ok(categories.Select(ContractUtils.Convert));
        }

        [HttpGet("categories/{id}")]
        [OutputCache(Tags = [CacheKeys.DigitalProductCategories], VaryByRouteValueNames = ["id"])]
        public async Task<IActionResult> GetCategory(string id)
        {
            var category = await _categoryRepository.GetItemAsync(id);
            return category == null ? NotFound() : Ok(ContractUtils.Convert(category));
        }

        [HttpGet("categories/{id}/products")]
        [OutputCache(Tags = [CacheKeys.DigitalProducts, CacheKeys.DigitalProductCategories], VaryByRouteValueNames = ["id"])]
        public async Task<IActionResult> GetProductsByCategory(string id, int limit = 2000)
        {
            var safeLimit = Math.Clamp(limit, 1, 2000);
            var products = await _productRepository.GetByCategoryIdAsync(id, safeLimit);
            return Ok(products.Select(ContractUtils.Convert));
        }
    }
}