using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Controllers;

public sealed class AskCampaignRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public AskCampaignStatus Status { get; set; } = AskCampaignStatus.Draft;
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public AskPolicy Policy { get; set; } = new();
}

[Route("api/[controller]")]
[ApiController]
[RequireChannelScope]
public sealed class AskController(IAskService askService, ICrossApiService crossApiService, IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.AskView, AuthorizationPermissionKeys.AskManage)]
    public async Task<ActionResult<IReadOnlyList<AskCampaignContract>>> GetAll()
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var items = await askService.GetForChannelAsync(channelId);
        return Ok(items.Select(item => ContractUtils.Convert(item)).ToArray());
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.AskView, AuthorizationPermissionKeys.AskManage)]
    public async Task<ActionResult<AskCampaignContract>> Get(string id)
    {
        var item = await askService.GetByIdAsync(id, HttpContext.GetChannelContext().ChannelId);
        return item is null ? NotFound() : Ok(ContractUtils.Convert(item));
    }

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.AskCreate, AuthorizationPermissionKeys.AskManage)]
    public async Task<ActionResult<AskCampaignContract>> Create(AskCampaignRequest request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var item = new AskCampaign { ChannelId = channelId, Title = request.Title, Description = request.Description, Slug = request.Slug, Status = request.Status, StartAt = request.StartAt, EndAt = request.EndAt, Policy = request.Policy };
        var errors = askService.Validate(item);
        if (errors.Count > 0) return BadRequest(errors);
        try { item = await askService.CreateAsync(item); }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000) { return Conflict("A campaign with this slug already exists in the channel."); }
        await InvalidateAsync();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ContractUtils.Convert(item));
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.AskUpdate, AuthorizationPermissionKeys.AskManage)]
    public async Task<ActionResult<AskCampaignContract>> Update(string id, AskCampaignRequest request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var item = new AskCampaign { Id = id, ChannelId = channelId, Title = request.Title, Description = request.Description, Slug = request.Slug, Status = request.Status, StartAt = request.StartAt, EndAt = request.EndAt, Policy = request.Policy };
        var errors = askService.Validate(item);
        if (errors.Count > 0) return BadRequest(errors);
        try { item = await askService.UpdateAsync(item, channelId) ?? throw new KeyNotFoundException(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000) { return Conflict("A campaign with this slug already exists in the channel."); }
        await InvalidateAsync();
        return Ok(ContractUtils.Convert(item));
    }

    [HttpGet("{id}/submissions")]
    [AllowUser(AuthorizationPermissionKeys.AskView, AuthorizationPermissionKeys.AskManage)]
    public async Task<ActionResult<IReadOnlyList<AskSubmissionContract>>> GetSubmissions(string id)
        => Ok((await askService.GetSubmissionsAsync(id, HttpContext.GetChannelContext().ChannelId)).Select(ContractUtils.Convert).ToArray());

    [HttpPost("submissions/{id}/response")]
    [AllowUser(AuthorizationPermissionKeys.AskModerate, AuthorizationPermissionKeys.AskManage)]
    public async Task<ActionResult<AskSubmissionContract>> Respond(string id, AskResponseRequest request)
    {
        var item = await askService.RespondAsync(id, HttpContext.GetChannelContext().ChannelId, request.Content, request.Author, request.Visibility);
        return item is null ? BadRequest() : Ok(ContractUtils.Convert(item));
    }

    [HttpGet("{id}/analytics")]
    [AllowUser(AuthorizationPermissionKeys.AskView, AuthorizationPermissionKeys.AskManage)]
    public async Task<ActionResult<AskAnalytics>> Analytics(string id)
    {
        var result = await askService.GetAnalyticsAsync(id, HttpContext.GetChannelContext().ChannelId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id}/share")]
    [AllowUser(AuthorizationPermissionKeys.AskView, AuthorizationPermissionKeys.AskManage)]
    public async Task<ActionResult> Share(string id)
    {
        var campaign = await askService.GetByIdAsync(id, HttpContext.GetChannelContext().ChannelId);
        if (campaign is null) return NotFound();
        var baseUrl = (configuration["Ask:PublicBaseUrl"] ?? "https://ask.morwalpiz.com").TrimEnd('/');
        var channelName = await askService.GetChannelNameAsync(campaign.ChannelId) ?? campaign.ChannelId;
        var shareUrl = $"{baseUrl}/{Uri.EscapeDataString(channelName)}/{Uri.EscapeDataString(campaign.Slug)}";
        return Ok(new { shareUrl, qrUrl = $"/api/QRCode/ask?data={Uri.EscapeDataString(shareUrl)}" });
    }

    [HttpGet("{id}/export")]
    [AllowUser(AuthorizationPermissionKeys.AskView, AuthorizationPermissionKeys.AskManage)]
    public async Task<IActionResult> Export(string id, [FromQuery] bool includeName = false)
    {
        var campaign = await askService.GetByIdAsync(id, HttpContext.GetChannelContext().ChannelId);
        if (campaign is null) return NotFound();
        var rows = await askService.GetSubmissionsAsync(id, campaign.ChannelId, 5000);
        var csv = new StringBuilder("id,submittedAt,status,text,responseContent,responseAuthor,responseCreatedAt");
        if (includeName) csv.Append(",name");
        csv.AppendLine();
        foreach (var row in rows)
        {
            csv.Append(string.Join(',', Csv(row.Id), Csv(row.SubmittedAt.ToString("O")), Csv(row.ModerationStatus.ToString()), Csv(row.Text), Csv(row.ResponseContent), Csv(row.ResponseAuthor), Csv(row.ResponseCreatedAt?.ToString("O") ?? string.Empty)));
            if (includeName) csv.Append(',').Append(Csv(row.Name));
            csv.AppendLine();
        }
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"ask-{campaign.Slug}-submissions.csv");
    }

    [HttpPost("submissions/{id}/moderate")]
    [AllowUser(AuthorizationPermissionKeys.AskModerate, AuthorizationPermissionKeys.AskManage)]
    public async Task<ActionResult<AskSubmissionContract>> Moderate(string id, AskModerationStatus status, string? note = null)
    {
        var item = await askService.ModerateAsync(id, HttpContext.GetChannelContext().ChannelId, status, note);
        return item is null ? NotFound() : Ok(ContractUtils.Convert(item));
    }

    private async Task InvalidateAsync()
    {
        await crossApiService.ResetCache(CacheKeys.Ask);
        await crossApiService.PurgeCache(ApiTagCacheKeys.Ask);
    }

    private static string Csv(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}