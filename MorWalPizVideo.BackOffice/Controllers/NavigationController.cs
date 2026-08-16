using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

public sealed class NavigationRequest
{
    public bool IsActive { get; set; } = true;
    public NavigationMenuItemRequest[] HeaderItems { get; set; } = [];
    public int FooterColumnCount { get; set; } = 1;
    public NavigationMenuItemRequest[] FooterItems { get; set; } = [];
}

public sealed class NavigationMenuItemRequest
{
    public NavigationItemType Type { get; set; }
    public string? PageId { get; set; }
    public string TargetUrl { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public int Column { get; set; }
}

[Route("api/[controller]")]
[ApiController]
[RequireChannelScope]
public sealed class NavigationController(
    IChannelNavigationService navigationService,
    IPageService pageService,
    ICrossApiService crossApiService) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.NavigationView, AuthorizationPermissionKeys.NavigationManage)]
    public async Task<ActionResult<ChannelNavigationContract>> Get()
    {
        var navigation = await navigationService.GetForChannelAsync(HttpContext.GetChannelContext().ChannelId);
        return navigation is null ? Ok(null) : Ok(ContractUtils.Convert(navigation));
    }

    [HttpPut]
    [AllowUser(AuthorizationPermissionKeys.NavigationUpdate, AuthorizationPermissionKeys.NavigationManage)]
    public async Task<ActionResult<ChannelNavigationContract>> Save(NavigationRequest request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var pages = await pageService.GetForChannelAsync(channelId);
        var navigation = ToEntity(request, channelId);
        var errors = navigationService.Validate(navigation, pages.ToArray());
        if (errors.Count > 0) return BadRequest(errors);

        ChannelNavigation saved;
        try
        {
            saved = await navigationService.SaveAsync(navigation);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000)
        {
            return Conflict("A navigation configuration already exists for this channel.");
        }
        await crossApiService.ResetCache(CacheKeys.Navigation);
        await crossApiService.PurgeCache(ApiTagCacheKeys.Navigation);
        return Ok(ContractUtils.Convert(saved));
    }

    private static ChannelNavigation ToEntity(NavigationRequest request, string channelId) => new()
    {
        ChannelId = channelId,
        IsActive = request.IsActive,
        HeaderItems = request.HeaderItems.Select(ToEntity).ToArray(),
        FooterColumnCount = request.FooterColumnCount,
        FooterItems = request.FooterItems.Select(ToEntity).ToArray()
    };

    private static NavigationMenuItem ToEntity(NavigationMenuItemRequest request) => new()
    {
        Type = request.Type,
        PageId = request.PageId,
        TargetUrl = request.TargetUrl,
        DisplayText = request.DisplayText,
        Column = request.Column
    };
}
