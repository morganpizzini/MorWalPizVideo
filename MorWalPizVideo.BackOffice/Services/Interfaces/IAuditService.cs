using System.Security.Claims;
using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Services.Interfaces;

public interface IAuditService
{
    Task<IReadOnlyList<AuditEvent>> GetEntityLogsAsync(string entityType, string entityId, int limit = 50);
    Task<IReadOnlyList<AuditEvent>> GetActorLogsAsync(string actorId, int limit = 50);

    Task RecordAsync(
        ClaimsPrincipal actor,
        string eventType,
        string entityType,
        string entityId,
        object? before,
        object? after,
        object? metadata = null);
}