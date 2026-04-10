using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/shop/catalog")]
public class ShopCatalogController : ControllerBase
{
    private readonly DataService _dataService;
    private readonly ILogger<ShopCatalogController> _logger;

    public ShopCatalogController(DataService dataService, ILogger<ShopCatalogController> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    [HttpGet("products")]
    public async Task<ActionResult<List<DigitalProduct>>> GetProducts()
    {
        try
        {
            var products = await _dataService.GetDigitalProductsAsync();
            var activeProducts = products.Where(p => p.IsActive).ToList();
            return Ok(activeProducts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching digital products");
            return StatusCode(500, "Error fetching products");
        }
    }

    [HttpGet("products/{id}")]
    public async Task<ActionResult<DigitalProduct>> GetProductById(string id)
    {
        try
        {
            var product = await _dataService.GetDigitalProductByIdAsync(id);
            
            if (product == null)
                return NotFound();

            if (!product.IsActive)
                return NotFound();

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId}", id);
            return StatusCode(500, "Error fetching product");
        }
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<ProductCategory>>> GetCategories()
    {
        try
        {
            var categories = await _dataService.GetProductCategoriesAsync();
            return Ok(categories.OrderBy(c => c.DisplayOrder ?? int.MaxValue).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product categories");
            return StatusCode(500, "Error fetching categories");
        }
    }
}