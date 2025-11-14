using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;
public class CreateQueryLinkRequest
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class UpdateQueryLinkRequest
{
    public string Title { get; set; } = string.Empty;
    public string NewTitle { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class QueryLinksController : ApplicationControllerBase
{
    private readonly DataService _dataService;

    public QueryLinksController(DataService dataService)
    {
        _dataService = dataService;
    }

    [HttpGet]
    public async Task<IActionResult> GetQueryLink()
    {
        var entities = await _dataService.GetQueryLinks();
        return Ok(entities);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQueryLink(CreateQueryLinkRequest request)
    {
        await _dataService.SaveQueryLink(new QueryLink(request.Title, request.Value));
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateQueryLink(UpdateQueryLinkRequest request)
    {
        var entity = await _dataService.GetQueryLinkByTitle(request.Title);
        if (entity == null)
            return BadRequest("Query link has not found");

        var updatedLink = entity with { Title = request.NewTitle, Value = request.Value };
        await _dataService.UpdateQueryLink(updatedLink);

        return NoContent();
    }

    [HttpDelete("{title}")]
    public async Task<IActionResult> DeleteQueryLink(string title)
    {
        var entity = await _dataService.GetQueryLinkByTitle(title);
        if (entity == null)
        {
            return BadRequest("Query link has not found");
        }

        await _dataService.DeleteQueryLink(entity.Id);
        return NoContent();
    }
}
