using MorWalPizVideo.Server.Contracts;
using MorWalPiz.Contracts.DTOs;

namespace MorWalPizVideo.BackOffice.Services;

public static class InsightCommentFilter
{
  public static List<VideoCommentDto> Retain(
      IEnumerable<CommentInfo> comments,
      string? uploaderChannelId,
      int commentCount,
      bool excludeUploaderComments)
  {
    return comments
        .Where(comment => !excludeUploaderComments || !IsUploaderComment(comment.AuthorChannelId, uploaderChannelId))
        .Take(commentCount)
        .Select(comment => new VideoCommentDto
        {
          Author = comment.Author,
          Text = comment.Text,
          PublishedAt = comment.PublishedAt
        })
        .ToList();
  }

  public static bool IsUploaderComment(string? authorChannelId, string? uploaderChannelId) =>
      !string.IsNullOrWhiteSpace(authorChannelId) &&
      !string.IsNullOrWhiteSpace(uploaderChannelId) &&
      string.Equals(authorChannelId, uploaderChannelId, StringComparison.Ordinal);
}