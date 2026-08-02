using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers;

public class CreateDigitalProductCategoryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public int? DisplayOrder { get; set; }
}

public class UpdateDigitalProductCategoryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public int? DisplayOrder { get; set; }
}

[Route("api/shop/categories")]
public class DigitalProductCategoriesController : ApplicationControllerBase
{
    private readonly DataService _dataService;

    public DigitalProductCategoriesController(DataService dataService)
    {
        _dataService = dataService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var entities = await _dataService.GetProductCategoriesAsync();
        return Ok(entities.OrderBy(c => c.DisplayOrder ?? int.MaxValue).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(string id)
    {
        var entity = await _dataService.GetDigitalProductCategoryByIdAsync(id);
        if (entity == null)
            return NotFound();
        return Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory(CreateDigitalProductCategoryRequest request)
    {
        var category = new DigitalProductCategory(request.Name, request.Description, request.DisplayOrder);
        await _dataService.SaveDigitalProductCategory(category);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(string id, [FromBody] UpdateDigitalProductCategoryRequest request)
    {
        var entity = await _dataService.GetDigitalProductCategoryByIdAsync(id);
        if (entity == null)
            return BadRequest("Digital product category not found");

        var updatedCategory = entity with
        {
            Name = request.Name,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder
        };

        await _dataService.UpdateDigitalProductCategory(updatedCategory);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(string id)
    {
        var entity = await _dataService.GetDigitalProductCategoryByIdAsync(id);
        if (entity == null)
            return BadRequest("Digital product category not found");

        await _dataService.DeleteDigitalProductCategory(entity.Id);
        return NoContent();
    }
}
