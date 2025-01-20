using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;
public class AddPageRequest
{
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string VideoId { get; set; } = string.Empty;
}
public class PagesController : ApplicationController
{
    private readonly DataService dataService;
    public PagesController(DataService _dataService)
    {
        dataService = _dataService;
    }
    [HttpPost]
    public async Task<IActionResult> AddPage(AddPageRequest request)
    {
        Page page = new(request.Title, request.Content, request.Url, request.VideoId, request.ThumbnailUrl);

        await dataService.SavePage(page);

        return NoContent();
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> RemovePage(string id)
    {
        await dataService.RemovePage(id);
        return NoContent();
    }
}
