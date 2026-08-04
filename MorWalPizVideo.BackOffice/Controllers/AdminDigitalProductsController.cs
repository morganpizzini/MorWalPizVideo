using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

[Authorize]
[Route("api/admin/shop/products")]
// Explicit BackOffice admin route for digital artifact management.
public class AdminDigitalProductsController : ApplicationControllerBase
{
    private readonly IShopManagementService _shopManagementService;

    public AdminDigitalProductsController(IShopManagementService shopManagementService)
    {
        _shopManagementService = shopManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var entities = await _shopManagementService.GetDigitalProductsAsync();
        return Ok(entities);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(string id)
    {
        var entity = await _shopManagementService.GetDigitalProductByIdAsync(id);
        if (entity == null)
            return NotFound();
        return Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(CreateDigitalProductRequest request)
    {
        var product = new DigitalProduct(
            request.Name,
            request.Description,
            request.PreviewImageUrl,
            request.ContentStorageKey,
            request.CategoryIds,
            request.Price,
            request.IsActive);

        await _shopManagementService.SaveDigitalProductAsync(product);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(string id, [FromBody] UpdateDigitalProductRequest request)
    {
        var entity = await _shopManagementService.GetDigitalProductByIdAsync(id);
        if (entity == null)
            return BadRequest("Digital product not found");

        var updatedProduct = entity with
        {
            Name = request.Name ?? entity.Name,
            Description = request.Description ?? entity.Description,
            PreviewImageUrl = request.PreviewImageUrl ?? entity.PreviewImageUrl,
            ContentStorageKey = request.ContentStorageKey ?? entity.ContentStorageKey,
            CategoryIds = request.CategoryIds ?? entity.CategoryIds,
            Price = request.Price ?? entity.Price,
            IsActive = request.IsActive ?? entity.IsActive
        };

        await _shopManagementService.UpdateDigitalProductAsync(updatedProduct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        var entity = await _shopManagementService.GetDigitalProductByIdAsync(id);
        if (entity == null)
            return BadRequest("Digital product not found");

        await _shopManagementService.DeleteDigitalProductAsync(entity.Id);
        return NoContent();
    }
}
