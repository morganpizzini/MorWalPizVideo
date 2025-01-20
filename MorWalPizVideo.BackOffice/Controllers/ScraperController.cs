using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

public class ScraperController : ApplicationController
{
    private readonly DataService dataService;
    private readonly IYTService ytService;
    
    public ScraperController(IYTService _ytService,DataService _dataService)
    {
        ytService = _ytService;
        dataService = _dataService;
    }
    [HttpGet]
    public async Task<IActionResult> Index(string channelName,int videos = 1,int commentsNumber = 20, bool showVideo = true)
    {
        var channel = await dataService.GetChannel(channelName);
        if (channel == null)
        {
            return BadRequest("Channel not found");
        }

        var comments = await ytService.GetChannelComments(channel.ChannelId, videos, commentsNumber, showVideo);

        // Salva i commenti in un file di testo
        var fileName = $"Comments_{channel.ChannelId}_{DateTime.Now:yyyyMMddHHmmss}.txt";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        await System.IO.File.WriteAllTextAsync(filePath, comments);
        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes, "text/plain", fileName);
    }
}
