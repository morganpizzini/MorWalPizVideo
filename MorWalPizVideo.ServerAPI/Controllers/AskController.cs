using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.Security.Cryptography;
using System.Text;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.ServerAPI.Services;

namespace MorWalPizVideo.ServerAPI.Controllers;

public sealed class AskSubmissionRequest
{
    public string Text { get; set; } = string.Empty;
    public string RecaptchaToken { get; set; } = string.Empty;
    public string? Name { get; set; }
}

[AllowAnonymous]
[Route("api/ask")]
public sealed class AskController(IAskService askService, IGenericDataService dataService, IMorWalPizCache cache, IRecaptchaService recaptchaService) : ApplicationController(dataService, cache)
{
    [HttpGet("{channelName}/{campaignSlug}")]
    [OutputCache(Tags = [ApiTagCacheKeys.Ask], VaryByRouteValueNames = ["channelName", "campaignSlug"])]
    public async Task<ActionResult<AskPublicCampaignContract>> Get(string channelName, string campaignSlug)
    {
        var campaign = await askService.GetPublicAsync(channelName, campaignSlug);
        return campaign is null ? NotFound() : Ok(new AskPublicCampaignContract { ChannelName = campaign.ChannelName, Title = campaign.Title, Description = campaign.Description, Slug = campaign.Slug, Status = AskCampaignStatus.Published, MaxSubmissionLength = campaign.Policy.MaxSubmissionLength, AllowNamedSubmissions = campaign.Policy.AllowNamedSubmissions, NameRequired = campaign.Policy.NameRequired, RecaptchaRequired = campaign.Policy.RecaptchaRequired, Responses = campaign.Responses.Select(x => new AskPublicResponseContract { Content = x.Content, Author = x.Author, CreatedAt = x.CreatedAt }).ToArray(), Questions = campaign.Questions.Select(x => new AskPublicQuestionContract { Id = x.Id, Text = x.Text, ReactionCount = x.ReactionCount, Response = x.Response is null ? null : new AskPublicResponseContract { Content = x.Response.Content, Author = x.Response.Author, CreatedAt = x.Response.CreatedAt } }).ToArray() });
    }

    [HttpPost("{channelName}/{campaignSlug}/submissions")]
    public async Task<IActionResult> Submit(string channelName, string campaignSlug, AskSubmissionRequest request)
    {
        var campaign = await askService.GetPublicAsync(channelName, campaignSlug);
        if (campaign is null) return NotFound();
        if (campaign.Policy.RecaptchaRequired && !await recaptchaService.VerifyAsync(request.RecaptchaToken, HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, "ask_submit", HttpContext.RequestAborted))
            return BadRequest();
        var result = await askService.SubmitAsync(channelName, campaignSlug, request.Text, Request.Headers["Idempotency-Key"].FirstOrDefault(), request.Name);
        return result switch
        {
            AskSubmissionResult.Created => Accepted(new { status = "received" }),
            AskSubmissionResult.Duplicate => Conflict(),
            AskSubmissionResult.RateLimited => StatusCode(429),
            AskSubmissionResult.Invalid => BadRequest(),
            AskSubmissionResult.NotFound => NotFound(),
            _ => StatusCode(500)
        };
    }

    [HttpPost("{channelName}/{campaignSlug}/submissions/{submissionId}/reactions")]
    public async Task<ActionResult<AskReactionContract>> React(string channelName, string campaignSlug, string submissionId)
    {
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{HttpContext.Connection.RemoteIpAddress}|{Request.Headers.UserAgent}")));
        var result = await askService.ReactAsync(channelName, campaignSlug, submissionId, fingerprint);
        return Ok(new AskReactionContract { Accepted = result.Accepted, Count = result.Count });
    }
}