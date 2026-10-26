using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain.Accounts;
using MorWalPizVideo.ServerAPI.Security;

namespace MorWalPizVideo.ServerAPI.Controllers;

[ApiController]
[EnableCors("public-account")]
[Route("api/v1")]
public sealed class PublicAccountController(
    IPublicAccountService accountService,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("account/csrf")]
    [AllowAnonymous]
    public ActionResult<PublicAccountCsrfContract> GetCsrf()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Response.Cookies.Append(
            PublicAccountSecurity.CsrfCookie,
            token,
            PublicAccountSecurity.CreateCookieOptions(IsSecureCookie()));
        return Ok(new PublicAccountCsrfContract { Token = token });
    }

    [HttpPost("accounts")]
    [AllowAnonymous]
    [EnableRateLimiting("public-account-auth")]
    public async Task<ActionResult<PublicAccountSessionContract>> Register(
        PublicAccountRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request))
            return StatusCode(StatusCodes.Status403Forbidden);

        try
        {
            var result = await accountService.RegisterAsync(
                new PublicAccountRegistrationCommand(request.Username, request.Email, request.Password, request.DisplayName),
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
    [EnableRateLimiting("public-account-auth")]
    public async Task<ActionResult<PublicAccountSessionContract>> Login(
        PublicAccountLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request))
            return StatusCode(StatusCodes.Status403Forbidden);

        var result = await accountService.LoginAsync(
            new PublicAccountLoginCommand(request.Identifier, request.Password), cancellationToken);
        if (result is null)
            return Unauthorized(new ProblemDetails { Title = "Invalid account credentials." });
        SetSessionCookie(result.SessionToken);
        return Ok(ToSessionContract(result.Account));
    }

    [HttpGet("account/session")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicAccountSessionContract>> Session(CancellationToken cancellationToken)
    {
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
            return StatusCode(StatusCodes.Status403Forbidden);
        if (Request.Cookies.TryGetValue(PublicAccountSecurity.SessionCookie, out var token))
            await accountService.RevokeSessionAsync(token, cancellationToken);
        Response.Cookies.Delete(PublicAccountSecurity.SessionCookie, PublicAccountSecurity.CreateCookieOptions(IsSecureCookie()));
        return NoContent();
    }

    private void SetSessionCookie(string token) => Response.Cookies.Append(
        PublicAccountSecurity.SessionCookie,
        token,
        PublicAccountSecurity.CreateCookieOptions(IsSecureCookie()));

    private bool IsSecureCookie() => !environment.IsDevelopment() && !environment.IsEnvironment("Test");

    private static PublicAccountSessionContract ToSessionContract(MorWalPizVideo.Models.Models.PublicAccount account) =>
        new() { Authenticated = true, Account = new PublicAccountSummaryContract
        {
            Id = account.Id,
            Username = account.Username,
            Email = account.Email,
            DisplayName = account.DisplayName
        }};
}

[ApiController]
[EnableCors("public-account")]
[Route("api/v1/profile")]
[Authorize(AuthenticationSchemes = PublicAccountSecurity.Scheme)]
public sealed class PublicProfileController(IPublicAccountService accountService) : ControllerBase
{
    [HttpGet]
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
        new() { Id = account.Id, Username = account.Username, Email = account.Email, DisplayName = account.DisplayName };
}
