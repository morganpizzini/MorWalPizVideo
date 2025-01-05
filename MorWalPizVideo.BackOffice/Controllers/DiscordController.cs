using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Controllers;

public class DiscordController : ApplicationController
{
    private readonly IDiscordService discordService;
    public DiscordController(IDiscordService _discordService)
    {
        discordService = _discordService;
    }

    [HttpGet("{shortLink}")]
    public async Task<IActionResult> CreatePost(string shortLink, [FromQuery(Name = "message")] string message)
    {
        var result = await discordService.CreatePost(shortLink, message);

        if (string.IsNullOrEmpty(result))
            return NoContent();
        return BadRequest(result);
    }
}
