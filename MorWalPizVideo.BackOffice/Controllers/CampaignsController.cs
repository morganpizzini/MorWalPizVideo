using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.MvcHelpers.Utils;
using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.Controllers;

public sealed class CampaignRequest
{
    [Required]
    public string PartnerId { get; set; } = string.Empty;
    [Required]
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public IReadOnlyList<string> ContentReferences { get; set; } = [];
}

public sealed class TimelineEntryRequest
{
    public TimelineEntryKind Kind { get; set; } = TimelineEntryKind.Note;
    [Required]
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? OccurredAt { get; set; }
    public DateTime? DueAt { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
}

[Route("api/[controller]")]
[ApiController]
[RequireChannelScope]
public sealed class CampaignsController(DataService dataService) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.CampaignsView, AuthorizationPermissionKeys.CampaignsManage)]
    public async Task<ActionResult<IReadOnlyList<CampaignContract>>> GetAll([FromQuery] bool includeArchived = false)
        => Ok((await dataService.GetCampaignsAsync(ChannelId, includeArchived)).Select(ContractUtils.Convert).ToArray());

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.CampaignsView, AuthorizationPermissionKeys.CampaignsManage)]
    public async Task<ActionResult<CampaignContract>> Get(string id)
    {
        var campaign = await dataService.GetCampaignAsync(id, ChannelId);
        return campaign is null ? NotFound() : Ok(ContractUtils.Convert(campaign));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.CampaignsCreate, AuthorizationPermissionKeys.CampaignsManage)]
    public async Task<ActionResult<CampaignContract>> Create(CampaignRequest request)
    {
        var campaign = await dataService.SaveCampaignAsync(ToEntity(request), ChannelId);
        return campaign is null ? BadRequest("A valid partner and campaign title are required.") : CreatedAtAction(nameof(Get), new { id = campaign.Id }, ContractUtils.Convert(campaign));
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.CampaignsUpdate, AuthorizationPermissionKeys.CampaignsManage)]
    public async Task<ActionResult<CampaignContract>> Update(string id, CampaignRequest request)
    {
        var campaign = await dataService.UpdateCampaignAsync(ToEntity(request) with { Id = id }, ChannelId);
        return campaign is null ? NotFound() : Ok(ContractUtils.Convert(campaign));
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.CampaignsDelete, AuthorizationPermissionKeys.CampaignsManage)]
    public async Task<IActionResult> Delete(string id)
        => await dataService.DeleteCampaignAsync(id, ChannelId) ? NoContent() : NotFound();

    [HttpPost("{id}/timeline")]
    [AllowUser(AuthorizationPermissionKeys.CampaignsUpdate, AuthorizationPermissionKeys.CampaignsManage)]
    public async Task<ActionResult<CampaignContract>> AddTimelineEntry(string id, TimelineEntryRequest request)
        => await SaveTimeline(id, null, request);

    [HttpPut("{id}/timeline/{entryId}")]
    [AllowUser(AuthorizationPermissionKeys.CampaignsUpdate, AuthorizationPermissionKeys.CampaignsManage)]
    public async Task<ActionResult<CampaignContract>> UpdateTimelineEntry(string id, string entryId, TimelineEntryRequest request)
        => await SaveTimeline(id, entryId, request);

    [HttpDelete("{id}/timeline/{entryId}")]
    [AllowUser(AuthorizationPermissionKeys.CampaignsUpdate, AuthorizationPermissionKeys.CampaignsManage)]
    public async Task<IActionResult> DeleteTimelineEntry(string id, string entryId)
        => await dataService.DeleteTimelineAsync(id, ChannelId, entryId) is null ? NotFound() : NoContent();

    private string ChannelId => HttpContext.GetChannelContext().ChannelId;
    private async Task<ActionResult<CampaignContract>> SaveTimeline(string id, string? entryId, TimelineEntryRequest request)
    {
        var campaign = await dataService.UpdateTimelineAsync(id, ChannelId, entryId, new TimelineEntry
        {
            Id = entryId ?? string.Empty, Kind = request.Kind, Title = request.Title, Description = request.Description,
            OccurredAt = request.OccurredAt, DueAt = request.DueAt, Url = request.Url, IsComplete = request.IsComplete
        });
        return campaign is null ? NotFound() : Ok(ContractUtils.Convert(campaign));
    }

    private static Campaign ToEntity(CampaignRequest request) => new()
    {
        PartnerId = request.PartnerId, Title = request.Title, Description = request.Description, Status = request.Status,
        StartAt = request.StartAt, EndAt = request.EndAt, ContentReferences = request.ContentReferences ?? []
    };
}