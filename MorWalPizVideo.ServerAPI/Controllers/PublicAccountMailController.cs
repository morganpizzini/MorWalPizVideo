using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain.Accounts;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.MvcHelpers.Authentication;
using MorWalPizVideo.ServerAPI.Services;

namespace MorWalPizVideo.ServerAPI.Controllers;

[ApiController]
[AllowAnonymous]
[EnableCors("public-account")]
[Route("api/v1/account")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(4096)]
public sealed class PublicAccountMailController(AccountMailQueue queue, PublicAccountMailService service) : ControllerBase
{
    [HttpPost("password-recovery-requests")]
    [EnableRateLimiting("account-mail-request")]
    public IActionResult RequestReset(PublicAccountMailRequest request) => RequestMail(request, PublicAccountMailPurpose.PasswordReset);

    [HttpPost("email-confirmation-requests")]
    [EnableRateLimiting("account-confirmation-request")]
    public IActionResult RequestConfirmation(PublicAccountMailRequest request) => RequestMail(request, PublicAccountMailPurpose.EmailConfirmation);

    [HttpPost("password-resets")]
    [EnableRateLimiting("account-mail-redeem")]
    public Task<IActionResult> Reset(PublicAccountPasswordResetRequest request, CancellationToken cancellationToken) =>
        Consume(request, PublicAccountMailPurpose.PasswordReset, request.Password, cancellationToken);

    [HttpPost("email-confirmations")]
    [EnableRateLimiting("account-mail-redeem")]
    public Task<IActionResult> Confirm(PublicAccountTokenRequest request, CancellationToken cancellationToken) =>
        Consume(request, PublicAccountMailPurpose.EmailConfirmation, null, cancellationToken);

    private IActionResult RequestMail(PublicAccountMailRequest request, PublicAccountMailPurpose purpose)
    {
        Response.Headers.CacheControl = "no-store";
        if (!PublicAccountSecurity.HasValidCsrf(Request)) return StatusCode(403, new ProblemDetails { Title = "The CSRF token is invalid." });
        return queue.TryEnqueue(new(request.Email, request.Site, purpose))
            ? Accepted(new PublicAccountMailReceipt(true))
            : StatusCode(503, new ProblemDetails { Title = "Account mail is unavailable. Please try again later." });
    }

    private async Task<IActionResult> Consume(PublicAccountTokenRequest request, PublicAccountMailPurpose purpose,
        string? password, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!PublicAccountSecurity.HasValidCsrf(Request)) return StatusCode(403, new ProblemDetails { Title = "The CSRF token is invalid." });
        return await service.ConsumeAsync(request.AccountId, request.Token, purpose, password, cancellationToken)
            ? NoContent()
            : BadRequest(new ProblemDetails { Title = "This link is invalid or has expired. Request a new email." });
    }
}
