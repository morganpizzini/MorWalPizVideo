using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers;
public class AddChannelRequest
{
    [Required]
    public string ChannelName { get; set; } = string.Empty;
}

public class ChannelsController : ApplicationController
{
    private readonly DataService dataService;
    private readonly IYTService ytService;

    public ChannelsController(IYTService _ytService, DataService _dataService)
    {
        ytService = _ytService;
        dataService = _dataService;
    }

    [HttpGet]
    public async Task<IActionResult> GetChannels()
    {
        return Ok(await dataService.GetChannels());
    }

    [HttpPost]
    public async Task<IActionResult> AddChannel(AddChannelRequest request)
    {
        var channelId = await ytService.GetChannelId(request.ChannelName);
        
        if (channelId == string.Empty)
        {
            return BadRequest("Channel not found");
        }
        await dataService.SaveChannel(new YTChannel(channelId, request.ChannelName));
        
        return NoContent();
    }

    [HttpDelete("{channelName}")]
    public async Task<IActionResult> RemoveChannel(string channelName)
    {
        await dataService.RemoveChannel(channelName);
        return NoContent();
    }
}
