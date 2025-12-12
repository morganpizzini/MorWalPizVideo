using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers;

public class CreateProductCategoryRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
}

public class UpdateProductCategoryRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
}

public class ProductCategoriesController : ApplicationControllerBase
{
    private readonly DataService _dataService;

    public ProductCategoriesController(DataService dataService)
    {
        _dataService = dataService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProductCategories()
    {
        var entities = await _dataService.FetchProductCategories();
        return Ok(entities);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductCategory(string id)
    {
        var entity = await _dataService.GetProductCategoryById(id);
        if (entity == null)
            return NotFound();
        return Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProductCategory(CreateProductCategoryRequest request)
    {
        var category = new ProductCategory(request.Title, request.Description);
        await _dataService.SaveProductCategory(category);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProductCategory(BaseRequestId<UpdateProductCategoryRequest> request)
    {
        var entity = await _dataService.GetProductCategoryById(request.Id);
        if (entity == null)
            return BadRequest("Product category not found");

        var updatedCategory = entity with
        {
            Title = request.Body.Title,
            Description = request.Body.Description
        };

        await _dataService.UpdateProductCategory(updatedCategory);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProductCategory(BaseRequestId request)
    {
        var entity = await _dataService.GetProductCategoryById(request.Id);
        if (entity == null)
        {
            return BadRequest("Product category not found");
        }

        await _dataService.DeleteProductCategory(entity.Id);
        return NoContent();
    }
}
