using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain.Accounts;
using MorWalPizVideo.MvcHelpers.Authentication;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.ServerAPI.Services;

namespace MorWalPizVideo.ServerAPI.Controllers;

[ApiController]
[EnableCors("public-account")]
[Route("api/v1")]
public sealed class PublicAccountController(
    IPublicAccountService accountService,
    AccountMailQueue mailQueue,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("account/csrf")]
    [AllowAnonymous]
    [OutputCache(NoStore = true)]
    public ActionResult<PublicAccountCsrfContract> GetCsrf()
    {
        Response.Headers.CacheControl = "no-store";
        var hasExistingToken = Request.Cookies.TryGetValue(
            PublicAccountSecurity.CsrfCookie,
            out var existingToken) &&
            !string.IsNullOrWhiteSpace(existingToken);
        var token = hasExistingToken
            ? existingToken!
            : Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        if (!hasExistingToken)
            Response.Cookies.Append(
                PublicAccountSecurity.CsrfCookie,
                token,
                PublicAccountSecurity.CreateCookieOptions(IsSecureCookie()));
        return Ok(new PublicAccountCsrfContract { Token = token });
    }

    [HttpPost("accounts")]
    [AllowAnonymous]
    [EnableRateLimiting("public-account-register")]
    public async Task<ActionResult<PublicAccountSessionContract>> Register(
        PublicAccountRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request))
            return CsrfFailure();
        if (request.Site is not ("workshop" or "public-site"))
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["Site"] = ["Select a supported account site."] }));
        if (!mailQueue.IsAvailable)
            return StatusCode(503, new ProblemDetails { Title = "Account mail is unavailable. Please try again later." });

        try
        {
            var account = await accountService.RegisterPendingAsync(
                new PublicAccountRegistrationCommand(
                    request.Username,
                    request.Email,
                    request.Password,
                    request.DisplayName,
                    request.TermsAccepted,
                    request.PrivacyNoticeAcknowledged),
                cancellationToken);
            if (account is null)
                return ValidationProblem("The account details are not valid.");
            var requested = mailQueue.TryEnqueue(new(account.Email, request.Site, PublicAccountMailPurpose.EmailConfirmation));
            return Ok(new PublicAccountSessionContract
            {
                Authenticated = false, EmailConfirmationRequired = true,
                ConfirmationEmailStatus = requested ? "requested" : "unavailable"
            });
        }
        catch (PublicAccountConflictException)
        {
            return Conflict(new ProblemDetails { Title = "The account could not be created." });
        }
    }

    [HttpPost("account/sessions")]
    [AllowAnonymous]
    [EnableRateLimiting("public-account-login")]
    public async Task<ActionResult<PublicAccountSessionContract>> Login(
        PublicAccountLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request))
            return CsrfFailure();

        var result = await accountService.LoginForPublicClientAsync(
            new PublicAccountLoginCommand(request.Identifier, request.Password, request.RememberMe),
            cancellationToken);
        if (result is null)
            return Unauthorized(new ProblemDetails { Title = "Invalid account credentials." });
        if (result.Authentication is null)
            return Ok(new PublicAccountSessionContract { Authenticated = false, EmailConfirmationRequired = true });
        SetSessionCookie(result.Authentication.SessionToken, result.Authentication.SessionExpiresAtUtc, request.RememberMe);
        var session = ToSessionContract(result.Account);
        if (!result.Account.EmailConfirmed && request.Site is "workshop" or "public-site")
            session = session with
            {
                ConfirmationEmailStatus = mailQueue.TryEnqueue(new(result.Account.Email, request.Site,
                    PublicAccountMailPurpose.EmailConfirmation, Automatic: true)) ? "requested" : "unavailable"
            };
        return Ok(session);
    }

    [HttpGet("account/session")]
    [AllowAnonymous]
    [OutputCache(NoStore = true)]
    public async Task<ActionResult<PublicAccountSessionContract>> Session(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!Request.Cookies.TryGetValue(PublicAccountSecurity.SessionCookie, out var token))
            return Ok(new PublicAccountSessionContract { Authenticated = false });

        var session = await accountService.ValidateSessionAsync(token, cancellationToken);
        return session is null
            ? Ok(new PublicAccountSessionContract { Authenticated = false })
            : Ok(ToSessionContract(session.Account));
    }

    [HttpDelete("account/sessions/current")]
    [Authorize(AuthenticationSchemes = PublicAccountSecurity.Scheme)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request))
            return CsrfFailure();
        if (Request.Cookies.TryGetValue(PublicAccountSecurity.SessionCookie, out var token))
            await accountService.RevokeSessionAsync(token, cancellationToken);
        Response.Cookies.Delete(PublicAccountSecurity.SessionCookie, PublicAccountSecurity.CreateCookieOptions(IsSecureCookie()));
        return NoContent();
    }

    private void SetSessionCookie(string token, DateTime expiresAtUtc, bool persistent)
    {
        var options = PublicAccountSecurity.CreateCookieOptions(IsSecureCookie());
        if (persistent)
            options.Expires = new DateTimeOffset(expiresAtUtc, TimeSpan.Zero);
        Response.Cookies.Append(PublicAccountSecurity.SessionCookie, token, options);
    }

    private bool IsSecureCookie() => !environment.IsDevelopment() && !environment.IsEnvironment("Test");
    private ObjectResult CsrfFailure() => new(new ProblemDetails
    {
        Status = StatusCodes.Status403Forbidden,
        Title = "The CSRF token is invalid",
        Extensions = { ["code"] = "csrf_invalid" }
    })
    { StatusCode = StatusCodes.Status403Forbidden };

    private static PublicAccountSessionContract ToSessionContract(MorWalPizVideo.Models.Models.PublicAccount account) =>
        new() { Authenticated = true, Account = new PublicAccountSummaryContract
        {
            Id = account.Id,
            Username = account.Username,
            Email = account.Email,
            DisplayName = account.DisplayName,
            EmailConfirmed = account.EmailConfirmed,
            EmailConfirmationRequired = account.EmailConfirmationRequired,
            WorkshopCapabilityGuideSeenVersion = account.WorkshopCapabilityGuideSeenVersion
        }};
}

