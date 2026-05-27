using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using WebPush;

namespace MorWalPizVideo.ServerAPI.Services;

/// <summary>
/// Web Push notification service using the VAPID protocol (RFC 8030).
/// VAPID keys are loaded from configuration:
///   WebPush:PublicKey  — the URL-safe Base64 ECDH public key
///   WebPush:PrivateKey — the URL-safe Base64 ECDH private key
///   WebPush:Subject    — mailto: or https: subject URI for VAPID header
/// </summary>
public sealed class WebPushService : IWebPushService
{
  private readonly WebPushClient _client;
  private readonly VapidDetails _vapid;
  private readonly IUserRepository _userRepository;
  private readonly IUserChannelRepository _userChannelRepository;
  private readonly ILogger<WebPushService> _logger;

  public WebPushService(
      IConfiguration configuration,
      IUserRepository userRepository,
      IUserChannelRepository userChannelRepository,
      ILogger<WebPushService> logger)
  {
    _userRepository = userRepository;
    _userChannelRepository = userChannelRepository;
    _logger = logger;

    var section = configuration.GetSection("WebPush");
    var publicKey = section["PublicKey"] ?? throw new InvalidOperationException("WebPush:PublicKey not configured");
    var privateKey = section["PrivateKey"] ?? throw new InvalidOperationException("WebPush:PrivateKey not configured");
    var subject = section["Subject"] ?? "mailto:info@morwalpiz.com";

    _vapid = new VapidDetails(subject, publicKey, privateKey);
    _client = new WebPushClient();
  }

  /// <inheritdoc/>
  public async Task SendAsync(PushSubscriptionInfo sub, string payload, CancellationToken ct = default)
  {
    var pushSub = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
    try
    {
      await _client.SendNotificationAsync(pushSub, payload, _vapid);
    }
    catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
    {
      // Subscription has expired / been revoked — caller should clean it up
      _logger.LogWarning("Push subscription gone (410): {Endpoint}", sub.Endpoint);
      throw;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to send push to {Endpoint}", sub.Endpoint);
    }
  }

  /// <inheritdoc/>
  public async Task SendToChannelSubscribersAsync(string channelId, string payload, CancellationToken ct = default)
  {
    // 1. Find all active subscriptions for this channel
    var channelSubs = await _userChannelRepository.GetByChannelIdAsync(channelId);
    var activeUserIds = channelSubs
        .Where(cs => cs.IsActive)
        .Select(cs => cs.UserId)
        .Distinct()
        .ToList();

    if (!activeUserIds.Any()) return;

    // 2. For each subscribed user, send to each of their push endpoints
    foreach (var userId in activeUserIds)
    {
      try
      {
        var user = await _userRepository.GetItemAsync(userId);
        if (user == null || !user.PushSubscriptions.Any()) continue;

        var staleEndpoints = new List<string>();
        foreach (var sub in user.PushSubscriptions)
        {
          try
          {
            await SendAsync(sub, payload, ct);
          }
          catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
          {
            staleEndpoints.Add(sub.Endpoint);
          }
        }

        // Remove stale subscriptions
        if (staleEndpoints.Any())
        {
          var cleaned = user with
          {
            PushSubscriptions = user.PushSubscriptions
                  .Where(s => !staleEndpoints.Contains(s.Endpoint))
                  .ToList()
          };
          await _userRepository.UpdateItemAsync(cleaned);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error sending push for user {UserId}", userId);
      }
    }
  }
}
