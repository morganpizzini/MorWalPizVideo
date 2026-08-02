using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

[Route("api/shop/products")]
public class DigitalProductsController : ApplicationControllerBase
{
    private readonly DataService _dataService;

    public DigitalProductsController(DataService dataService)
    {
        _dataService = dataService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var entities = await _dataService.GetDigitalProductsAsync();
        return Ok(entities);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(string id)
    {
        var entity = await _dataService.GetDigitalProductByIdAsync(id);
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

        await _dataService.SaveDigitalProduct(product);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(string id, [FromBody] UpdateDigitalProductRequest request)
    {
        var entity = await _dataService.GetDigitalProductByIdAsync(id);
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

        await _dataService.UpdateDigitalProduct(updatedProduct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        var entity = await _dataService.GetDigitalProductByIdAsync(id);
        if (entity == null)
            return BadRequest("Digital product not found");

        await _dataService.DeleteDigitalProduct(entity.Id);
        return NoContent();
    }
}
