using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;

namespace MorWalPizVideo.ShootingRange.Security;

public sealed class ShootingRangeSessions(IShootingRangeRepositorySet repositories)
{
    public const string SessionClaim = "range_session";
    public const string ForcedChangeClaim = "range_password_change";
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    public async Task SignInAsync(HttpContext context, ShootingRangeUser user, CancellationToken cancellationToken)
    {
        var session = await repositories.LocalSessions.InsertAsync(new ShootingRangeLocalSession
        {
            Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            UserId = user.Id,
            SecurityVersion = user.SecurityVersion,
            ExpiresUtc = repositories.Clock.GetUtcNow().Add(Lifetime).UtcDateTime
        }, cancellationToken);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, Principal(user, session.Id), new AuthenticationProperties
        {
            ExpiresUtc = new DateTimeOffset(session.ExpiresUtc),
            AllowRefresh = false
        });
    }

    public Task RevokeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken) => repositories.ExecuteGuardedAsync(async () =>
    {
        var id = principal.FindFirstValue(SessionClaim);
        var session = id is null ? null : await repositories.LocalSessions.GetAsync(id, cancellationToken);
        if (session is not null) await repositories.LocalSessions.ReplaceAsync(session with { Revoked = true }, cancellationToken);
        return true;
    }, cancellationToken);

    public async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var cancellationToken = context.HttpContext.RequestAborted;
        var id = context.Principal?.FindFirstValue(SessionClaim);
        var session = id is null ? null : await repositories.LocalSessions.GetAsync(id, cancellationToken);
        var user = session is null ? null : await repositories.Users.GetAsync(session.UserId, cancellationToken);
        if (session is null || session.Revoked || session.ExpiresUtc <= repositories.Clock.GetUtcNow().UtcDateTime ||
            user is null || user.Status != ShootingRangeAccountStatus.Approved || user.SecurityVersion != session.SecurityVersion ||
            context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) != user.Id)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }
        var principal = Principal(user, session.Id);
        context.ShouldRenew = context.Principal.FindFirstValue(ClaimTypes.Role) != principal.FindFirstValue(ClaimTypes.Role) ||
            context.Principal.FindFirstValue(ForcedChangeClaim) != principal.FindFirstValue(ForcedChangeClaim);
        context.ReplacePrincipal(principal);
    }

    private static ClaimsPrincipal Principal(ShootingRangeUser user, string sessionId) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.IsAdmin ? "admin" : "user"), new Claim(SessionClaim, sessionId),
        new Claim(ForcedChangeClaim, user.ForcePasswordChange ? "true" : "false")
    ], CookieAuthenticationDefaults.AuthenticationScheme));
}