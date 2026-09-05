using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/newsletters")]
[Authorize]
[RequireChannelScope]
public sealed class NewslettersController(
    INewsletterRepository newsletterRepository,
    INewsletterService newsletterService,
    INewsletterTemplateRepository templateRepository,
    INewsletterUserRepository userRepository,
    INewsletterRecipientRepository recipientRepository,
    INewsletterDispatchService dispatchService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NewsletterContract>>> GetAll()
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var items = await newsletterRepository.GetItemsAsync(item => item.ChannelId == channelId);
        return Ok(items.Select(ToContract).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<NewsletterContract>> Create(NewsletterCreateRequest request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        if (string.IsNullOrWhiteSpace(request.Name) || request.Sections.Count == 0)
            return BadRequest("Name and at least one section are required.");
        if (string.IsNullOrWhiteSpace(request.TemplateId) || !(await templateRepository.GetItemsAsync(template => template.Id == request.TemplateId && template.ChannelId == channelId && template.Version == request.TemplateVersion)).Any())
            return BadRequest("Template does not belong to the selected channel or version.");
        var item = await newsletterService.CreateAsync(new Newsletter(
            channelId, request.Name.Trim(), request.SubjectIt.Trim(), request.SubjectEng.Trim(), request.TemplateId,
            request.TemplateVersion, request.Sections.Select(section => new NewsletterSection(section.Type, section.Title, section.Body, section.ImageUrl, section.ShortLinkCode)).ToArray()));
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ToContract(item));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<NewsletterContract>> Update(string id, NewsletterCreateRequest request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var current = (await newsletterRepository.GetItemsAsync(item => item.Id == id && item.ChannelId == channelId)).FirstOrDefault();
        var templateExists = !string.IsNullOrWhiteSpace(request.TemplateId) && (await templateRepository.GetItemsAsync(template => template.Id == request.TemplateId && template.ChannelId == channelId && template.Version == request.TemplateVersion)).Any();
        if (current is null) return NotFound();
        if (current.State is NewsletterState.Approved or NewsletterState.Sending or NewsletterState.Sent || string.IsNullOrWhiteSpace(request.Name) || request.Sections.Count == 0 || !templateExists)
            return Conflict("Only draft content with a channel-owned template can be edited.");
        var updated = current with { Name = request.Name.Trim(), SubjectIt = request.SubjectIt.Trim(), SubjectEng = request.SubjectEng.Trim(), TemplateId = request.TemplateId, TemplateVersion = request.TemplateVersion, Sections = request.Sections.Select(section => new NewsletterSection(section.Type, section.Title, section.Body, section.ImageUrl, section.ShortLinkCode)).ToArray() };
        await newsletterRepository.UpdateItemAsync(updated);
        return Ok(ToContract(updated));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NewsletterContract>> Get(string id)
    {
        var item = (await newsletterRepository.GetItemsAsync(newsletter => newsletter.Id == id && newsletter.ChannelId == HttpContext.GetChannelContext().ChannelId)).FirstOrDefault();
        return item is null ? NotFound() : Ok(ToContract(item));
    }

    [HttpPost("{id}/state")]
    public async Task<ActionResult<NewsletterContract>> ChangeState(string id, NewsletterStateRequest request)
    {
        if (!Enum.TryParse<NewsletterState>(request.State, true, out var state)) return BadRequest("State is invalid.");
        var item = await newsletterService.TransitionAsync(HttpContext.GetChannelContext().ChannelId, id, state);
        return item is null ? Conflict("Invalid newsletter state transition.") : Ok(ToContract(item));
    }

    [HttpPost("{id}/send")]
    public async Task<ActionResult> Send(string id, CancellationToken cancellationToken)
    {
        var queued = await dispatchService.QueueAsync(HttpContext.GetChannelContext().ChannelId, id, cancellationToken);
        return queued ? Accepted() : Conflict("Newsletter must be approved and Hangfire must be enabled before sending.");
    }

    [HttpGet("{id}/preview")]
    public async Task<ActionResult<object>> Preview(string id)
    {
        var item = (await newsletterRepository.GetItemsAsync(newsletter => newsletter.Id == id && newsletter.ChannelId == HttpContext.GetChannelContext().ChannelId)).FirstOrDefault();
        if (item is null) return NotFound();
        var html = string.Join("\n", item.Sections.Select(section => section.Type switch
        {
            "heading" => $"<h2>{System.Net.WebUtility.HtmlEncode(section.Title)}</h2>",
            "text" => $"<p>{System.Net.WebUtility.HtmlEncode(section.Body)}</p>",
            "image" when !string.IsNullOrWhiteSpace(section.ImageUrl) => $"<img src=\"{System.Net.WebUtility.HtmlEncode(section.ImageUrl)}\" alt=\"\" />",
            _ => string.Empty
        }));
        return Ok(new { item.SubjectIt, item.SubjectEng, html, links = item.Sections.Where(section => !string.IsNullOrWhiteSpace(section.ShortLinkCode)).Select(section => new { code = section.ShortLinkCode, channelId = item.ChannelId, newsletterId = item.Id }).ToArray() });
    }

    [HttpGet("{id}/subscribers")]
    public async Task<ActionResult> Subscribers(string id)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var exists = (await newsletterRepository.GetItemsAsync(item => item.Id == id && item.ChannelId == channelId)).Any();
        if (!exists) return NotFound();
        var users = await userRepository.GetItemsAsync(item => item.ChannelId == channelId);
        return Ok(users.Select(item => new { item.Id, item.Language, Status = item.Status.ToString() }));
    }

    [HttpGet("{id}/stats")]
    public async Task<ActionResult> Stats(string id)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        var exists = (await newsletterRepository.GetItemsAsync(item => item.Id == id && item.ChannelId == channelId)).Any();
        if (!exists) return NotFound();
        var recipients = await recipientRepository.GetItemsAsync(item => item.ChannelId == channelId && item.NewsletterId == id);
        return Ok(new { total = recipients.Count, sent = recipients.Count(item => item.Status == NewsletterRecipientStatus.Sent), delivered = recipients.Count(item => item.Status == NewsletterRecipientStatus.Delivered), failed = recipients.Count(item => item.Status == NewsletterRecipientStatus.Failed), bounced = recipients.Count(item => item.Status == NewsletterRecipientStatus.Bounced) });
    }

    [HttpGet("templates")]
    public async Task<ActionResult> Templates()
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        return Ok(await templateRepository.GetItemsAsync(item => item.ChannelId == channelId));
    }

    [HttpPost("templates")]
    public async Task<ActionResult> CreateTemplate(NewsletterTemplateCreateRequest request)
    {
        var channelId = HttpContext.GetChannelContext().ChannelId;
        if (string.IsNullOrWhiteSpace(request.Name) || request.Sections.Count == 0) return BadRequest();
        var template = await templateRepository.AddItemAsync(new NewsletterTemplate(channelId, request.Name.Trim(), request.Version, request.Sections));
        return Created($"api/newsletters/templates/{template.Id}", template);
    }

    private static NewsletterContract ToContract(Newsletter item) =>
        new(item.Id, item.ChannelId, item.Name, item.SubjectIt, item.SubjectEng, item.TemplateId, item.TemplateVersion, item.State.ToString());
}

public sealed record NewsletterTemplateCreateRequest(string Name, int Version, IReadOnlyList<NewsletterSection> Sections);