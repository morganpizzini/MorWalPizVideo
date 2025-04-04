using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;
public class CreateCategoryRequest
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class UpdateCategoryRequest
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class CategoryController : ApplicationController
{
    private readonly DataService _dataService;
    
    public CategoryController(DataService dataService)
    {
        _dataService = dataService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _dataService.GetCategories();
        return Ok(categories);
    }

    [HttpGet("{title}")]
    public async Task<IActionResult> GetCategory(string title)
    {
        var category = await _dataService.GetCategory(title);
        if (category == null)
            return NotFound("Category not found");

        return Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory(CreateCategoryRequest request)
    {
        var category = new Category(request.Title, request.Value);
        await _dataService.SaveCategory(category);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(string id, UpdateCategoryRequest request)
    {
        var entity = await _dataService.GetCategoryById(id);
        if (entity == null)
            return BadRequest("Category not found");

        entity = entity with { Title = request.Title, Value = request.Value };
        await _dataService.UpdateCategory(entity);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(string id)
    {
        var entity = await _dataService.GetCategoryById(id);
        if (entity == null)
            return BadRequest("Category not found");
            
        await _dataService.DeleteCategory(id);
        return NoContent();
    }
}