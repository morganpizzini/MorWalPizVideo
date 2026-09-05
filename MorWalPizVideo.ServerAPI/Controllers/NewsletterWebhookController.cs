using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.ServerAPI.Controllers;

[ApiController]
[Route("api/newsletter/webhooks")]
[AllowAnonymous]
public sealed class NewsletterWebhookController(
    INewsletterRecipientRepository recipientRepository,
    INewsletterUserRepository userRepository,
    INewsletterEventRepository eventRepository,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("bounce")]
    public async Task<ActionResult> Bounce(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;
        var secret = configuration["Newsletter:WebhookSigningKey"];
        var signature = Request.Headers["X-Newsletter-Signature"].ToString();
        byte[] suppliedSignature;
        try
        {
            suppliedSignature = Convert.FromHexString(signature);
        }
        catch (FormatException)
        {
            return Unauthorized();
        }
        if (string.IsNullOrWhiteSpace(secret) || suppliedSignature.Length != 32 || !CryptographicOperations.FixedTimeEquals(
                suppliedSignature,
                HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body))))
            return Unauthorized();

        var payload = JsonSerializer.Deserialize<NewsletterBounceRequest>(body);
        if (payload is null || string.IsNullOrWhiteSpace(payload.ChannelId) || string.IsNullOrWhiteSpace(payload.ProviderMessageId))
            return BadRequest();
        var recipient = (await recipientRepository.GetItemsAsync(item => item.ChannelId == payload.ChannelId && item.ProviderMessageId == payload.ProviderMessageId)).FirstOrDefault();
        if (recipient is null) return NoContent();
        if (recipient.Status == NewsletterRecipientStatus.Bounced) return NoContent();

        var now = DateTime.UtcNow;
        await recipientRepository.UpdateItemAsync(recipient with { Status = NewsletterRecipientStatus.Bounced, BouncedAt = now, FailureReason = payload.Reason });
        var user = await userRepository.GetItemAsync(recipient.NewsletterUserId);
        if (user is not null && user.Status != NewsletterUserStatus.Suppressed)
            await userRepository.UpdateItemAsync(user with { Status = NewsletterUserStatus.Suppressed });
        var eventId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"bounce:{payload.ChannelId}:{payload.ProviderMessageId}")))[..24].ToLowerInvariant();
        var existing = await eventRepository.GetItemAsync(eventId);
        if (existing is null)
            await eventRepository.AddItemAsync(new NewsletterEvent(payload.ChannelId, recipient.NewsletterId, NewsletterEventType.Bounce, now, ProviderMessageId: payload.ProviderMessageId) { Id = eventId });
        return NoContent();
    }
}

[ApiController]
[Route("api/newsletter/webhooks")]
[AllowAnonymous]
public sealed class NewsletterDeliveredWebhookController(
    INewsletterRecipientRepository recipientRepository,
    INewsletterEventRepository eventRepository,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("delivered")]
    public async Task<ActionResult> Delivered(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;
        var signature = Request.Headers["X-Newsletter-Signature"].ToString();
        var secret = configuration["Newsletter:WebhookSigningKey"];
        byte[] suppliedSignature;
        try { suppliedSignature = Convert.FromHexString(signature); }
        catch (FormatException) { return Unauthorized(); }
        if (string.IsNullOrWhiteSpace(secret) || suppliedSignature.Length != 32 || !CryptographicOperations.FixedTimeEquals(suppliedSignature, HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body)))) return Unauthorized();
        var payload = JsonSerializer.Deserialize<NewsletterDeliveryRequest>(body);
        if (payload is null || string.IsNullOrWhiteSpace(payload.ChannelId) || string.IsNullOrWhiteSpace(payload.ProviderMessageId)) return BadRequest();
        var recipient = (await recipientRepository.GetItemsAsync(item => item.ChannelId == payload.ChannelId && item.ProviderMessageId == payload.ProviderMessageId)).FirstOrDefault();
        if (recipient is null) return NoContent();
        if (recipient.Status != NewsletterRecipientStatus.Delivered)
            await recipientRepository.UpdateItemAsync(recipient with { Status = NewsletterRecipientStatus.Delivered, DeliveredAt = DateTime.UtcNow });
        var eventId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"delivered:{payload.ChannelId}:{payload.ProviderMessageId}")))[..24].ToLowerInvariant();
        if (await eventRepository.GetItemAsync(eventId) is null)
            await eventRepository.AddItemAsync(new NewsletterEvent(payload.ChannelId, recipient.NewsletterId, NewsletterEventType.Delivered, DateTime.UtcNow, ProviderMessageId: payload.ProviderMessageId) { Id = eventId });
        return NoContent();
    }
}

public sealed record NewsletterBounceRequest(string ChannelId, string ProviderMessageId, string? Reason);
public sealed record NewsletterDeliveryRequest(string ChannelId, string ProviderMessageId);
