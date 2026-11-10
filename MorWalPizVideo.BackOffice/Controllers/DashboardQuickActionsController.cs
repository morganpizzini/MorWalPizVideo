using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/v1/dashboard/quick-actions")]
[AllowUser("perm:" + AuthorizationPermissionKeys.BackofficeAccess)]
[RequireChannelScope]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class DashboardQuickActionsController(
    DashboardQuickActionsService service,
    ILogger<DashboardQuickActionsController> logger) : ControllerBase
{
    private const int MaxRequestBytes = 64 * 1024;
    private bool IsPersonalUser => !User.HasClaim(claim =>
        claim.Type.Equals("ApiKeyId", StringComparison.OrdinalIgnoreCase)) &&
        !User.Identities.Any(identity => identity.AuthenticationType?.Equals("ApiKey", StringComparison.OrdinalIgnoreCase) == true) &&
        !string.IsNullOrWhiteSpace(ImpersonationClaimsTransformation.GetEffectiveUserId(User));

    [HttpGet]
    public async Task<ActionResult<QuickActionsResponse>> Get(CancellationToken cancellationToken)
    {
        if (!IsPersonalUser) return Forbid();
        return Ok(await service.GetAsync(User, HttpContext.GetChannelContext().ChannelId, cancellationToken));
    }

    [HttpPut]
    [BlockImpersonation]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<ActionResult<QuickActionsResponse>> Put(
        [FromBody] SaveQuickActionsRequest request, CancellationToken cancellationToken)
    {
        if (!IsPersonalUser) return Forbid();
        if (Request.ContentLength > MaxRequestBytes)
            return Problem(statusCode: StatusCodes.Status413PayloadTooLarge, title: "The quick-action request is too large.");
        try
        {
            return Ok(await service.SaveAsync(User, HttpContext.GetChannelContext().ChannelId, request, cancellationToken));
        }
        catch (QuickActionsValidationException exception)
        {
            logger.LogWarning("Invalid quick-action structure for user {UserId} and channel {ChannelId}",
                ImpersonationClaimsTransformation.GetEffectiveUserId(User), HttpContext.GetChannelContext().ChannelId);
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid quick-action request.", detail: exception.Message);
        }
    }

    [HttpDelete]
    [BlockImpersonation]
    public async Task<ActionResult<QuickActionsResponse>> Delete(CancellationToken cancellationToken)
    {
        if (!IsPersonalUser) return Forbid();
        return Ok(await service.ResetAsync(User, HttpContext.GetChannelContext().ChannelId, cancellationToken));
    }
}
