using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.BackOffice.Services;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers;

public sealed class QuickLinkRequest
{
    [Required] public QuickLinkKind Kind { get; set; }
    [Required, Url] public string TargetUrl { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Label { get; set; }
    public string? ImageUrl { get; set; }
    public string? Icon { get; set; }
    public string? Provider { get; set; }
}

public sealed class QuickLinksRequest
{
    [Required] public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    [Required] public string Url { get; set; } = string.Empty;
    public QuickLinkRequest[] Links { get; set; } = [];
}

[Route("api/[controller]")]
public sealed class QuickLinksController(IQuickLinksService quickLinksService, ICrossApiService crossApiService) : ApplicationControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.QuickLinksView, AuthorizationPermissionKeys.QuickLinksManage)]
    public async Task<IActionResult> Index()
        => Ok((await quickLinksService.GetAllAsync()).Select(ContractUtils.Convert));

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.QuickLinksView, AuthorizationPermissionKeys.QuickLinksManage)]
    public async Task<IActionResult> Detail(string id)
    {
        var entity = await quickLinksService.GetByIdAsync(id);
        return entity is null ? NotFound() : Ok(ContractUtils.Convert(entity));
    }

    [HttpPost]
    [RequireChannelScope]
    [AllowUser(AuthorizationPermissionKeys.QuickLinksCreate, AuthorizationPermissionKeys.QuickLinksManage)]
    public async Task<IActionResult> Create(QuickLinksRequest request)
    {
        var entity = ToEntity(request, HttpContext.GetChannelContext().ChannelId);
        var validationErrors = QuickLinksService.Validate(entity);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors);
        if (!await quickLinksService.IsUrlAvailableAsync(entity.Url))
            return Conflict("A QuickLinks page with this url already exists.");

        await quickLinksService.CreateAsync(entity);
        await InvalidateCachesAsync();
        return NoContent();
    }

    [HttpPut("{id}")]
    [RequireChannelScope]
    [AllowUser(AuthorizationPermissionKeys.QuickLinksUpdate, AuthorizationPermissionKeys.QuickLinksManage)]
    public async Task<IActionResult> Update(BaseRequestId<QuickLinksRequest> request)
    {
        var entity = await quickLinksService.GetByIdAsync(request.Id);
        if (entity is null || entity.ChannelId != HttpContext.GetChannelContext().ChannelId)
            return NotFound();

        var updated = ToEntity(request.Body, entity.ChannelId) with { Id = entity.Id, CreationDateTime = entity.CreationDateTime };
        var validationErrors = QuickLinksService.Validate(updated);
        if (validationErrors.Count > 0)
            return BadRequest(validationErrors);
        if (!await quickLinksService.IsUrlAvailableAsync(updated.Url, entity.Id))
            return Conflict("A QuickLinks page with this url already exists.");

        await quickLinksService.UpdateAsync(updated);
        await InvalidateCachesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [RequireChannelScope]
    [AllowUser(AuthorizationPermissionKeys.QuickLinksDelete, AuthorizationPermissionKeys.QuickLinksManage)]
    public async Task<IActionResult> Delete(BaseRequestId request)
    {
        var entity = await quickLinksService.GetByIdAsync(request.Id);
        if (entity is null || entity.ChannelId != HttpContext.GetChannelContext().ChannelId)
            return NotFound();

        await quickLinksService.DeleteAsync(entity.Id);
        await InvalidateCachesAsync();
        return NoContent();
    }

    private static QuickLinks ToEntity(QuickLinksRequest request, string channelId)
        => new(request.Title, request.Subtitle, request.Url, request.Links.Select(ToEntity).ToArray())
        {
            ChannelId = channelId
        };

    private static QuickLink ToEntity(QuickLinkRequest request)
        => new(request.Kind, request.TargetUrl, request.Title, request.Subtitle, request.Label,
            request.ImageUrl, request.Icon, request.Provider);

    private async Task InvalidateCachesAsync()
    {
        await crossApiService.ResetCache(CacheKeys.QuickLinks);
        await crossApiService.PurgeCache(CacheKeys.QuickLinks);
    }
}
