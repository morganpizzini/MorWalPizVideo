using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Controllers;
using MorWalPizVideo.Domain.Push;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.ServerAPI.Controllers;

/// <summary>
/// Legacy Web Push subscription routes. These registrations are anonymous and are
/// stored in the push subscription collection so the BackOffice dispatch pipeline
/// can address them.
/// </summary>
[ApiController]
[Route("api/push")]
public class PushController : ApplicationControllerBase
{
  private const string ApplicationKey = "backoffice";
  private readonly IPushSubscriptionRepository _subscriptionRepository;

  public PushController(IPushSubscriptionRepository subscriptionRepository)
  {
    _subscriptionRepository = subscriptionRepository;
  }

  /// <summary>
  /// Save a browser push subscription without requiring an application user.
  /// POST /api/push/subscribe
  /// </summary>
  [HttpPost("subscribe")]
  [AllowAnonymous]
  public async Task<IActionResult> Subscribe([FromBody] PushSubscribeDto dto)
  {
    if (string.IsNullOrWhiteSpace(dto.Endpoint) ||
        string.IsNullOrWhiteSpace(dto.P256dh) ||
        string.IsNullOrWhiteSpace(dto.Auth))
      return BadRequest(new { message = "Endpoint, p256dh and auth are required" });

    if (!await PushEndpointValidator.IsSafeAsync(dto.Endpoint))
      return BadRequest(new { message = "Endpoint is not valid" });

    var endpointHash = PushEndpointProtection.HashEndpoint(dto.Endpoint);
    var existing = await _subscriptionRepository.GetByEndpointHashAsync(endpointHash);
    var now = DateTime.UtcNow;
    var subscription = existing is null
      ? new PushChannelSubscription(
          endpointHash,
          dto.Endpoint,
          new PushSubscriptionKeys(dto.P256dh, dto.Auth),
          string.Empty,
          [])
        {
          ApplicationKey = ApplicationKey,
          Scope = PushSubscriptionScope.Platform,
          ConsentedAt = now,
          CreationDateTime = now
        }
      : existing with
        {
          Endpoint = dto.Endpoint,
          Keys = new PushSubscriptionKeys(dto.P256dh, dto.Auth),
          ApplicationKey = ApplicationKey,
          Scope = PushSubscriptionScope.Platform,
          IsActive = true,
          RevokedAt = null
        };

    await _subscriptionRepository.UpsertAsync(subscription with
    {
      UpdatedAt = now
    });
    return NoContent();
  }

  /// <summary>
  /// Remove a browser push subscription without requiring an application user.
  /// DELETE /api/push/unsubscribe
  /// </summary>
  [HttpDelete("unsubscribe")]
  [AllowAnonymous]
  public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeDto dto)
  {
    if (string.IsNullOrWhiteSpace(dto.Endpoint))
      return BadRequest(new { message = "Endpoint is required" });

    await _subscriptionRepository.DeactivateAsync(
      PushEndpointProtection.HashEndpoint(dto.Endpoint),
      DateTime.UtcNow);
    return NoContent();
  }

  /// <summary>
  /// Returns the VAPID public key so the browser can subscribe.
  /// GET /api/push/public-key
  /// </summary>
  [HttpGet("public-key")]
  [AllowAnonymous]
  public IActionResult GetPublicKey([FromServices] IConfiguration configuration)
  {
    var publicKey = configuration["WebPush:PublicKey"];
    if (string.IsNullOrEmpty(publicKey))
      return StatusCode(503, new { message = "Push notifications not configured" });

    return Ok(new { publicKey });
  }
}

public record PushSubscribeDto
{
  public string Endpoint { get; init; } = string.Empty;
  public string P256dh { get; init; } = string.Empty;
  public string Auth { get; init; } = string.Empty;
}

public record PushUnsubscribeDto
{
  public string Endpoint { get; init; } = string.Empty;
}
