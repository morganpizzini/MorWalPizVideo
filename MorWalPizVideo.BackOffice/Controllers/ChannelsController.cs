using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers;
public class AddChannelRequest
{
    [Required]
    public string ChannelName { get; set; } = string.Empty;
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

    public ChannelsController(IYTService _ytService, DataService dataService)
    {
        ytService = _ytService;
        _dataService = dataService;
    }

    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.ChannelsView, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> GetChannels()
    {
        var entities = await _dataService.GetChannels();
        return Ok(entities.Select(ContractUtils.Convert));
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ChannelsView, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> GetChannel(string id)
    {
        var existing = await _dataService.GetChannelById(id);
        if(existing == null)
        {
            return NotFound();
        }
        return Ok(ContractUtils.Convert(existing));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.ChannelsCreate, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> AddChannel(AddChannelRequest request)
    {
        var channelId = await ytService.GetChannelId(request.ChannelName);
        
        if (channelId == string.Empty)
        {
            return BadRequest("Channel not found");
        }
        await _dataService.SaveChannel(new YTChannel(channelId, request.ChannelName));
        
        return NoContent();
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.ChannelsUpdate, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> UpdateChannel(string id, UpdateChannelRequest request)
    {
        var existing = await _dataService.GetChannelById(id);
        if (existing is null)
        {
            return NotFound();
        }

        await _dataService.UpdateChannel(existing with { ChannelName = request.ChannelName.Trim() });
        return NoContent();
    }

    [HttpDelete("{channelName}")]
    [AllowUser(AuthorizationPermissionKeys.ChannelsDelete, AuthorizationPermissionKeys.ChannelsManage)]
    public async Task<IActionResult> RemoveChannel(string channelName)
    {
        await _dataService.RemoveChannel(channelName);
        return NoContent();
    }
}
