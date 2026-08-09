using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Services
{
  public class InsightIngestionService : IInsightIngestionService
  {
    private const int DefaultMaxPostsPerSource = 5;
    private const int DefaultVideosPerScan = 1;
    private const int MaxVideosPerScan = 10;
    private const int DefaultCommentsPerVideo = 20;
    private const int MaxCommentsPerVideo = 100;

    private readonly IInsightsService _insightsService;
    private readonly IInsightAgentService _insightAgentService;
    private readonly IYTService _ytService;

    public InsightIngestionService(IInsightsService insightsService, IInsightAgentService insightAgentService, IYTService ytService)
    {
      _insightsService = insightsService;
      _insightAgentService = insightAgentService;
      _ytService = ytService;
    }

    public async Task<ManualScanResponseDto> ProcessManualScanAsync(InsightTopic topic, ManualScanRequest request)
    {
      var maxPostsPerSource = request.MaxPostsPerSource is > 0 ? request.MaxPostsPerSource.Value : DefaultMaxPostsPerSource;
      var createdNewsItemIds = new List<string>();
      var summaries = new List<SourceScanSummaryDto>();

      foreach (var source in request.Sources)
      {
        var summary = new SourceScanSummaryDto { SourceUrl = source.SourceUrl };

        try
        {
          var cursor = await _insightsService.GetSourceCursorAsync(topic.Id, source.SourceUrl);

          // Posts are expected newest-first; stop at the first post already seen in a previous run.
          var candidatePosts = new List<RawSocialPostDto>();
          foreach (var post in source.Posts.Take(maxPostsPerSource))
          {
            if (cursor != null && IsAlreadySeen(cursor, post))
              break;

            candidatePosts.Add(post);
          }

          summary.ProcessedCount = candidatePosts.Count;

          foreach (var post in candidatePosts)
          {
            if (string.IsNullOrWhiteSpace(post.PostUrl))
              continue;

            if (await _insightsService.NewsItemExistsBySourceUrlAsync(post.PostUrl))
            {
              summary.SkippedDuplicateCount++;
              continue;
            }

            var classification = await _insightAgentService.ClassifyPostAsync(topic, post);
            if (!classification.IsNews)
            {
              summary.SkippedNotNewsCount++;
              continue;
            }

            var newsItem = new InsightNewsItem(
                topicId: topic.Id,
                title: string.IsNullOrWhiteSpace(classification.SuggestedTitle) ? post.PostUrl : classification.SuggestedTitle,
                summary: string.IsNullOrWhiteSpace(classification.Summary) ? post.Text : classification.Summary,
                sourceUrl: post.PostUrl,
                sourceName: post.PlatformSource,
                status: InsightNewsStatus.AutoDetected,
                starRating: 0,
                aiRelevanceScore: classification.RelevanceScore,
                discoveredAt: post.PublishedAt ?? DateTime.UtcNow,
                platformSource: post.PlatformSource,
                postId: post.PostId,
                analysisReason: classification.Reason
            )
            {
              Id = Guid.NewGuid().ToString()
            };

            await _insightsService.SaveNewsItemAsync(newsItem);
            createdNewsItemIds.Add(newsItem.Id);
            summary.CreatedCount++;
          }

          if (candidatePosts.Count > 0)
          {
            var newest = candidatePosts[0];
            var updatedCursor = (cursor ?? new InsightSourceCursor(topic.Id, source.SourceUrl) { Id = Guid.NewGuid().ToString() })
                .UpdateCursor(newest.PostId, newest.PostUrl, DateTime.UtcNow);

            await _insightsService.SaveOrUpdateSourceCursorAsync(updatedCursor);
          }
        }
        catch (Exception ex)
        {
          summary.Error = ex.Message;
        }

        summaries.Add(summary);
      }

      return new ManualScanResponseDto
      {
        SourceSummaries = summaries,
        CreatedNewsItemIds = createdNewsItemIds
      };
    }

    private static bool IsAlreadySeen(InsightSourceCursor cursor, RawSocialPostDto post)
    {
      if (!string.IsNullOrEmpty(post.PostId) && !string.IsNullOrEmpty(cursor.LastSeenPostId))
        return post.PostId == cursor.LastSeenPostId;

      return !string.IsNullOrEmpty(post.PostUrl) &&
          post.PostUrl.Equals(cursor.LastSeenPostUrl, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ScanShortContentResponseDto> ProcessShortContentScanAsync(InsightTopic topic, string channelName, int videos, int commentsNumber, string? channelId = null)
    {
      var response = new ScanShortContentResponseDto();

      var channel = await _insightsService.GetChannelByNameAsync(channelName);
      if (channel == null)
        return response;

      var videoCount = videos > 0 ? Math.Min(videos, MaxVideosPerScan) : DefaultVideosPerScan;
      var commentCount = commentsNumber > 0 ? Math.Min(commentsNumber, MaxCommentsPerVideo) : DefaultCommentsPerVideo;

      var channelComments = await _ytService.GetChannelComments(channel.ChannelId, videoCount, commentCount, showVideo: true);
      var processedVideos = channel.Videos?.ToList() ?? new List<YouTubeVideo>();

      foreach (var videoWithComments in channelComments.Videos)
      {
        var existingVideo = processedVideos.FirstOrDefault(v => v.VideoId == videoWithComments.VideoId);
        var lastCommentDate = existingVideo?.LastCommentDate ?? DateTime.MinValue;

        var newComments = videoWithComments.Comments
            .Where(c => c.PublishedAt > lastCommentDate)
            .Select(c => new VideoCommentDto { Author = c.Author, Text = c.Text, PublishedAt = c.PublishedAt })
            .ToList();

        if (newComments.Count == 0)
          continue;

        response.CommentsAnalyzed += newComments.Count;

        var videoUrl = $"https://www.youtube.com/watch?v={videoWithComments.VideoId}";

        try
        {
          var newsItems = await _insightAgentService.AnalyzeVideoCommentsAsync(
              topic, videoWithComments.VideoId, videoWithComments.Title, videoUrl, channel.ChannelName, newComments);

          foreach (var newsItem in newsItems)
          {
            await _insightsService.SaveNewsItemAsync(newsItem, channelId ?? topic.ChannelId);
            response.CreatedNewsItemIds.Add(newsItem.Id);
          }

          response.VideosProcessed++;

          var newLastCommentDate = newComments.Max(c => c.PublishedAt);
          var updatedVideo = new YouTubeVideo
          {
            VideoId = videoWithComments.VideoId,
            Title = videoWithComments.Title,
            LastCommentDate = newLastCommentDate
          };

          var index = processedVideos.FindIndex(v => v.VideoId == videoWithComments.VideoId);
          if (index >= 0)
            processedVideos[index] = updatedVideo;
          else
            processedVideos.Add(updatedVideo);
        }
        catch (Exception ex)
        {
          // Leave this video's cursor untouched so its unanalyzed comments are retried on the next scan.
          response.Errors.Add($"{videoWithComments.VideoId}: {ex.Message}");
        }
      }

      await _insightsService.UpdateChannelAsync(channel with { Videos = processedVideos });

      return response;
    }

    public async Task<ScanShortContentResponseDto> AnalyzeCommentsAsync(InsightTopic topic, AnalyzeInsightCommentsRequest request, string selectedChannelId)
    {
      var response = new ScanShortContentResponseDto();
      var commentCount = request.CommentsNumber > 0 ? Math.Min(request.CommentsNumber, MaxCommentsPerVideo) : DefaultCommentsPerVideo;
      var videos = new List<(string Id, string Title, string ChannelName, IList<VideoCommentDto> Comments)>();

      if (request.SourceType == InsightCommentSourceType.StoredChannel)
      {
        var channel = await _insightsService.GetChannelByIdAsync(request.ChannelId);
        if (channel == null) return response;
        var comments = await _ytService.GetChannelComments(channel.ChannelId, 10, commentCount, showVideo: true);
        videos.AddRange(comments.Videos.Select(video => (video.VideoId, video.Title, channel.ChannelName,
          (IList<VideoCommentDto>)video.Comments.Select(comment => new VideoCommentDto { Author = comment.Author, Text = comment.Text, PublishedAt = comment.PublishedAt }).ToList())));
      }
      else
      {
        if (string.IsNullOrWhiteSpace(request.VideoId)) return response;
        var channel = string.IsNullOrWhiteSpace(request.ChannelId) ? null : await _insightsService.GetChannelByIdAsync(request.ChannelId);
        var title = channel?.Videos.FirstOrDefault(video => video.VideoId == request.VideoId)?.Title ?? string.Empty;
        var metadata = await _ytService.FetchFromYoutube(new[] { request.VideoId });
        var video = metadata.FirstOrDefault();
        title = string.IsNullOrWhiteSpace(title) ? video?.Title ?? request.VideoId : title;
        var comments = await _ytService.GetVideoComments(request.VideoId, commentCount);
        var mappedComments = comments.Items.Select(item => new VideoCommentDto
        {
          Author = item.Snippet.TopLevelComment.Snippet.AuthorDisplayName,
          Text = item.Snippet.TopLevelComment.Snippet.TextDisplay,
          PublishedAt = item.Snippet.TopLevelComment.Snippet.PublishedAt.GetValueOrDefault()
        }).ToList();
        videos.Add((request.VideoId, title, channel?.ChannelName ?? "YouTube", mappedComments));
      }

      foreach (var video in videos)
      {
        if (video.Comments.Count == 0) continue;
        response.CommentsAnalyzed += video.Comments.Count;
        var items = await _insightAgentService.AnalyzeVideoCommentsAsync(topic, video.Id, video.Title,
          $"https://www.youtube.com/watch?v={video.Id}", video.ChannelName, video.Comments);
        foreach (var item in items)
        {
          await _insightsService.SaveNewsItemAsync(item, selectedChannelId);
          response.CreatedNewsItemIds.Add(item.Id);
        }
        response.VideosProcessed++;
      }
      return response;
    }
  }
}
