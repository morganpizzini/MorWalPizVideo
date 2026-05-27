using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserRequestsController : ControllerBase
{
  private readonly IUserRequestRepository _repository;
  private readonly ILogger<UserRequestsController> _logger;

  public UserRequestsController(IUserRequestRepository repository, ILogger<UserRequestsController> logger)
  {
    _repository = repository;
    _logger = logger;
  }

  /// <summary>
  /// Submit a new community video-topic request (public).
  /// POST /api/userrequests
  /// </summary>
  [HttpPost]
  [AllowAnonymous]
  public async Task<IActionResult> Submit([FromBody] SubmitUserRequestDto dto)
  {
    if (string.IsNullOrWhiteSpace(dto.Topic))
      return BadRequest(new { message = "Topic is required" });

    if (string.IsNullOrWhiteSpace(dto.Name))
      return BadRequest(new { message = "Name is required" });

    var request = new UserRequest
    {
      Name = dto.Name.Trim(),
      Email = dto.Email?.Trim() ?? string.Empty,
      Topic = dto.Topic.Trim(),
      Description = dto.Description?.Trim(),
      Status = UserRequestStatus.Pending
    };

    await _repository.AddItemAsync(request);
    _logger.LogInformation("New user request submitted: {Topic} by {Name}", request.Topic, request.Name);

    return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
  }

  /// <summary>
  /// Get all requests, optionally filtered by status (admin).
  /// GET /api/userrequests
  /// GET /api/userrequests?status=Pending
  /// </summary>
  [HttpGet]
  [Authorize]
  public async Task<IActionResult> GetAll([FromQuery] UserRequestStatus? status = null)
  {
    var items = status.HasValue
        ? await _repository.GetItemsAsync(r => r.Status == status.Value)
        : await _repository.GetItemsAsync();

    return Ok(items.OrderByDescending(r => r.Votes).ThenByDescending(r => r.CreationDateTime));
  }

  /// <summary>
  /// Get a single request by ID (admin).
  /// GET /api/userrequests/{id}
  /// </summary>
  [HttpGet("{id}")]
  [Authorize]
  public async Task<IActionResult> GetById(string id)
  {
    var item = await _repository.GetItemAsync(id);
    if (item == null)
      return NotFound();
    return Ok(item);
  }

  /// <summary>
  /// Moderate a request: change status and add admin note (admin).
  /// PATCH /api/userrequests/{id}/status
  /// </summary>
  [HttpPatch("{id}/status")]
  [Authorize]
  public async Task<IActionResult> UpdateStatus(string id, [FromBody] ModerateUserRequestDto dto)
  {
    var existing = await _repository.GetItemAsync(id);
    if (existing == null)
      return NotFound();

    var updated = existing with
    {
      Status = dto.Status,
      AdminNote = dto.AdminNote ?? existing.AdminNote
    };

    await _repository.UpdateItemAsync(updated);
    _logger.LogInformation("User request {Id} status changed to {Status} by {Admin}",
        id, dto.Status, User.Identity?.Name);

    return Ok(updated);
  }

  /// <summary>
  /// Delete a request (admin).
  /// DELETE /api/userrequests/{id}
  /// </summary>
  [HttpDelete("{id}")]
  [Authorize]
  public async Task<IActionResult> Delete(string id)
  {
    var existing = await _repository.GetItemAsync(id);
    if (existing == null)
      return NotFound();

    await _repository.DeleteItemAsync(id);
    _logger.LogInformation("User request {Id} deleted by {Admin}", id, User.Identity?.Name);

    return NoContent();
  }
}

public record SubmitUserRequestDto
{
  public string Name { get; init; } = string.Empty;
  public string? Email { get; init; }
  public string Topic { get; init; } = string.Empty;
  public string? Description { get; init; }
}

public record ModerateUserRequestDto
{
  public UserRequestStatus Status { get; init; }
  public string? AdminNote { get; init; }
}
