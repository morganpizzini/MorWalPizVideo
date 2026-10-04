using System.Net;
using MorWalPizVideo.Domain.Push;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using WebPush;

namespace MorWalPizVideo.BackOffice.Services;

/// <summary>Outcome of a single endpoint delivery attempt.</summary>
public enum PushSendOutcome
{
    Sent,
    /// <summary>The push service rejected the endpoint permanently (404/410); the subscription must be pruned.</summary>
    Gone,
    /// <summary>Permanently invalid payload or credentials; retrying cannot succeed.</summary>
    Rejected,
    /// <summary>Temporary failure; the recipient stays claimable for a later batch.</summary>
    Transient
}

public sealed record PushSendResult(PushSendOutcome Outcome, string? Reason = null);

public interface IWebPushSender
{
    bool IsConfigured { get; }
    Task<PushSendResult> SendAsync(PushChannelSubscription subscription, string payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// VAPID (RFC 8030/8292) delivery using the shared <see cref="HttpClientNames.WebPush"/> named client,
/// so the connection pool is owned by <see cref="IHttpClientFactory"/> rather than this service.
/// Endpoints are redacted before they reach any log sink.
/// </summary>
public sealed class WebPushSender : IWebPushSender
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<WebPushSender> logger;
    private readonly VapidDetails? vapidDetails;

    public WebPushSender(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<WebPushSender> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;

        var section = configuration.GetSection("WebPush");
        var publicKey = section["PublicKey"];
        var privateKey = section["PrivateKey"];
        if (!string.IsNullOrWhiteSpace(publicKey) && !string.IsNullOrWhiteSpace(privateKey))
            vapidDetails = new VapidDetails(section["Subject"] ?? "mailto:info@morwalpiz.com", publicKey, privateKey);
    }

    public bool IsConfigured => vapidDetails is not null;

    public async Task<PushSendResult> SendAsync(PushChannelSubscription subscription, string payload, CancellationToken cancellationToken = default)
    {
        if (vapidDetails is null)
            return new(PushSendOutcome.Rejected, "WebPush VAPID keys are not configured.");

        // The factory owns the handler lifetime; the client must not be disposed here.
        var client = new WebPushClient(httpClientFactory.CreateClient(HttpClientNames.WebPush));
        try
        {
            await client.SendNotificationAsync(
                new WebPush.PushSubscription(subscription.Endpoint, subscription.Keys.P256dh, subscription.Keys.Auth),
                payload,
                vapidDetails,
                cancellationToken);
            return new(PushSendOutcome.Sent);
        }
        catch (WebPushException exception) when (exception.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
        {
            logger.LogInformation("Push endpoint {Endpoint} is gone ({StatusCode}); pruning subscription",
                PushEndpointProtection.Redact(subscription.Endpoint), (int)exception.StatusCode);
            return new(PushSendOutcome.Gone, $"Push service returned {(int)exception.StatusCode}.");
        }
        catch (WebPushException exception) when (IsPermanent(exception.StatusCode))
        {
            logger.LogWarning("Push delivery to {Endpoint} permanently rejected with {StatusCode}",
                PushEndpointProtection.Redact(subscription.Endpoint), (int)exception.StatusCode);
            return new(PushSendOutcome.Rejected, $"Push service returned {(int)exception.StatusCode}.");
        }
        catch (Exception exception) when (exception is WebPushException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Transient push delivery failure for {Endpoint}",
                PushEndpointProtection.Redact(subscription.Endpoint));
            return new(PushSendOutcome.Transient, exception.Message);
        }
    }

    private static bool IsPermanent(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
            or HttpStatusCode.RequestEntityTooLarge;
}

/// <summary>No-op sender used when running in mock mode or without VAPID keys.</summary>
public sealed class WebPushSenderMock : IWebPushSender
{
    public bool IsConfigured => true;

    public Task<PushSendResult> SendAsync(PushChannelSubscription subscription, string payload, CancellationToken cancellationToken = default)
        => Task.FromResult(new PushSendResult(PushSendOutcome.Sent));
}
