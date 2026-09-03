namespace MorWalPiz.Contracts.Contracts;

public sealed class AuditLogContract
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? MetadataJson { get; set; }
}