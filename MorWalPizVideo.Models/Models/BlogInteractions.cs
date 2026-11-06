using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public enum BlogCommentStatus
{
    Pending,
    Approved,
    Rejected,
    Hidden,
    Withdrawn
}

public enum BlogReactionValue
{
    Like,
    Dislike
}

public sealed record BlogComment : BaseEntity
{
    [BsonElement("channelId")] public string ChannelId { get; init; } = string.Empty;
    [BsonElement("postId")] public string PostId { get; init; } = string.Empty;
    [BsonElement("slug")] public string Slug { get; init; } = string.Empty;
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("displayName")] public string DisplayName { get; init; } = string.Empty;
    [BsonElement("body")] public string Body { get; init; } = string.Empty;
    [BsonElement("status")] public BlogCommentStatus Status { get; init; } = BlogCommentStatus.Pending;
    [BsonElement("revision")] public long Revision { get; init; } = 1;
    [BsonElement("requestId")] public string RequestId { get; init; } = string.Empty;
    [BsonElement("moderatedBy")] public string? ModeratedBy { get; init; }
    [BsonElement("moderatedAt")] public DateTime? ModeratedAt { get; init; }
}

public sealed record BlogReaction : BaseEntity
{
    [BsonElement("channelId")] public string ChannelId { get; init; } = string.Empty;
    [BsonElement("postId")] public string PostId { get; init; } = string.Empty;
    [BsonElement("slug")] public string Slug { get; init; } = string.Empty;
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("value")] public BlogReactionValue Value { get; init; }
    [BsonElement("revision")] public long Revision { get; init; } = 1;
}

public sealed record BlogPortalAccess : BaseEntity
{
    [BsonElement("channelId")] public string ChannelId { get; init; } = string.Empty;
    [BsonElement("accountId")] public string AccountId { get; init; } = string.Empty;
    [BsonElement("commentsBlocked")] public bool CommentsBlocked { get; init; }
    [BsonElement("portalAccessRevoked")] public bool PortalAccessRevoked { get; init; }
    [BsonElement("revision")] public long Revision { get; init; } = 1;
    [BsonElement("reason")] public string Reason { get; init; } = string.Empty;
    [BsonElement("updatedBy")] public string UpdatedBy { get; init; } = string.Empty;
    [BsonElement("updatedAt")] public DateTime UpdatedAt { get; init; }
}
