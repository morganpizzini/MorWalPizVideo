using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers;

public class CreateProductRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
    
    public string[] CategoryIds { get; set; } = [];
}

public class UpdateProductRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
    
    public string[] CategoryIds { get; set; } = [];
}

public class ProductsController : ApplicationControllerBase
{
    private readonly DataService _dataService;

    public ProductsController(DataService dataService)
    {
        _dataService = dataService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var entities = await _dataService.GetProducts();
        return Ok(entities);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(string id)
    {
        var entity = await _dataService.GetProductById(id);
        if (entity == null)
            return NotFound();
        return Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(CreateProductRequest request)
    {
        // Fetch and validate categories
        CategoryRef[] categoryRefs = [];
        if (request.CategoryIds.Length > 0)
        {
            var categories = await _dataService.FetchProductCategories(request.CategoryIds);
            if (categories.Count != request.CategoryIds.Length)
                return BadRequest("One or more category IDs are invalid");
            
            categoryRefs = categories.Select(c => new CategoryRef(c.Id, c.Title)).ToArray();
        }

        var product = new Product(
            request.Title,
            request.Description,
            request.Url,
            categoryRefs
        );

        await _dataService.SaveProduct(product);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(BaseRequestId<UpdateProductRequest> request)
    {
        var entity = await _dataService.GetProductById(request.Id);
        if (entity == null)
            return BadRequest("Product not found");

        // Fetch and validate categories
        CategoryRef[] categoryRefs = [];
        if (request.Body.CategoryIds.Length > 0)
        {
            var categories = await _dataService.FetchProductCategories(request.Body.CategoryIds);
            if (categories.Count != request.Body.CategoryIds.Length)
                return BadRequest("One or more category IDs are invalid");
            
            categoryRefs = categories.Select(c => new CategoryRef(c.Id, c.Title)).ToArray();
        }

        var updatedProduct = entity with
        {
            Title = request.Body.Title,
            Description = request.Body.Description,
            Url = request.Body.Url,
            Categories = categoryRefs
        };

        await _dataService.UpdateProduct(updatedProduct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(BaseRequestId request)
    {
        var entity = await _dataService.GetProductById(request.Id);
        if (entity == null)
        {
            return BadRequest("Product not found");
        }

        await _dataService.DeleteProduct(entity.Id);
        return NoContent();
    }
}