[ApiController]
[EnableCors("public-account")]
[Route("api/v1/profile")]
[Authorize(AuthenticationSchemes = PublicAccountSecurity.Scheme)]
public sealed class PublicProfileController(IPublicAccountService accountService) : ControllerBase
{
    [HttpGet]
    [OutputCache(NoStore = true)]
    public async Task<ActionResult<PublicAccountSummaryContract>> Get(CancellationToken cancellationToken)
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var account = accountId is null ? null : await accountService.GetAccountAsync(accountId, cancellationToken);
        return account is null ? Unauthorized() : Ok(ToContract(account));
    }

    [HttpPut]
    public async Task<ActionResult<PublicAccountSummaryContract>> Update(
        PublicAccountProfileUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request))
            return StatusCode(StatusCodes.Status403Forbidden);
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (accountId is null)
            return Unauthorized();

        try
        {
            var account = await accountService.UpdateProfileAsync(
                accountId,
                new PublicAccountProfileUpdateCommand(request.DisplayName, request.Email),
                cancellationToken);
            return account is null ? NotFound() : Ok(ToContract(account));
        }
        catch (PublicAccountConflictException)
        {
            return Conflict(new ProblemDetails { Title = "The profile could not be updated." });
        }
    }

    private static PublicAccountSummaryContract ToContract(MorWalPizVideo.Models.Models.PublicAccount account) =>
        new()
        {
            Id = account.Id,
            Username = account.Username,
            Email = account.Email,
            DisplayName = account.DisplayName,
            EmailConfirmed = account.EmailConfirmed,
            EmailConfirmationRequired = account.EmailConfirmationRequired,
            WorkshopCapabilityGuideSeenVersion = account.WorkshopCapabilityGuideSeenVersion
        };
}
