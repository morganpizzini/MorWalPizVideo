using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Controllers;

public sealed class EditorialProductionCardRequest
{
    public string Title { get; set; } = string.Empty;
    public string EditorialAngle { get; set; } = string.Empty;
    public IReadOnlyList<string> SourceUrls { get; set; } = [];
    public string Brief { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public EditorialProductionCardStatus Status { get; set; } = EditorialProductionCardStatus.Idea;
    public string? OwnerUserId { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public string? TopicId { get; set; }
    public string? NewsItemId { get; set; }
    public string? ContentPlanId { get; set; }
    public string? YoutubeVideoId { get; set; }
    public string? BackOfficeContentId { get; set; }
    public long Revision { get; set; }
}

[Route("api/editorial-production-cards")]
[ApiController]
[RequireChannelScope]
public sealed class EditorialProductionCardsController(IEditorialProductionCardService service) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.EditorialProductionCardsView, AuthorizationPermissionKeys.EditorialProductionCardsManage)]
    public async Task<ActionResult<IReadOnlyList<EditorialProductionCardContract>>> GetAll()
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var cards = await service.GetForChannelAsync(channelId);
        return Ok(cards.OrderByDescending(card => card.UpdatedDateTime).Select(ContractUtils.Convert).ToArray());
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.EditorialProductionCardsView, AuthorizationPermissionKeys.EditorialProductionCardsManage)]
    public async Task<ActionResult<EditorialProductionCardContract>> Get(string id)
    {
        var card = await service.GetAsync(id, HttpContext.GetChannelContext().ChannelId);
        return card is null ? NotFound() : Ok(ContractUtils.Convert(card));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.EditorialProductionCardsCreate, AuthorizationPermissionKeys.EditorialProductionCardsManage)]
    public async Task<ActionResult<EditorialProductionCardContract>> Create(EditorialProductionCardRequest request)
    {
        try
        {
            var card = await service.CreateAsync(ToEntity(request, HttpContext.GetChannelContext().ChannelId));
            return CreatedAtAction(nameof(Get), new { id = card.Id }, ContractUtils.Convert(card));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost("from-plan/{planId}")]
    [AllowUser(AuthorizationPermissionKeys.EditorialProductionCardsCreate, AuthorizationPermissionKeys.EditorialProductionCardsManage)]
    public async Task<ActionResult<EditorialProductionCardContract>> CreateFromPlan(string planId)
    {
        try
        {
            var card = await service.CreateFromPlanAsync(planId, HttpContext.GetChannelContext().ChannelId);
            return card is null
                ? NotFound()
                : CreatedAtAction(nameof(Get), new { id = card.Id }, ContractUtils.Convert(card));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.EditorialProductionCardsUpdate, AuthorizationPermissionKeys.EditorialProductionCardsManage)]
    public async Task<ActionResult<EditorialProductionCardContract>> Update(string id, EditorialProductionCardRequest request)
    {
        try
        {
            var card = await service.UpdateAsync(ToEntity(request, HttpContext.GetChannelContext().ChannelId) with { Id = id },
                HttpContext.GetChannelContext().ChannelId, request.Revision);
            return card is null ? Conflict(new { error = "The card is missing or has changed." }) : Ok(ContractUtils.Convert(card));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.EditorialProductionCardsDelete, AuthorizationPermissionKeys.EditorialProductionCardsManage)]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await service.DeleteAsync(id, HttpContext.GetChannelContext().ChannelId);
        return deleted ? NoContent() : NotFound();
    }

    private static EditorialProductionCard ToEntity(EditorialProductionCardRequest request, string channelId) => new()
    {
        ChannelId = channelId,
        Title = request.Title,
        EditorialAngle = request.EditorialAngle,
        SourceUrls = request.SourceUrls,
        Brief = request.Brief,
        Script = request.Script,
        Status = request.Status,
        OwnerUserId = request.OwnerUserId,
        DueDateUtc = request.DueDateUtc,
        TopicId = request.TopicId,
        NewsItemId = request.NewsItemId,
        ContentPlanId = request.ContentPlanId,
        YoutubeVideoId = request.YoutubeVideoId,
        BackOfficeContentId = request.BackOfficeContentId,
        Revision = request.Revision
    };
}