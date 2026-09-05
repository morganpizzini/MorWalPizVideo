using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MorWalPiz.Contracts;
using MorWalPizVideo.Domain;
using MorWalPizVideo.ServerAPI.Services;

namespace MorWalPizVideo.ServerAPI.Controllers;

[ApiController]
[Route("api/newsletter")]
[AllowAnonymous]
[EnableRateLimiting("newsletter-public")]
public sealed class NewsletterController(INewsletterService newsletterService, IRecaptchaService recaptchaService, IConfiguration configuration) : ControllerBase
{
    [HttpPost("subscribe")]
    public async Task<ActionResult<NewsletterSubscribeResponse>> Subscribe(NewsletterSubscribeRequest request, CancellationToken cancellationToken)
    {
        var remoteAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        if (!await recaptchaService.VerifyAsync(request.RecaptchaToken, remoteAddress, "newsletterSubscribe", cancellationToken))
            return BadRequest(new { message = "Unable to process subscription." });

        await newsletterService.SubscribeAsync(request.ChannelId, request.Email, request.Language, cancellationToken);
        return Ok(new NewsletterSubscribeResponse("If the address can be subscribed, a confirmation message will be sent."));
    }

    [HttpPost("confirm")]
    public async Task<ActionResult<NewsletterConfirmationResponse>> Confirm(NewsletterTokenRequest request, CancellationToken cancellationToken)
    {
        var unsubscribeToken = await newsletterService.ConfirmAsync(request.ChannelId, request.Token, cancellationToken);
        if (unsubscribeToken is null) return BadRequest();
        var baseUrl = configuration["Newsletter:PublicBaseUrl"]?.TrimEnd('/') ?? string.Empty;
        var link = $"{baseUrl}/newsletter?channelId={Uri.EscapeDataString(request.ChannelId)}&action=unsubscribe&token={Uri.EscapeDataString(unsubscribeToken)}";
        return Ok(new NewsletterConfirmationResponse(unsubscribeToken, link));
    }

    [HttpPost("unsubscribe")]
    public async Task<ActionResult> Unsubscribe(NewsletterTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await newsletterService.UnsubscribeAsync(request.ChannelId, request.Token, cancellationToken);
        return result switch
        {
            NewsletterUnsubscribeResult.Valid => NoContent(),
            NewsletterUnsubscribeResult.Expired => StatusCode(StatusCodes.Status410Gone),
            NewsletterUnsubscribeResult.Consumed => Conflict(),
            _ => BadRequest()
        };
    }
}