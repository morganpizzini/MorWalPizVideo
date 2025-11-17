using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPizVideo.MvcHelpers.Utils;
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
        var entities = await _dataService.FetchQueryLinks();
        return Ok(entities.Select(ContractUtils.Convert));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetQueryLink(string id)
    {
        var entities = await _dataService.GetQueryLink(id);
        if (entities == null)
            return NotFound();
        return Ok(ContractUtils.Convert(entities));
    }

    [HttpPost]
    public async Task<IActionResult> CreateQueryLink(CreateQueryLinkRequest request)
    {
        await _dataService.SaveQueryLink(new QueryLink(request.Title, request.Value));
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQueryLink(BaseRequestId<UpdateQueryLinkRequest> request)
    {
        var entity = await _dataService.GetQueryLink(request.Id);
        if (entity == null)
            return BadRequest("Query link has not found");

        var updatedLink = entity with { Title = request.Body.Title, Value = request.Body.Value };
        await _dataService.UpdateQueryLink(updatedLink);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQueryLink(BaseRequestId request)
    {
        var entity = await _dataService.GetQueryLink(request.Id);
        if (entity == null)
        {
            return BadRequest("Query link has not found");
        }

        await _dataService.DeleteQueryLink(entity.Id);
        return NoContent();
    }
}
