using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Services.Interfaces;
namespace MorWalPizVideo.BackOffice.Controllers;

public class TelegramController : ApplicationControllerBase
{
    private readonly ITelegramService telegramService;
    public TelegramController(ITelegramService _telegramService, IConfiguration _configuration)
    {
        telegramService = _telegramService;
    }

    [HttpGet("{shortLink}")]
    public async Task<IActionResult> CreatePost(string shortLink, [FromQuery(Name = "message")] string message)
    {
        var result = await telegramService.CreatePost(shortLink, message);
        if(string.IsNullOrEmpty(result))
            return NoContent();
        return BadRequest(result);
    }
}
