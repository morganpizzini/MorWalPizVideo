using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Domain.Push;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/pushnotificationtemplates")]
[Authorize]
public sealed class PushNotificationTemplatesController(
    IPushNotificationTemplateRepository repository,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.PushPlatformSend)]
    public async Task<ActionResult<IReadOnlyList<PushNotificationTemplateContract>>> Get(CancellationToken cancellationToken)
        => Ok((await repository.GetItemsAsync()).Where(x => x.IsActive).OrderBy(x => x.Name).Select(ToContract).ToArray());

    [HttpPost]
    [AllowUser(AuthorizationPermissionKeys.PushPlatformSend)]
    public async Task<ActionResult<PushNotificationTemplateContract>> Create(PushNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var normalized = Normalize(request);
        if (normalized is null) return BadRequest("Invalid destination or action.");
        var item = await repository.AddItemAsync(new PushNotificationTemplate(
            normalized.Name, normalized.Title, normalized.Body, normalized.Destination!, ToActions(normalized.Actions), 1)
        { CreatedBy = User.Identity?.Name ?? string.Empty, UpdatedAt = DateTime.UtcNow });
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ToContract(item));
    }

    [HttpPut("{id}")]
    [AllowUser(AuthorizationPermissionKeys.PushPlatformSend)]
    public async Task<ActionResult<PushNotificationTemplateContract>> Update(string id, PushNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetItemAsync(id);
        if (existing is null) return NotFound();
        var normalized = Normalize(request);
        if (normalized is null) return BadRequest("Invalid destination or action.");
        var updated = existing with
        {
            Name = normalized.Name, Title = normalized.Title, Body = normalized.Body,
            Destination = normalized.Destination!, Actions = ToActions(normalized.Actions),
            Version = existing.Version + 1, UpdatedAt = DateTime.UtcNow
        };
        await repository.UpdateItemAsync(updated);
        return Ok(ToContract(updated));
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.PushPlatformSend)]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await repository.GetItemAsync(id);
        if (existing is null) return NotFound();
        await repository.UpdateItemAsync(existing with { IsActive = false, UpdatedAt = DateTime.UtcNow });
        return NoContent();
    }

    private PushNotificationTemplateRequest? Normalize(PushNotificationTemplateRequest request)
    {
        var destination = PushDestination.Normalize(request.Destination);
        if (destination is null) return null;
        var max = Math.Clamp(configuration.GetValue("WebPush:MaxActions", 2), 0, 2);
        if (request.Actions.Count > max) return null;
        var actions = new List<PushNotificationAction>();
        foreach (var action in request.Actions)
        {
            var actionDestination = PushDestination.Normalize(action.Destination);
            if (string.IsNullOrWhiteSpace(action.Action) || string.IsNullOrWhiteSpace(action.Title) || actionDestination is null) return null;
            actions.Add(new PushNotificationAction(action.Action.Trim(), action.Title.Trim(), actionDestination));
        }
        return new PushNotificationTemplateRequest
        {
            Name = request.Name.Trim(), Title = request.Title.Trim(), Body = request.Body.Trim(),
            Destination = destination, Actions = actions.Select(a => new PushNotificationActionRequest { Action = a.Action, Title = a.Title, Destination = a.Destination }).ToArray()
        };
    }

    private static PushNotificationTemplateContract ToContract(PushNotificationTemplate item) => new()
    {
        Id = item.Id, Name = item.Name, Title = item.Title, Body = item.Body, Destination = item.Destination,
        Version = item.Version, IsActive = item.IsActive, UpdatedAt = item.UpdatedAt,
        Actions = item.Actions.Select(a => new PushNotificationActionRequest { Action = a.Action, Title = a.Title, Destination = a.Destination }).ToArray()
    };

    private static IReadOnlyList<PushNotificationAction> ToActions(IReadOnlyList<PushNotificationActionRequest> actions)
        => actions.Select(a => new PushNotificationAction(a.Action, a.Title, a.Destination)).ToArray();
}
