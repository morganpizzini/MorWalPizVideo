using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.BackOffice.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.BackOffice.Controllers;

public class AddChannelRequest
{
    [Required]
    public string ChannelName { get; set; } = string.Empty;

    [JsonPropertyName("yTChannelId")]
    public string? YTChannelId { get; set; }
}

public class UpdateChannelRequest
{
    [Required]
    public string ChannelName { get; set; } = string.Empty;
}

public class ChannelsController : ApplicationControllerBase
{
    private readonly DataService _dataService;
    private readonly IYTService ytService;
    private readonly IChannelContextResolver channelContextResolver;
    private readonly IVideoAuthorizationService channelAuthorization;

    public ChannelsController(
        IYTService _ytService,
        DataService dataService,
        IChannelContextResolver channelContextResolver,
        IVideoAuthorizationService channelAuthorization)
    {
        ytService = _ytService;
        _dataService = dataService;
        this.channelContextResolver = channelContextResolver;
        this.channelAuthorization = channelAuthorization;
    }

    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.ChannelsView, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> GetChannels()
    {
        var entities = await channelContextResolver.GetAccessibleChannelsAsync(User);
        return Ok(entities.Select(ContractUtils.Convert));
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ChannelsView, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> GetChannel(string id)
    {
        if (!await channelAuthorization.CanManageChannelAsync(User, id))
        {
            return NotFound();
        }

        var existing = await _dataService.GetChannelById(id);
        if (existing == null)
        {
            return NotFound();
        }
        return Ok(ContractUtils.Convert(existing));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.ChannelsCreate, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> AddChannel(AddChannelRequest request)
    {
        var channelId = request.YTChannelId is null
            ? await ytService.GetChannelId(request.ChannelName.Trim())
            : request.YTChannelId.Trim();

        if (string.IsNullOrWhiteSpace(channelId))
        {
            return BadRequest("YouTube channel ID is required");
        }
        await _dataService.SaveChannel(new YTChannel(channelId, request.ChannelName.Trim()));

        return NoContent();
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ChannelsUpdate, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> UpdateChannel(string id, UpdateChannelRequest request)
    {
        if (!await channelAuthorization.CanManageChannelAsync(User, id))
        {
            return NotFound();
        }

        var existing = await _dataService.GetChannelById(id);
        if (existing is null)
        {
            return NotFound();
        }

        await _dataService.UpdateChannel(existing with { ChannelName = request.ChannelName.Trim() });
        return NoContent();
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ChannelsDelete, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> RemoveChannel(string id)
    {
        if (!await channelAuthorization.CanManageChannelAsync(User, id))
        {
            return NotFound();
        }

        var existing = await _dataService.GetChannelById(id);
        if (existing is null)
        {
            return NotFound();
        }

        await _dataService.RemoveChannelById(id);
        return NoContent();
    }
}
