using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/impersonation")]
[Authorize]
public sealed class ImpersonationController(IImpersonationService impersonationService) : ControllerBase
{
    [HttpPost("grants")]
    [AllowUser(AuthorizationPermissionKeys.BackofficeImpersonate)]
    public async Task<IActionResult> IssueGrant([FromBody] IssueImpersonationGrantRequest request)
    {
        var result = await impersonationService.IssueGrantAsync(User, request.TargetUserId);
        if (result.Succeeded)
        {
            return Ok(new { grant = result.Grant, expiresAt = result.ExpiresAt });
        }

        return result.ErrorCode == "feature_disabled"
            ? NotFound()
            : Forbid();
    }

    [HttpPost("sessions")]
    [AllowUser(AuthorizationPermissionKeys.BackofficeImpersonate)]
    public async Task<IActionResult> RedeemGrant([FromBody] RedeemImpersonationGrantRequest request)
    {
        var result = await impersonationService.RedeemGrantAsync(User, request.Grant);
        if (!result.Succeeded)
        {
            return result.ErrorCode == "invalid_request"
                ? BadRequest(new { message = "A grant is required." })
                : Forbid();
        }

        var session = result.Session!;
        Response.Cookies.Append(ImpersonationCookieNames.Session, result.SessionToken!, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = session.ExpiresAt,
            MaxAge = TimeSpan.FromSeconds(Math.Clamp((session.ExpiresAt - DateTime.UtcNow).TotalSeconds, 1, 600)),
            Path = "/"
        });

        return Ok(new { expiresAt = session.ExpiresAt, targetUserId = session.TargetUserId });
    }

    [HttpDelete("sessions/current")]
    public async Task<IActionResult> EndSession()
    {
        var sessionToken = Request.Cookies[ImpersonationCookieNames.Session];
        await impersonationService.EndSessionAsync(User, sessionToken ?? string.Empty);
        Response.Cookies.Delete(ImpersonationCookieNames.Session, new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        });
        return NoContent();
    }
}

public sealed record IssueImpersonationGrantRequest(string TargetUserId);

public sealed record RedeemImpersonationGrantRequest(string Grant);