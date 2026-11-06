using System.ComponentModel.DataAnnotations;
using MorWalPizVideo.Models.Models;

namespace MorWalPiz.Contracts.Contracts;

public sealed record BlogInteractionSummaryContract(long Likes, long Dislikes, long ApprovedComments);
public sealed record BlogCommentContract(string Id, string DisplayName, string Body, DateTime CreatedAt);
public sealed record BlogCommentPageContract(IReadOnlyList<BlogCommentContract> Items, int Page, int PageSize, long TotalCount);
public sealed record BlogAdminCommentPageContract(IReadOnlyList<BlogAdminCommentContract> Items, int Page, int PageSize, long TotalCount);
public sealed record BlogViewerStateContract(bool CommentsBlocked, bool PortalAccessRevoked, BlogReactionValue? Reaction);
public sealed record BlogCommentRequest
{
    [Required, StringLength(2000, MinimumLength = 1)] public string Body { get; init; } = string.Empty;
    [Required, StringLength(80, MinimumLength = 8)] public string RequestId { get; init; } = string.Empty;
}
public sealed record BlogReactionRequest
{
    [Required] public string Value { get; init; } = string.Empty;
}
public sealed record BlogCommentModerationRequest
{
    [Range(1, long.MaxValue)] public long Revision { get; init; }
    [Required] public BlogCommentStatus Status { get; init; }
}
public sealed record BlogPortalAccessRequest
{
    [Range(0, long.MaxValue)] public long Revision { get; init; }
    public bool CommentsBlocked { get; init; }
    public bool PortalAccessRevoked { get; init; }
    [Required, StringLength(500, MinimumLength = 1)] public string Reason { get; init; } = string.Empty;
}
public sealed record BlogPortalAccessContract(
    string ChannelId, string AccountId, bool CommentsBlocked, bool PortalAccessRevoked,
    long Revision, string Reason, string UpdatedBy, DateTime? UpdatedAt);
public sealed record BlogAdminCommentContract(
    string Id, string AccountId, string DisplayName, string PostId, string Slug, string Body,
    BlogCommentStatus Status, long Revision, DateTime CreatedAt, string? ModeratedBy, DateTime? ModeratedAt);
public sealed record PublicAccountDirectoryItemContract(
    string Id, string Username, string DisplayName, bool IsActive, DateTime CreatedAt);
public sealed record PublicAccountDirectoryPageContract(
    IReadOnlyList<PublicAccountDirectoryItemContract> Items, int Page, int PageSize, long TotalCount);
public sealed record PublicAccountAdminDetailContract(
    string Id, string Username, string DisplayName, bool IsActive, DateTime CreatedAt,
    BlogPortalAccessContract PortalAccess, long CommentCount, long LikeCount, long DislikeCount);
public sealed record BlogReactionAdminContract(string PostId, string Slug, BlogReactionValue Value, DateTime CreatedAt);
public sealed record BlogArticleInteractionsContract(
    string PostId, string Slug, string Title, bool Published, long Likes, long Dislikes, long Comments);
public sealed record BlogArticleInteractionsPageContract(
    IReadOnlyList<BlogArticleInteractionsContract> Items, int Page, int PageSize, bool HasMore);
public sealed record PublicAccountInteractionDetailContract(
    PublicAccountAdminDetailContract Account, IReadOnlyList<BlogAdminCommentContract> Comments,
    IReadOnlyList<BlogReactionAdminContract> Reactions, int Page, int PageSize);
