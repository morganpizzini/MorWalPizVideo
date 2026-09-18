using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Models.Constraints;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.BackOffice.Services;
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

[RequireChannelScope]
public class ProductsController : ApplicationControllerBase
{
    private readonly DataService _dataService;
    private readonly ICrossApiService _crossApiService;

    public ProductsController(DataService dataService, ICrossApiService crossApiService)
    {
        _dataService = dataService;
        _crossApiService = crossApiService;
    }

    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.ProductsView, AuthorizationPermissionKeys.ProductsManage)]
    public async Task<IActionResult> GetProducts()
    {
        var entities = await _dataService.GetProducts(HttpContext.GetChannelContext().ChannelId);
        return Ok(entities.Select(ContractUtils.Convert));
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ProductsView, AuthorizationPermissionKeys.ProductsManage)]
    public async Task<IActionResult> GetProduct(string id)
    {
        var entity = await _dataService.GetProductById(id, HttpContext.GetChannelContext().ChannelId);
        if (entity == null)
            return NotFound();
        return Ok(ContractUtils.Convert(entity));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.ProductsCreate, AuthorizationPermissionKeys.ProductsManage)]
    public async Task<IActionResult> CreateProduct(CreateProductRequest request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        // Fetch and validate categories
        CategoryRef[] categoryRefs = [];
        if (request.CategoryIds.Length > 0)
        {
            var categories = await _dataService.FetchProductCategories(request.CategoryIds, channelId);
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

        if (!await _dataService.SaveProduct(product, channelId))
            return Conflict("A product with this title already exists for the selected channel.");
        await InvalidateProductsCacheAsync();
        return NoContent();
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ProductsUpdate, AuthorizationPermissionKeys.ProductsManage)]
    public async Task<IActionResult> UpdateProduct(BaseRequestId<UpdateProductRequest> request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var entity = await _dataService.GetProductById(request.Id, channelId);
        if (entity == null)
            return BadRequest("Product not found");

        // Fetch and validate categories
        CategoryRef[] categoryRefs = [];
        if (request.Body.CategoryIds.Length > 0)
        {
            var categories = await _dataService.FetchProductCategories(request.Body.CategoryIds, channelId);
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

        if (!await _dataService.UpdateProduct(updatedProduct, channelId))
            return Conflict("A product with this title already exists for the selected channel.");
        await InvalidateProductsCacheAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ProductsDelete, AuthorizationPermissionKeys.ProductsManage)]
    public async Task<IActionResult> DeleteProduct(BaseRequestId request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var entity = await _dataService.GetProductById(request.Id, channelId);
        if (entity == null)
        {
            return BadRequest("Product not found");
        }

        await _dataService.DeleteProduct(entity.Id, channelId);
        await InvalidateProductsCacheAsync();
        return NoContent();
    }

    private async Task InvalidateProductsCacheAsync()
    {
        await _crossApiService.ResetCache(CacheKeys.Products);
        await _crossApiService.PurgeCache(ApiTagCacheKeys.Products);
    }
}
