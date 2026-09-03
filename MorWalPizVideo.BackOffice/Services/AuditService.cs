using System.Security.Claims;
using System.Text.Json;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

public sealed class AuditService(
    IAuditEventRepository repository,
    ILogger<AuditService> logger,
    TimeProvider timeProvider) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<AuditEvent>> GetEntityLogsAsync(string entityType, string entityId, int limit = 50)
        => (await repository.GetItemsAsync(eventItem =>
                eventItem.EntityType == entityType && eventItem.EntityId == entityId))
            .OrderByDescending(eventItem => eventItem.OccurredAt)
            .Take(Math.Clamp(limit, 1, 50))
            .ToList();

    public async Task<IReadOnlyList<AuditEvent>> GetActorLogsAsync(string actorId, int limit = 50)
        => (await repository.GetItemsAsync(eventItem => eventItem.ActorId == actorId))
            .OrderByDescending(eventItem => eventItem.OccurredAt)
            .Take(Math.Clamp(limit, 1, 50))
            .ToList();

    public async Task RecordAsync(
        ClaimsPrincipal actor,
        string eventType,
        string entityType,
        string entityId,
        object? before,
        object? after,
        object? metadata = null)
    {
        try
        {
            await repository.AddItemAsync(new AuditEvent
            {
                EventType = eventType,
                EntityType = entityType,
                EntityId = entityId,
                ActorId = actor.FindFirstValue("actor_user_id") ??
                    actor.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    actor.FindFirstValue("ApiKeyId") ?? string.Empty,
                ActorType = actor.HasClaim(claim => claim.Type.Equals("ApiKeyId", StringComparison.OrdinalIgnoreCase))
                    ? "api_key"
                    : "user",
                OccurredAt = timeProvider.GetUtcNow().UtcDateTime,
                BeforeJson = Serialize(before),
                AfterJson = Serialize(after),
                MetadataJson = Serialize(metadata)
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to persist audit event {EventType} for {EntityType} {EntityId}",
                eventType, entityType, entityId);
        }
    }

    private static string? Serialize(object? value) => value is null
        ? null
        : JsonSerializer.Serialize(value, JsonOptions);
}