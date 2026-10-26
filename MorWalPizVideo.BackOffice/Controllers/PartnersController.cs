using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers;

public sealed class PartnerRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    [EmailAddress]
    public string? ContactEmail { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    [Url]
    public string? WebsiteUrl { get; set; }
    public string Notes { get; set; } = string.Empty;
    public PartnerStatus Status { get; set; } = PartnerStatus.Prospect;
}

[Route("api/[controller]")]
[ApiController]
[RequireChannelScope]
public sealed class PartnersController(DataService dataService) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.PartnersView, AuthorizationPermissionKeys.PartnersManage)]
    public async Task<ActionResult<IReadOnlyList<PartnerContract>>> GetAll([FromQuery] bool includeArchived = false)
        => Ok((await dataService.GetPartnersAsync(ChannelId, includeArchived)).Select(ContractUtils.Convert).ToArray());

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.PartnersView, AuthorizationPermissionKeys.PartnersManage)]
    public async Task<ActionResult<PartnerContract>> Get(string id)
    {
        var partner = await dataService.GetPartnerAsync(id, ChannelId);
        return partner is null ? NotFound() : Ok(ContractUtils.Convert(partner));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.PartnersCreate, AuthorizationPermissionKeys.PartnersManage)]
    public async Task<ActionResult<PartnerContract>> Create(PartnerRequest request)
    {
        var partner = await dataService.SavePartnerAsync(ToEntity(request), ChannelId);
        return partner is null ? Conflict("A partner with this name already exists in the channel.") : CreatedAtAction(nameof(Get), new { id = partner.Id }, ContractUtils.Convert(partner));
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.PartnersUpdate, AuthorizationPermissionKeys.PartnersManage)]
    public async Task<ActionResult<PartnerContract>> Update(string id, PartnerRequest request)
    {
        var partner = await dataService.UpdatePartnerAsync(ToEntity(request) with { Id = id }, ChannelId);
        return partner is null ? NotFound() : Ok(ContractUtils.Convert(partner));
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.PartnersDelete, AuthorizationPermissionKeys.PartnersManage)]
    public async Task<IActionResult> Delete(string id)
        => await dataService.DeletePartnerAsync(id, ChannelId) ? NoContent() : Conflict("Partner not found or still referenced by a campaign.");

    private string ChannelId => HttpContext.GetChannelContext().ChannelId;
    private static Partner ToEntity(PartnerRequest request) => new()
    {
        Name = request.Name, ContactName = request.ContactName, ContactEmail = request.ContactEmail ?? string.Empty,
        ContactPhone = request.ContactPhone, WebsiteUrl = request.WebsiteUrl ?? string.Empty, Notes = request.Notes, Status = request.Status
    };
}