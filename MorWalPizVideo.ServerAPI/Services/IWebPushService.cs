using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.ServerAPI.Services;

/// <summary>
/// Sends Web Push notifications using the VAPID protocol.
/// </summary>
public interface IWebPushService
{
  /// <summary>
  /// Send a push notification to a specific subscription endpoint.
  /// </summary>
  Task SendAsync(PushSubscriptionInfo subscription, string payload, CancellationToken ct = default);

  /// <summary>
  /// Send a push notification to all subscribers of a YouTube channel.
  /// </summary>
  Task SendToChannelSubscribersAsync(string channelId, string payload, CancellationToken ct = default);
}
