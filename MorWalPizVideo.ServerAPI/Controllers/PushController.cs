using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Controllers;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Models;

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
  /// <summary>
  /// Legacy mutation route retained as an explicit retirement response. New clients must use
  /// /api/push/subscriptions, which requires the per-subscription credential for existing records.
  /// </summary>
  [HttpPost("subscribe")]
  [AllowAnonymous]
  public IActionResult Subscribe()
  {
    return LegacyMutationRetired();
  }

  /// <summary>
  /// Legacy mutation route retained as an explicit retirement response. New clients must use
  /// /api/push/subscriptions/revoke with the per-subscription credential.
  /// </summary>
  [HttpDelete("unsubscribe")]
  [AllowAnonymous]
  public IActionResult Unsubscribe()
  {
    return LegacyMutationRetired();
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

  private static IActionResult LegacyMutationRetired() =>
    new StatusCodeResult(StatusCodes.Status410Gone);
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
