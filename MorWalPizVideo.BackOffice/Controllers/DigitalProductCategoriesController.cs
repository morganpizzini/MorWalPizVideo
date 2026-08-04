using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

[Authorize]
[Route("api/shop/categories")]
// Legacy compatibility alias for the explicit admin route family under /api/admin/shop/categories.
public class DigitalProductCategoriesController : ApplicationControllerBase
{
    private readonly IShopManagementService _shopManagementService;

    public DigitalProductCategoriesController(IShopManagementService shopManagementService)
    {
        _shopManagementService = shopManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var entities = await _shopManagementService.GetDigitalProductCategoriesAsync();
        return Ok(entities.OrderBy(c => c.DisplayOrder ?? int.MaxValue).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(string id)
    {
        var entity = await _shopManagementService.GetDigitalProductCategoryByIdAsync(id);
        if (entity == null)
            return NotFound();
        return Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory(CreateDigitalProductCategoryRequest request)
    {
        var category = new DigitalProductCategory(request.Name, request.Description, request.DisplayOrder);
        await _shopManagementService.SaveDigitalProductCategoryAsync(category);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(string id, [FromBody] UpdateDigitalProductCategoryRequest request)
    {
        var entity = await _shopManagementService.GetDigitalProductCategoryByIdAsync(id);
        if (entity == null)
            return BadRequest("Digital product category not found");

        var updatedCategory = entity with
        {
            Name = request.Name,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder
        };

        await _shopManagementService.UpdateDigitalProductCategoryAsync(updatedCategory);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(string id)
    {
        var entity = await _shopManagementService.GetDigitalProductCategoryByIdAsync(id);
        if (entity == null)
            return BadRequest("Digital product category not found");

        await _shopManagementService.DeleteDigitalProductCategoryAsync(entity.Id);
        return NoContent();
    }
}
