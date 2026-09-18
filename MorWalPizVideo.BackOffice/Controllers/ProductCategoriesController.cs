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

[RequireChannelScope]
public class ProductCategoriesController : ApplicationControllerBase
{
    private readonly DataService _dataService;
    private readonly ICrossApiService _crossApiService;

    public ProductCategoriesController(DataService dataService, ICrossApiService crossApiService)
    {
        _dataService = dataService;
        _crossApiService = crossApiService;
    }

    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.ProductCategoriesView, AuthorizationPermissionKeys.ProductCategoriesManage)]
    public async Task<IActionResult> GetProductCategories()
    {
        var entities = await _dataService.FetchProductCategories(null, HttpContext.GetChannelContext().ChannelId);
        return Ok(entities.Select(ContractUtils.Convert));
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ProductCategoriesView, AuthorizationPermissionKeys.ProductCategoriesManage)]
    public async Task<IActionResult> GetProductCategory(string id)
    {
        var entity = await _dataService.GetProductCategoryById(id, HttpContext.GetChannelContext().ChannelId);
        if (entity == null)
            return NotFound();
        return Ok(ContractUtils.Convert(entity));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.ProductCategoriesCreate, AuthorizationPermissionKeys.ProductCategoriesManage)]
    public async Task<IActionResult> CreateProductCategory(CreateProductCategoryRequest request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var category = new ProductCategory(request.Title, request.Description);
        if (!await _dataService.SaveProductCategory(category, channelId))
            return Conflict("A product category with this title already exists for the selected channel.");
        await InvalidateProductsCacheAsync();
        return NoContent();
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ProductCategoriesUpdate, AuthorizationPermissionKeys.ProductCategoriesManage)]
    public async Task<IActionResult> UpdateProductCategory(BaseRequestId<UpdateProductCategoryRequest> request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var entity = await _dataService.GetProductCategoryById(request.Id, channelId);
        if (entity == null)
            return BadRequest("Product category not found");

        var updatedCategory = entity with
        {
            Title = request.Body.Title,
            Description = request.Body.Description
        };

        if (!await _dataService.UpdateProductCategory(updatedCategory, channelId))
            return Conflict("A product category with this title already exists for the selected channel.");
        await InvalidateProductsCacheAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ProductCategoriesDelete, AuthorizationPermissionKeys.ProductCategoriesManage)]
    public async Task<IActionResult> DeleteProductCategory(BaseRequestId request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var entity = await _dataService.GetProductCategoryById(request.Id, channelId);
        if (entity == null)
        {
            return BadRequest("Product category not found");
        }

        await _dataService.DeleteProductCategory(entity.Id, channelId);
        await InvalidateProductsCacheAsync();
        return NoContent();
    }

    private async Task InvalidateProductsCacheAsync()
    {
        await _crossApiService.ResetCache(CacheKeys.Products);
        await _crossApiService.PurgeCache(ApiTagCacheKeys.Products);
    }
}
