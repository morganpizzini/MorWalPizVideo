using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.ServerAPI.Services;

/// <summary>
/// No-op Web Push implementation used in mock scenarios where VAPID keys are not configured.
/// </summary>
public sealed class WebPushServiceMock : IWebPushService
{
    public Task SendAsync(PushSubscriptionInfo subscription, string payload, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SendToChannelSubscribersAsync(string channelId, string payload, CancellationToken ct = default)
        => Task.CompletedTask;
}
