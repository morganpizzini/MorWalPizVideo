using System.Security.Claims;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public sealed record ImpersonationGrantIssueResult(
    bool Succeeded,
    string? ErrorCode,
    string? Grant,
    DateTime? ExpiresAt);

public sealed record ImpersonationSessionRedeemResult(
    bool Succeeded,
    string? ErrorCode,
    string? SessionToken,
    ImpersonationSession? Session);

public sealed record ImpersonationSessionContext(
    ImpersonationSession Session,
    UserAccessProfile TargetProfile);

public interface IImpersonationService
{
    Task<ImpersonationGrantIssueResult> IssueGrantAsync(ClaimsPrincipal principal, string targetUserId);
    Task<ImpersonationSessionRedeemResult> RedeemGrantAsync(ClaimsPrincipal principal, string grant);
    Task<ImpersonationSessionContext?> ResolveSessionAsync(string sessionToken, string actorUserId);
    Task<bool> EndSessionAsync(ClaimsPrincipal principal, string sessionToken);
}