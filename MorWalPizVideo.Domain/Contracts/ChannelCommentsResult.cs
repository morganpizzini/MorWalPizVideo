using Google.Apis.YouTube.v3.Data;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Server.Contracts
{
  public class ChannelCommentsResult
  {
    public List<VideoWithComments> Videos { get; set; } = new();
  }

  public class VideoWithComments
  {
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<CommentInfo> Comments { get; set; } = new();
  }

  public class CommentInfo
  {
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
  }
}
