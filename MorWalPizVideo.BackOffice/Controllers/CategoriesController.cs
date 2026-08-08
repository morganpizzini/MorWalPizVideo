using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.BackOffice.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

public class CreateCategoryRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateCategoryRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

[RequireChannelScope]
public class CategoriesController : ApplicationControllerBase
{
    private readonly DataService _dataService;

    public CategoriesController(DataService dataService)
    {
        _dataService = dataService;
    }

    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.CategoriesView, AuthorizationPermissionKeys.CategoriesManage)]
    public async Task<IActionResult> GetCategories()
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var categories = await _dataService.FetchCategories(null, channelId);
        return Ok(categories.Select(ContractUtils.Convert));
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.CategoriesView, AuthorizationPermissionKeys.CategoriesManage)]
    public async Task<IActionResult> GetCategory(string id)
    {
        var category = await _dataService.GetCategoryById(id, HttpContext.GetChannelContext().ChannelId);
        if (category == null)
            return NotFound("Category not found");

        return Ok(ContractUtils.Convert(category));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.CategoriesCreate, AuthorizationPermissionKeys.CategoriesManage)]
    public async Task<IActionResult> CreateCategory(CreateCategoryRequest request)
    {
        var category = new Category(request.Title, request.Description)
        {
            ChannelId = HttpContext.GetChannelContext().ChannelId
        };
        await _dataService.SaveCategory(category);
        return NoContent();
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.CategoriesUpdate, AuthorizationPermissionKeys.CategoriesManage)]
    public async Task<IActionResult> UpdateCategory(string id, UpdateCategoryRequest request)
    {
        var entity = await _dataService.GetCategoryById(id, HttpContext.GetChannelContext().ChannelId);
        if (entity == null)
            return BadRequest("Category not found");

        entity = entity with { Title = request.Title, Description = request.Description };
        await _dataService.UpdateCategory(entity, HttpContext.GetChannelContext().ChannelId);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.CategoriesDelete, AuthorizationPermissionKeys.CategoriesManage)]
    public async Task<IActionResult> DeleteCategory(string id)
    {
        var entity = await _dataService.GetCategoryById(id, HttpContext.GetChannelContext().ChannelId);
        if (entity == null)
            return BadRequest("Category not found");

        await _dataService.DeleteCategory(id, HttpContext.GetChannelContext().ChannelId);
        return NoContent();
    }
}