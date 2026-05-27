using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Controllers;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Services;
using System.Security.Claims;

namespace MorWalPizVideo.ServerAPI.Controllers;

/// <summary>
/// Manages Web Push subscriptions for the authenticated user.
/// </summary>
[ApiController]
[Route("api/push")]
[Authorize]
public class PushController : ApplicationControllerBase
{
  private readonly IUserRepository _userRepository;

  public PushController(IUserRepository userRepository)
  {
    _userRepository = userRepository;
  }

  /// <summary>
  /// Save a browser push subscription for the authenticated user.
  /// POST /api/push/subscribe
  /// </summary>
  [HttpPost("subscribe")]
  public async Task<IActionResult> Subscribe([FromBody] PushSubscribeDto dto)
  {
    if (string.IsNullOrWhiteSpace(dto.Endpoint) ||
        string.IsNullOrWhiteSpace(dto.P256dh) ||
        string.IsNullOrWhiteSpace(dto.Auth))
      return BadRequest(new { message = "Endpoint, p256dh and auth are required" });

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId)) return Unauthorized();

    var user = await _userRepository.GetItemAsync(userId);
    if (user == null) return NotFound(new { message = "User not found" });

    // Upsert: replace if endpoint already exists
    var newSub = new PushSubscriptionInfo
    {
      Endpoint = dto.Endpoint,
      P256dh = dto.P256dh,
      Auth = dto.Auth,
      CreatedAt = DateTime.UtcNow
    };

    var updated = user with
    {
      PushSubscriptions = user.PushSubscriptions
            .Where(s => s.Endpoint != dto.Endpoint)
            .Append(newSub)
            .ToList()
    };

    await _userRepository.UpdateItemAsync(updated);
    return NoContent();
  }

  /// <summary>
  /// Remove a browser push subscription for the authenticated user.
  /// DELETE /api/push/unsubscribe
  /// </summary>
  [HttpDelete("unsubscribe")]
  public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeDto dto)
  {
    if (string.IsNullOrWhiteSpace(dto.Endpoint))
      return BadRequest(new { message = "Endpoint is required" });

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId)) return Unauthorized();

    var user = await _userRepository.GetItemAsync(userId);
    if (user == null) return NotFound(new { message = "User not found" });

    var updated = user with
    {
      PushSubscriptions = user.PushSubscriptions
            .Where(s => s.Endpoint != dto.Endpoint)
            .ToList()
    };

    await _userRepository.UpdateItemAsync(updated);
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
