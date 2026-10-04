using Microsoft.Extensions.Logging;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain.Push;

public enum PushSubscriptionOutcome { Created, Updated, Forbidden, Invalid, NotFound, Revoked }

/// <summary>
/// Result of an anonymous subscription operation. <see cref="Credential"/> is populated only when a new
/// credential is minted; it is the single moment the plaintext value exists outside the browser.
/// </summary>
public sealed record PushSubscriptionResult(
    PushSubscriptionOutcome Outcome,
    IReadOnlyList<string> ChannelIds,
    string? Credential = null);

public sealed record PushSubscriptionRequest(
    string Endpoint,
    string P256dh,
    string Auth,
    IReadOnlyList<string> ChannelIds,
    string ApplicationKey,
    string? Language,
    string? Credential);

public interface IPushSubscriptionService
{
    Task<PushSubscriptionResult> SubscribeAsync(PushSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<PushSubscriptionResult> GetSettingsAsync(string endpoint, string credential, CancellationToken cancellationToken = default);
    Task<PushSubscriptionResult> RevokeAsync(string endpoint, string credential, CancellationToken cancellationToken = default);
}

/// <summary>
/// Owns anonymous, per-channel Web Push subscriptions for the public applications. There is no user identity:
/// a 256-bit credential minted on first consent (stored hashed) authorises later preference changes and revocation.
/// </summary>
public sealed class PushSubscriptionService(
    IPushSubscriptionRepository subscriptionRepository,
    ILogger<PushSubscriptionService> logger) : IPushSubscriptionService
{
    private const int MaxChannelsPerSubscription = 50;
    private static readonly HashSet<string> AllowedApplications =
        ["morwalpizvideo", "shooting-ita", "backoffice"];

    public async Task<PushSubscriptionResult> SubscribeAsync(PushSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var channelIds = NormalizeChannels(request.ChannelIds);
        if (!IsWellFormed(request) || !AllowedApplications.Contains(request.ApplicationKey) ||
            (channelIds.Count == 0 && !string.Equals(request.ApplicationKey, "backoffice", StringComparison.Ordinal)) ||
            !await PushEndpointValidator.IsSafeAsync(request.Endpoint, cancellationToken))
            return new(PushSubscriptionOutcome.Invalid, []);

        var endpointHash = PushEndpointProtection.HashEndpoint(request.Endpoint);
        var existing = await subscriptionRepository.GetByEndpointHashAsync(endpointHash, cancellationToken);
        var now = DateTime.UtcNow;

        if (existing is not null)
        {
            // An existing endpoint can only be re-targeted by whoever holds its credential.
            if (!PushEndpointProtection.HashesMatch(existing.CredentialHash, HashOrNull(request.Credential)))
            {
                logger.LogWarning("Rejected push preference change without a matching credential for {Endpoint}",
                    PushEndpointProtection.Redact(request.Endpoint));
                return new(PushSubscriptionOutcome.Forbidden, []);
            }

            var reactivated = await subscriptionRepository.UpsertAsync(existing with
            {
                Endpoint = request.Endpoint,
                Keys = new PushSubscriptionKeys(request.P256dh, request.Auth),
                ChannelIds = channelIds,
                ApplicationKey = request.ApplicationKey,
                Scope = string.Equals(request.ApplicationKey, "backoffice", StringComparison.Ordinal)
                    ? PushSubscriptionScope.Platform
                    : PushSubscriptionScope.Channel,
                Language = request.Language,
                IsActive = true,
                RevokedAt = null,
                UpdatedAt = now
            }, cancellationToken);

            logger.LogInformation("Updated anonymous push subscription {Endpoint} to {ChannelCount} channel(s)",
                PushEndpointProtection.Redact(request.Endpoint), reactivated.ChannelIds.Count);
            return new(PushSubscriptionOutcome.Updated, reactivated.ChannelIds);
        }

        var credential = PushEndpointProtection.CreateCredential();
        var created = await subscriptionRepository.UpsertAsync(new PushChannelSubscription(
            endpointHash,
            request.Endpoint,
            new PushSubscriptionKeys(request.P256dh, request.Auth),
            PushEndpointProtection.HashCredential(credential),
            channelIds)
        {
            ApplicationKey = request.ApplicationKey,
            Scope = string.Equals(request.ApplicationKey, "backoffice", StringComparison.Ordinal)
                ? PushSubscriptionScope.Platform
                : PushSubscriptionScope.Channel,
            Language = request.Language,
            ConsentedAt = now,
            UpdatedAt = now,
            CreationDateTime = now
        }, cancellationToken);

        logger.LogInformation("Stored anonymous push subscription {Endpoint} for {ChannelCount} channel(s)",
            PushEndpointProtection.Redact(request.Endpoint), created.ChannelIds.Count);
        return new(PushSubscriptionOutcome.Created, created.ChannelIds, credential);
    }

    public async Task<PushSubscriptionResult> GetSettingsAsync(string endpoint, string credential, CancellationToken cancellationToken = default)
    {
        var subscription = await ResolveAsync(endpoint, credential, cancellationToken);
        return subscription is null
            ? new(PushSubscriptionOutcome.NotFound, [])
            : new(PushSubscriptionOutcome.Updated, subscription.IsActive ? subscription.ChannelIds : []);
    }

    public async Task<PushSubscriptionResult> RevokeAsync(string endpoint, string credential, CancellationToken cancellationToken = default)
    {
        var subscription = await ResolveAsync(endpoint, credential, cancellationToken);
        if (subscription is null) return new(PushSubscriptionOutcome.NotFound, []);

        await subscriptionRepository.DeactivateAsync(subscription.EndpointHash, DateTime.UtcNow, cancellationToken);
        logger.LogInformation("Revoked anonymous push subscription {Endpoint}", PushEndpointProtection.Redact(endpoint));
        return new(PushSubscriptionOutcome.Revoked, []);
    }

    private async Task<PushChannelSubscription?> ResolveAsync(string endpoint, string credential, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(credential)) return null;
        var subscription = await subscriptionRepository.GetByEndpointHashAsync(PushEndpointProtection.HashEndpoint(endpoint), cancellationToken);
        return subscription is not null && PushEndpointProtection.HashesMatch(subscription.CredentialHash, PushEndpointProtection.HashCredential(credential))
            ? subscription
            : null;
    }

    private static string? HashOrNull(string? credential) =>
        string.IsNullOrWhiteSpace(credential) ? null : PushEndpointProtection.HashCredential(credential);

    private static bool IsWellFormed(PushSubscriptionRequest request) =>
        !string.IsNullOrWhiteSpace(request.Endpoint) &&
        Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        !string.IsNullOrWhiteSpace(request.P256dh) &&
        !string.IsNullOrWhiteSpace(request.Auth);

    private static IReadOnlyList<string> NormalizeChannels(IReadOnlyList<string> channelIds) =>
        [.. channelIds
            .Where(channelId => !string.IsNullOrWhiteSpace(channelId))
            .Select(channelId => channelId.Trim())
            .Distinct(StringComparer.Ordinal)
            .Take(MaxChannelsPerSubscription)];
}
