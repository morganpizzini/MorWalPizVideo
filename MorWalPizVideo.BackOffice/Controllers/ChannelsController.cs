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
    public async Task<IActionResult> GetChannels()
    {
        return Ok(await _dataService.GetChannels());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetChannel(string id)
    {
        var existing = await _dataService.GetChannelById(id);
        if(existing == null)
        {
            return NotFound();
        }
        return Ok(existing);
    }

    [HttpPost]
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

    [HttpDelete("{channelName}")]
    public async Task<IActionResult> RemoveChannel(string channelName)
    {
        await _dataService.RemoveChannel(channelName);
        return NoContent();
    }
}
