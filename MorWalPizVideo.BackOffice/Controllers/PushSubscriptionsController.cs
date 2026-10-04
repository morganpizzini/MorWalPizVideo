using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Domain.Push;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/push/subscriptions")]
[Authorize]
public sealed class PushSubscriptionsController(IPushSubscriptionService service) : ControllerBase
{
    [HttpPost("register")]
    [AllowUser(AuthorizationPermissionKeys.BackofficeAccess)]
    public async Task<ActionResult<PushSubscriptionStateContract>> Register(PushSubscribeRequest request, CancellationToken cancellationToken)
    {
        request.ApplicationKey = "backoffice";
        var result = await service.SubscribeAsync(new(request.Endpoint, request.Keys.P256dh, request.Keys.Auth,
            request.ChannelIds, "backoffice", request.Language, request.Credential), cancellationToken);
        return result.Outcome switch
        {
            PushSubscriptionOutcome.Created or PushSubscriptionOutcome.Updated => Ok(new PushSubscriptionStateContract { ChannelIds = result.ChannelIds, Credential = result.Credential }),
            PushSubscriptionOutcome.Forbidden => Forbid(),
            _ => BadRequest("Subscription is invalid.")
        };
    }

    [HttpPost("status")]
    [AllowUser(AuthorizationPermissionKeys.BackofficeAccess)]
    public async Task<ActionResult<PushSubscriptionStateContract>> Status(PushSubscriptionCredentialRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetSettingsAsync(request.Endpoint, request.Credential, cancellationToken);
        return result.Outcome == PushSubscriptionOutcome.Updated
            ? Ok(new PushSubscriptionStateContract { ChannelIds = result.ChannelIds })
            : Unauthorized();
    }

    [HttpPost("revoke")]
    [AllowUser(AuthorizationPermissionKeys.BackofficeAccess)]
    public async Task<IActionResult> Revoke(PushSubscriptionCredentialRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RevokeAsync(request.Endpoint, request.Credential, cancellationToken);
        return result.Outcome == PushSubscriptionOutcome.Revoked ? NoContent() : Unauthorized();
    }
}
