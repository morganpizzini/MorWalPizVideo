using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Server.Utils;

namespace MorWalPizVideo.BackOffice.Services;

public sealed class ImpersonationService(
    IUserAccessResolver userAccessResolver,
    IImpersonationGrantRepository grantRepository,
    IImpersonationSessionRepository sessionRepository,
    IImpersonationAuditRepository auditRepository,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<ImpersonationService> logger) : IImpersonationService
{
    public static readonly TimeSpan MaximumSessionLifetime = TimeSpan.FromMinutes(10);

    public async Task<ImpersonationGrantIssueResult> IssueGrantAsync(
        ClaimsPrincipal principal,
        string targetUserId)
    {
        var actorUserId = GetActorUserId(principal);
        if (!IsEnabled())
        {
            await AuditAsync("grant_denied", actorUserId, targetUserId, null, "feature_disabled");
            return DeniedGrant("feature_disabled");
        }

        if (IsApiKeyPrincipal(principal) || principal.HasClaim("impersonation", "true"))
        {
            await AuditAsync("grant_denied", actorUserId, targetUserId, null, "operator_not_allowed");
            return DeniedGrant("operator_not_allowed");
        }

        var actorProfile = await userAccessResolver.ResolveAsync(actorUserId);
        if (actorProfile is null)
        {
            await AuditAsync("grant_denied", actorUserId, targetUserId, null, "operator_inactive_or_missing");
            return DeniedGrant("operator_inactive_or_missing");
        }

        if (!actorProfile.EffectivePermissions.Contains(
                AuthorizationPermissionKeys.BackofficeImpersonate,
                StringComparer.OrdinalIgnoreCase))
        {
            await AuditAsync("grant_denied", actorUserId, targetUserId, null, "operator_permission_missing");
            return DeniedGrant("operator_permission_missing");
        }

        var targetProfile = await userAccessResolver.ResolveAsync(targetUserId);
        var targetError = GetTargetRejection(targetProfile);
        if (targetError is not null)
        {
            await AuditAsync("grant_denied", actorUserId, targetUserId, null, targetError);
            return DeniedGrant(targetError);
        }

        var now = UtcNow();
        var rawGrant = CreateOpaqueToken();
        var grant = await grantRepository.AddItemAsync(new ImpersonationGrant
        {
            GrantHash = HashToken(rawGrant),
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            IssuedAt = now,
            ExpiresAt = now.Add(MaximumSessionLifetime)
        });

        await AuditAsync("grant_issued", actorUserId, targetUserId, grant.Id, null, grant.ExpiresAt);
        return new ImpersonationGrantIssueResult(true, null, rawGrant, grant.ExpiresAt);
    }

    public async Task<ImpersonationSessionRedeemResult> RedeemGrantAsync(
        ClaimsPrincipal principal,
        string grant)
    {
        var actorUserId = GetActorUserId(principal);
        if (!IsEnabled())
        {
            await AuditAsync("redeem_denied", actorUserId, null, null, "feature_disabled");
            return DeniedSession("feature_disabled");
        }

        if (IsApiKeyPrincipal(principal) || principal.HasClaim("impersonation", "true"))
        {
            await AuditAsync("redeem_denied", actorUserId, null, null, "operator_not_allowed");
            return DeniedSession("operator_not_allowed");
        }

        if (string.IsNullOrWhiteSpace(actorUserId) || string.IsNullOrWhiteSpace(grant))
        {
            await AuditAsync("redeem_denied", actorUserId, null, null, "invalid_request");
            return DeniedSession("invalid_request");
        }

        var storedGrant = await grantRepository.GetByHashAsync(HashToken(grant));
        if (storedGrant is null)
        {
            await AuditAsync("redeem_denied", actorUserId, null, null, "grant_not_found");
            return DeniedSession("grant_not_found");
        }

        if (!string.Equals(storedGrant.ActorUserId, actorUserId, StringComparison.Ordinal))
        {
            await AuditAsync("redeem_denied", actorUserId, storedGrant.TargetUserId, storedGrant.Id, "actor_mismatch");
            return DeniedSession("actor_mismatch");
        }

        var now = UtcNow();
        if (storedGrant.ExpiresAt <= now)
        {
            await AuditAsync("grant_expired", actorUserId, storedGrant.TargetUserId, storedGrant.Id, "grant_expired");
            return DeniedSession("grant_expired");
        }

        if (storedGrant.RedeemedAt is not null)
        {
            await AuditAsync("grant_replay", actorUserId, storedGrant.TargetUserId, storedGrant.Id, "grant_already_redeemed");
            return DeniedSession("grant_already_redeemed");
        }

        var actorProfile = await userAccessResolver.ResolveAsync(actorUserId);
        var targetProfile = await userAccessResolver.ResolveAsync(storedGrant.TargetUserId);
        if (actorProfile is null || !actorProfile.EffectivePermissions.Contains(
                AuthorizationPermissionKeys.BackofficeImpersonate,
                StringComparer.OrdinalIgnoreCase))
        {
            await AuditAsync("redeem_denied", actorUserId, storedGrant.TargetUserId, storedGrant.Id, "operator_no_longer_allowed");
            return DeniedSession("operator_no_longer_allowed");
        }

        var targetError = GetTargetRejection(targetProfile);
        if (targetError is not null)
        {
            await AuditAsync("redeem_denied", actorUserId, storedGrant.TargetUserId, storedGrant.Id, targetError);
            return DeniedSession(targetError);
        }

        var sessionId = CreateSessionId();
        var redeemedGrant = await grantRepository.RedeemAsync(HashToken(grant), sessionId, now);
        if (redeemedGrant is null)
        {
            var currentGrant = await grantRepository.GetByHashAsync(HashToken(grant));
            var replay = currentGrant?.RedeemedAt is not null;
            await AuditAsync(
                replay ? "grant_replay" : "redeem_denied",
                actorUserId,
                currentGrant?.TargetUserId,
                currentGrant?.Id,
                replay ? "grant_already_redeemed" : "grant_expired_or_unavailable");
            return DeniedSession(replay ? "grant_already_redeemed" : "grant_expired_or_unavailable");
        }

        var rawSessionToken = CreateOpaqueToken();
        var session = await sessionRepository.AddItemAsync(new ImpersonationSession
        {
            Id = sessionId,
            SessionHash = HashToken(rawSessionToken),
            ActorUserId = actorUserId,
            TargetUserId = storedGrant.TargetUserId,
            CreatedAt = now,
            ExpiresAt = Min(now.Add(MaximumSessionLifetime), redeemedGrant.ExpiresAt)
        });

        await AuditAsync("session_redeemed", actorUserId, session.TargetUserId, session.Id, null, session.ExpiresAt);
        return new ImpersonationSessionRedeemResult(true, null, rawSessionToken, session);
    }

    public async Task<ImpersonationSessionContext?> ResolveSessionAsync(
        string sessionToken,
        string actorUserId)
    {
        if (!IsEnabled() || string.IsNullOrWhiteSpace(sessionToken) || string.IsNullOrWhiteSpace(actorUserId))
        {
            return null;
        }

        var session = await sessionRepository.GetByHashAsync(HashToken(sessionToken));
        if (session is null || session.EndedAt is not null)
        {
            return null;
        }

        if (!string.Equals(session.ActorUserId, actorUserId, StringComparison.Ordinal))
        {
            await AuditAsync("session_denied", actorUserId, session.TargetUserId, session.Id, "actor_mismatch");
            return null;
        }

        var now = UtcNow();
        if (session.ExpiresAt <= now)
        {
            if (await sessionRepository.EndAsync(HashToken(sessionToken), now, "expired"))
            {
                await AuditAsync("session_expired", session.ActorUserId, session.TargetUserId, session.Id, "session_expired");
            }

            return null;
        }

        if (await userAccessResolver.ResolveAsync(actorUserId) is null)
        {
            await sessionRepository.EndAsync(HashToken(sessionToken), now, "actor_inactive");
            await AuditAsync("session_denied", actorUserId, session.TargetUserId, session.Id, "actor_inactive");
            return null;
        }

        var targetProfile = await userAccessResolver.ResolveAsync(session.TargetUserId);
        var targetError = GetTargetRejection(targetProfile);
        if (targetError is not null)
        {
            await sessionRepository.EndAsync(HashToken(sessionToken), now, targetError);
            await AuditAsync("session_denied", actorUserId, session.TargetUserId, session.Id, targetError);
            return null;
        }

        return new ImpersonationSessionContext(session, targetProfile!);
    }

    public async Task<bool> EndSessionAsync(ClaimsPrincipal principal, string sessionToken)
    {
        var actorUserId = GetActorUserId(principal);
        var session = string.IsNullOrWhiteSpace(sessionToken)
            ? null
            : await sessionRepository.GetByHashAsync(HashToken(sessionToken));
        if (session is null || !string.Equals(session.ActorUserId, actorUserId, StringComparison.Ordinal))
        {
            await AuditAsync("session_end_denied", actorUserId, session?.TargetUserId, session?.Id, "session_not_owned");
            return false;
        }

        var ended = await sessionRepository.EndAsync(HashToken(sessionToken), UtcNow(), "operator_requested");
        if (ended)
        {
            await AuditAsync("session_ended", actorUserId, session.TargetUserId, session.Id, "operator_requested");
        }

        return ended;
    }

    private bool IsEnabled() => configuration.IsFeatureEnabled(MyFeatureFlags.EnableImpersonation);

    private static string? GetTargetRejection(UserAccessProfile? targetProfile)
    {
        if (targetProfile is null)
        {
            return "target_inactive_or_missing";
        }

        if (targetProfile.User.IsSecurityAccount)
        {
            return "target_security_account";
        }

        if (targetProfile.GroupCodes.Contains(AuthorizationGroupCodes.Admin, StringComparer.OrdinalIgnoreCase))
        {
            return "target_admin_account";
        }

        if (targetProfile.EffectivePermissions.Contains(
                AuthorizationPermissionKeys.BackofficeManageAll,
                StringComparer.OrdinalIgnoreCase))
        {
            return "target_manageall_account";
        }

        return targetProfile.EffectivePermissions.Contains(
                AuthorizationPermissionKeys.BackofficeAccess,
                StringComparer.OrdinalIgnoreCase)
            ? null
            : "target_missing_backoffice_access";
    }

    private async Task AuditAsync(
        string eventType,
        string? actorUserId,
        string? targetUserId,
        string? sessionId,
        string? reason,
        DateTime? occurredAt = null)
    {
        try
        {
            await auditRepository.AddItemAsync(new ImpersonationAuditEvent
            {
                EventType = eventType,
                ActorUserId = actorUserId,
                TargetUserId = targetUserId,
                SessionId = sessionId,
                Reason = reason,
                OccurredAt = occurredAt ?? UtcNow()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist impersonation audit event {EventType}", eventType);
        }
    }

    private static string GetActorUserId(ClaimsPrincipal principal)
        => principal.FindFirstValue("actor_user_id") ??
           principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private static bool IsApiKeyPrincipal(ClaimsPrincipal principal)
        => principal.HasClaim(claim => claim.Type.Equals("ApiKeyId", StringComparison.OrdinalIgnoreCase)) ||
           principal.Identities.Any(identity => identity.AuthenticationType?.Equals("ApiKey", StringComparison.OrdinalIgnoreCase) == true);

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;

    private static DateTime Min(DateTime left, DateTime right) => left <= right ? left : right;

    private static string CreateOpaqueToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static string CreateSessionId() => Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();

    public static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static ImpersonationGrantIssueResult DeniedGrant(string errorCode)
        => new(false, errorCode, null, null);

    private static ImpersonationSessionRedeemResult DeniedSession(string errorCode)
        => new(false, errorCode, null, null);
}