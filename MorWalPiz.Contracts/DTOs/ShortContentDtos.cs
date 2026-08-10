namespace MorWalPiz.Contracts.DTOs
{
  using MorWalPizVideo.Server.Models;

  /// <summary>
  /// A single comment collected from a YouTube video, pending AI analysis for ShortContent ideas.
  /// </summary>
  public class VideoCommentDto
  {
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
  }

  /// <summary>
  /// Request to scan a YouTube channel's recent video comments for ShortContent ideas.
  /// </summary>
  public class ScanShortContentRequest
  {
    public string ChannelName { get; set; } = string.Empty;
    public int Videos { get; set; } = 1;
    public int CommentsNumber { get; set; } = 20;
  }

  public class ScanShortContentResponseDto
  {
    public int VideosProcessed { get; set; }
    public int CommentsAnalyzed { get; set; }
    public List<string> CreatedNewsItemIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
  }

  public enum InsightCommentSourceType
  {
    StoredChannel,
    StoredVideo,
    DirectVideoId
  }

  public class AnalyzeInsightCommentsRequest
  {
    public InsightCommentSourceType SourceType { get; set; }
    public InsightSourceKind? SourceKind { get; set; }
    public string ChannelId { get; set; } = string.Empty;
    public string VideoId { get; set; } = string.Empty;
    public int CommentsNumber { get; set; } = 20;
  }
}
