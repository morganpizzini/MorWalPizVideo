using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain.Push;

namespace MorWalPizVideo.ServerAPI.Controllers;

/// <summary>
/// Anonymous, per-channel Web Push subscription management for the public applications.
/// No identity is required or created: the browser keeps a 256-bit credential that authorises later changes.
/// </summary>
[ApiController]
[Route("api/push/subscriptions")]
[AllowAnonymous]
[EnableRateLimiting("push-public")]
public sealed class PushSubscriptionsController(
    IPushSubscriptionService pushSubscriptionService,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>VAPID application server public key required by <c>PushManager.subscribe</c>.</summary>
    [HttpGet("/api/push/subscriptions/public-key")]
    public ActionResult<PushPublicKeyContract> GetPublicKey()
    {
        var publicKey = configuration["WebPush:PublicKey"];
        return string.IsNullOrWhiteSpace(publicKey)
            ? StatusCode(StatusCodes.Status503ServiceUnavailable)
            : Ok(new PushPublicKeyContract { PublicKey = publicKey });
    }

    /// <summary>Creates or re-targets an anonymous subscription. The credential is returned only on creation.</summary>
    [HttpPost]
    public async Task<ActionResult<PushSubscriptionStateContract>> Subscribe(PushSubscribeRequest request, CancellationToken cancellationToken)
    {
        var result = await pushSubscriptionService.SubscribeAsync(new PushSubscriptionRequest(
            request.Endpoint,
            request.Keys.P256dh,
            request.Keys.Auth,
            request.ChannelIds,
            request.ApplicationKey,
            request.Language,
            request.Credential), cancellationToken);

        return result.Outcome switch
        {
            PushSubscriptionOutcome.Created => Ok(ToContract(result)),
            PushSubscriptionOutcome.Updated => Ok(ToContract(result)),
            PushSubscriptionOutcome.Forbidden => Forbid(),
            _ => BadRequest(new { message = "The subscription payload is not valid." })
        };
    }

    /// <summary>Reads the current per-channel preferences for a credential-holding browser.</summary>
    [HttpPost("settings")]
    public async Task<ActionResult<PushSubscriptionStateContract>> GetSettings(PushSubscriptionCredentialRequest request, CancellationToken cancellationToken)
    {
        var result = await pushSubscriptionService.GetSettingsAsync(request.Endpoint, request.Credential, cancellationToken);
        return result.Outcome == PushSubscriptionOutcome.NotFound ? NotFound() : Ok(ToContract(result));
    }

    /// <summary>Revokes an anonymous subscription.</summary>
    [HttpPost("revoke")]
    public async Task<ActionResult> Revoke(PushSubscriptionCredentialRequest request, CancellationToken cancellationToken)
    {
        var result = await pushSubscriptionService.RevokeAsync(request.Endpoint, request.Credential, cancellationToken);
        return result.Outcome == PushSubscriptionOutcome.NotFound ? NotFound() : NoContent();
    }

    private static PushSubscriptionStateContract ToContract(PushSubscriptionResult result) =>
        new() { ChannelIds = result.ChannelIds, Credential = result.Credential };
}
