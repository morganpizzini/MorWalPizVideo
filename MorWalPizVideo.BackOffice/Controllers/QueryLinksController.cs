using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers;
public class CreateQueryLinkRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
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
    [AllowUser(AuthorizationPermissionKeys.QueryLinksView, AuthorizationPermissionKeys.QueryLinksManage)]
    public async Task<IActionResult> GetQueryLink()
    {
        var entities = await _dataService.FetchQueryLinks();
        return Ok(entities.Select(ContractUtils.Convert));
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.QueryLinksView, AuthorizationPermissionKeys.QueryLinksManage)]
    public async Task<IActionResult> GetQueryLink(string id)
    {
        var entities = await _dataService.GetQueryLink(id);
        if (entities == null)
            return NotFound();
        return Ok(ContractUtils.Convert(entities));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.QueryLinksCreate, AuthorizationPermissionKeys.QueryLinksManage)]
    public async Task<IActionResult> CreateQueryLink(CreateQueryLinkRequest request)
    {
        await _dataService.SaveQueryLink(new QueryLink(request.Title, request.Value));
        return NoContent();
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.QueryLinksUpdate, AuthorizationPermissionKeys.QueryLinksManage)]
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
    [AllowUser(AuthorizationPermissionKeys.QueryLinksDelete, AuthorizationPermissionKeys.QueryLinksManage)]
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
