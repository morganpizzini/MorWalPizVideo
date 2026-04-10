using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [Route("api/shop")]
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
        public async Task<IActionResult> GetProducts()
        {
            var products = await _productRepository.GetItemsAsync();
            return Ok(products);
        }

        [HttpGet("products/{id}")]
        [OutputCache(Tags = [CacheKeys.DigitalProducts], VaryByRouteValueNames = ["id"])]
        public async Task<IActionResult> GetProduct(string id)
        {
            var product = await _productRepository.GetItemAsync(id);
            return product == null ? NotFound() : Ok(product);
        }

        [HttpGet("categories")]
        [OutputCache(Tags = [CacheKeys.DigitalProductCategories])]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryRepository.GetItemsAsync();
            return Ok(categories);
        }

        [HttpGet("categories/{id}")]
        [OutputCache(Tags = [CacheKeys.DigitalProductCategories], VaryByRouteValueNames = ["id"])]
        public async Task<IActionResult> GetCategory(string id)
        {
            var category = await _categoryRepository.GetItemAsync(id);
            return category == null ? NotFound() : Ok(category);
        }

        [HttpGet("categories/{id}/products")]
        [OutputCache(Tags = [CacheKeys.DigitalProducts, CacheKeys.DigitalProductCategories], VaryByRouteValueNames = ["id"])]
        public async Task<IActionResult> GetProductsByCategory(string id)
        {
            var products = await _productRepository.GetItemsAsync();
            var filtered = products.Where(p => p.CategoryIds.Contains(id)).ToList();
            return Ok(filtered);
        }
    }
}