using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain.Accounts;
using MorWalPizVideo.MvcHelpers.Authentication;
using FreezerAnalysis.Api.Security;

namespace FreezerAnalysis.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class FreezerAccountController(
    IPublicAccountService accountService,
    IWebHostEnvironment environment) : ControllerBase
{
    private const string SessionCookie = "freezer_account_session";
    private const string CsrfCookie = "freezer_account_csrf";

    [HttpGet("account/csrf")]
    [AllowAnonymous]
    public ActionResult<PublicAccountCsrfContract> GetCsrf()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Response.Cookies.Append(CsrfCookie, token, PublicAccountSecurity.CreateCookieOptions(IsSecureCookie()));
        return Ok(new PublicAccountCsrfContract { Token = token });
    }

    [HttpPost("accounts")]
    [AllowAnonymous]
    [EnableRateLimiting("freezer-account-auth")]
    public async Task<ActionResult<PublicAccountSessionContract>> Register(
        PublicAccountRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request, CsrfCookie))
            return StatusCode(StatusCodes.Status403Forbidden);

        try
        {
            var result = await accountService.RegisterAsync(
                new PublicAccountRegistrationCommand(
                    request.Username,
                    request.Email,
                    request.Password,
                    request.DisplayName,
                    request.TermsAccepted,
                    request.PrivacyNoticeAcknowledged),
                cancellationToken);
            if (result is null)
                return ValidationProblem("The account details are not valid.");
            SetSessionCookie(result.SessionToken);
            return Ok(ToSessionContract(result.Account));
        }
        catch (PublicAccountConflictException)
        {
            return Conflict(new ProblemDetails { Title = "The account could not be created." });
        }
    }

    [HttpPost("account/sessions")]
    [AllowAnonymous]
    [EnableRateLimiting("freezer-account-auth")]
    public async Task<ActionResult<PublicAccountSessionContract>> Login(
        PublicAccountLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request, CsrfCookie))
            return StatusCode(StatusCodes.Status403Forbidden);
        var result = await accountService.LoginAsync(
            new PublicAccountLoginCommand(request.Identifier, request.Password),
            cancellationToken);
        if (result is null)
            return Unauthorized(new ProblemDetails { Title = "Invalid account credentials." });
        SetSessionCookie(result.SessionToken);
        return Ok(ToSessionContract(result.Account));
    }

    [HttpGet("account/session")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicAccountSessionContract>> Session(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(SessionCookie, out var token))
            return Ok(new PublicAccountSessionContract { Authenticated = false });

        var session = await accountService.ValidateSessionAsync(token, cancellationToken);
        return session is null
            ? Ok(new PublicAccountSessionContract { Authenticated = false })
            : Ok(ToSessionContract(session.Account));
    }

    [HttpDelete("account/sessions/current")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request, CsrfCookie))
            return StatusCode(StatusCodes.Status403Forbidden);
        if (Request.Cookies.TryGetValue(SessionCookie, out var token))
            await accountService.RevokeSessionAsync(token, cancellationToken);
        Response.Cookies.Delete(SessionCookie, PublicAccountSecurity.CreateCookieOptions(IsSecureCookie()));
        return NoContent();
    }

    private void SetSessionCookie(string token) => Response.Cookies.Append(
        SessionCookie,
        token,
        PublicAccountSecurity.CreateCookieOptions(IsSecureCookie()));

    private bool IsSecureCookie() => !environment.IsDevelopment() && !environment.IsEnvironment("Test");

    private static PublicAccountSessionContract ToSessionContract(MorWalPizVideo.Models.Models.PublicAccount account) =>
        new()
        {
            Authenticated = true,
            Account = new PublicAccountSummaryContract
            {
                Id = account.Id,
                Username = account.Username,
                Email = account.Email,
                DisplayName = account.DisplayName
            }
        };

}
