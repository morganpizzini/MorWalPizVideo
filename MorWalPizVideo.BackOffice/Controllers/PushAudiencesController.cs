using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Controllers;

/// <summary>
/// Named channel collections used as platform push audiences. Audiences are a content-targeting concept and are
/// deliberately independent of RBAC groups; managing them requires the same platform send permission.
/// </summary>
[ApiController]
[Route("api/pushaudiences")]
[AllowUser(AuthorizationPermissionKeys.PushPlatformSend)]
public sealed class PushAudiencesController(
    IPushAudienceRepository audienceRepository,
    IYTChannelRepository channelRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PushAudienceContract>>> GetAll()
    {
        var items = await audienceRepository.GetItemsAsync();
        return Ok(items.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).Select(ToContract).ToArray());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PushAudienceContract>> Get(string id)
    {
        var item = await audienceRepository.GetItemAsync(id);
        return item is null ? NotFound() : Ok(ToContract(item));
    }

    [HttpPost]
    public async Task<ActionResult<PushAudienceContract>> Create(PushAudienceRequest request)
    {
        var code = NormalizeCode(request.Code);
        var channelIds = await ResolveChannelIdsAsync(request.ChannelIds);
        if (channelIds.Count == 0) return BadRequest("At least one existing channel must be selected.");
        if (await audienceRepository.GetByCodeAsync(code) is not null) return Conflict("An audience with this code already exists.");

        var created = await audienceRepository.AddItemAsync(new PushAudience(code, request.Name.Trim(), channelIds)
        {
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            CreationDateTime = DateTime.UtcNow
        });
        return CreatedAtAction(nameof(Get), new { id = created.Id }, ToContract(created));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PushAudienceContract>> Update(string id, PushAudienceRequest request)
    {
        var current = await audienceRepository.GetItemAsync(id);
        if (current is null) return NotFound();

        var code = NormalizeCode(request.Code);
        var channelIds = await ResolveChannelIdsAsync(request.ChannelIds);
        if (channelIds.Count == 0) return BadRequest("At least one existing channel must be selected.");

        var conflict = await audienceRepository.GetByCodeAsync(code);
        if (conflict is not null && conflict.Id != id) return Conflict("An audience with this code already exists.");

        var updated = current with
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ChannelIds = channelIds,
            IsActive = request.IsActive,
            UpdatedAt = DateTime.UtcNow
        };
        await audienceRepository.UpdateItemAsync(updated);
        return Ok(ToContract(updated));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        if (await audienceRepository.GetItemAsync(id) is null) return NotFound();
        await audienceRepository.DeleteItemAsync(id);
        return NoContent();
    }

    private async Task<IReadOnlyList<string>> ResolveChannelIdsAsync(IReadOnlyList<string> requested)
    {
        var wanted = requested.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToHashSet(StringComparer.Ordinal);
        if (wanted.Count == 0) return [];
        var known = (await channelRepository.GetItemsAsync()).Select(channel => channel.ChannelId).ToHashSet(StringComparer.Ordinal);
        return [.. wanted.Where(known.Contains)];
    }

    private static string NormalizeCode(string code) => code.Trim().ToLowerInvariant();

    private static PushAudienceContract ToContract(PushAudience audience) => new()
    {
        Id = audience.Id,
        Code = audience.Code,
        Name = audience.Name,
        Description = audience.Description,
        ChannelIds = audience.ChannelIds,
        IsActive = audience.IsActive,
        CreationDateTime = audience.CreationDateTime,
        UpdatedAt = audience.UpdatedAt
    };
}
