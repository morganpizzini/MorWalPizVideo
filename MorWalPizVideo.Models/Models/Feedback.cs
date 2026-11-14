using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public enum FeedbackStatus
{
    New,
    Reviewing,
    Resolved,
    Dismissed
}

public sealed record Feedback : BaseEntity
{
    [BsonElement("requestId")]
    public string RequestId { get; init; } = string.Empty;
    [BsonElement("fingerprint")]
    public string Fingerprint { get; init; } = string.Empty;
    [BsonElement("application")]
    public string Application { get; init; } = string.Empty;
    [BsonElement("description")]
    public string Description { get; init; } = string.Empty;
    [BsonElement("pagePath")]
    public string PagePath { get; init; } = string.Empty;
    [BsonElement("componentKey")]
    public string? ComponentKey { get; init; }
    [BsonElement("clientRelease")]
    public string? ClientRelease { get; init; }
    [BsonElement("userAgent")]
    public string? UserAgent { get; init; }
    [BsonElement("language")]
    public string? Language { get; init; }
    [BsonElement("timezone")]
    public string? Timezone { get; init; }
    [BsonElement("viewportWidth")]
    public int? ViewportWidth { get; init; }
    [BsonElement("viewportHeight")]
    public int? ViewportHeight { get; init; }
    [BsonElement("online")]
    public bool? Online { get; init; }
    [BsonElement("reporterAccountId")]
    public string? ReporterAccountId { get; init; }
    [BsonElement("reporterUsername")]
    public string? ReporterUsername { get; init; }
    [BsonElement("status")]
    public FeedbackStatus Status { get; init; } = FeedbackStatus.New;
    [BsonElement("revision")]
    public long Revision { get; init; } = 1;
    [BsonElement("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; init; }
    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; init; }
    [BsonElement("updatedByAccountId")]
    public string? UpdatedByAccountId { get; init; }
    [BsonElement("updatedByUsername")]
    public string? UpdatedByUsername { get; init; }
}
