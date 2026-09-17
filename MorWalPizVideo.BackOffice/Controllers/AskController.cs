using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain;
using MorWalPizVideo.MvcHelpers.Utils;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

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
public sealed class AskController(IAskService askService, ILinksService linksService, ICrossApiService crossApiService, IConfiguration configuration, ITelegramService telegramService) : ControllerBase
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
        try
        {
            item = await askService.CreateAsync(item);
            await linksService.EnsureAskShortLinkAsync(item);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000)
        {
            if (!string.IsNullOrWhiteSpace(item.Id))
                await askService.DeleteAsync(item.Id, channelId);
            return Conflict("A campaign with this slug already exists in the channel.");
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(item.Id))
                await askService.DeleteAsync(item.Id, channelId);
            throw;
        }
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
        try
        {
            item = await askService.UpdateAsync(item, channelId) ?? throw new KeyNotFoundException();
            await linksService.EnsureAskShortLinkAsync(item);
        }
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
        var shareUrl = await GetShortUrlAsync(campaign);
        return Ok(new { shareUrl, qrUrl = $"/api/QRCode/ask?data={Uri.EscapeDataString(shareUrl)}" });
    }

    [HttpPost("{id}/publish-telegram")]
    [AllowUser(AuthorizationPermissionKeys.AskModerate, AuthorizationPermissionKeys.AskManage)]
    public async Task<ActionResult> PublishTelegram(string id)
    {
        var campaign = await askService.GetByIdAsync(id, HttpContext.GetChannelContext().ChannelId);
        if (campaign is null) return NotFound();

        var url = await GetShortUrlAsync(campaign);
        var message = campaign.Title;
        try
        {
            var providerError = await telegramService.CreatePostWithUrl(url, message);
            return string.IsNullOrEmpty(providerError)
                ? Ok(new { url, message })
                : StatusCode(StatusCodes.Status502BadGateway, new { error = "telegram_publish_failed" });
        }
        catch (SocialProviderNotConfiguredException exception)
        {
            return Conflict(new { code = "social_provider_not_configured", provider = exception.Provider, message = exception.Message });
        }
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
        await crossApiService.ResetCache(CacheKeys.ShortLinks);
        await crossApiService.PurgeCache(ApiTagCacheKeys.Ask);
    }

    private async Task<string> GetShortUrlAsync(AskCampaign campaign)
    {
        var link = await linksService.EnsureAskShortLinkAsync(campaign);
        var baseUrl = (configuration["ShortLinks:PublicBaseUrl"] ?? "https://shorts.morwalpiz.com").TrimEnd('/');
        return $"{baseUrl}/{Uri.EscapeDataString(link.Code)}";
    }

    private static string Csv(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}