using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.DTOs;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.Globalization;
using System.Text;

namespace MorWalPizVideo.BackOffice.Controllers
{
    [RequireChannelScope]
    public class InsightsController : ApplicationControllerBase
    {
        private readonly IInsightsService _insightsService;
        private readonly IInsightAgentService _insightAgentService;
        private readonly IInsightIngestionService _insightIngestionService;

        public InsightsController(IInsightsService insightsService, IInsightAgentService insightAgentService, IInsightIngestionService insightIngestionService)
        {
            _insightsService = insightsService;
            _insightAgentService = insightAgentService;
            _insightIngestionService = insightIngestionService;
        }

        private string SelectedChannelId => HttpContext.GetChannelContext().ChannelId;

        #region Topics

        [HttpGet("topics")]
        [ApiKeyAuth]
        public async Task<IActionResult> GetTopics()
        {
            var topics = await _insightsService.GetTopicsAsync(SelectedChannelId);
            return Ok(topics.Select(ContractUtils.Convert));
        }

        [HttpGet("topics/admin")]
        [AllowUser(AuthorizationPermissionKeys.InsightsView, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> GetTopicsForAdmin()
        {
            var topics = await _insightsService.GetTopicsAsync(SelectedChannelId);
            return Ok(topics.Select(ContractUtils.Convert));
        }

        [HttpGet("topics/{id}")]
        [AllowUser(AuthorizationPermissionKeys.InsightsView, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> GetTopicById([FromRoute] string id)
        {
            var topic = await _insightsService.GetTopicByIdAsync(id, SelectedChannelId);
            if (topic == null)
                return NotFound();

            return Ok(ContractUtils.Convert(topic));
        }

        [HttpPost("topics")]
        [AllowUser(AuthorizationPermissionKeys.InsightsCreate, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> CreateTopic([FromBody] CreateInsightTopicRequest request)
        {
            var topic = new InsightTopic(
                title: request.Title,
                description: request.Description,
                seedArguments: request.SeedArguments ?? Array.Empty<string>(),
                preferredSources: request.PreferredSources ?? Array.Empty<string>()
            )
            {
                Id = ObjectId.GenerateNewId().ToString()
            };

            await _insightsService.SaveTopicAsync(topic, SelectedChannelId);
            return CreatedAtAction(nameof(GetTopicById), new { id = topic.Id }, ContractUtils.Convert(topic));
        }

        [HttpPut("topics/{id}")]
        [AllowUser(AuthorizationPermissionKeys.InsightsUpdate, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> UpdateTopic([FromRoute] string id, [FromBody] UpdateInsightTopicRequest request)
        {
            var existing = await _insightsService.GetTopicByIdAsync(id, SelectedChannelId);
            if (existing == null)
                return NotFound();

            var updated = existing with
            {
                Title = request.Title ?? existing.Title,
                Description = request.Description ?? existing.Description,
                SeedArguments = request.SeedArguments ?? existing.SeedArguments,
                PreferredSources = request.PreferredSources ?? existing.PreferredSources
            };

            await _insightsService.UpdateTopicAsync(updated, SelectedChannelId);
            return Ok(ContractUtils.Convert(updated));
        }

        [HttpDelete("topics/{id}")]
        [AllowUser(AuthorizationPermissionKeys.InsightsDelete, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> DeleteTopic([FromRoute] string id)
        {
            var existing = await _insightsService.GetTopicByIdAsync(id, SelectedChannelId);
            if (existing == null)
                return NotFound();

            await _insightsService.DeleteTopicAsync(id, SelectedChannelId);
            return NoContent();
        }

        #endregion

        #region News Discovery

        [HttpPost("topics/{id}/scan-news")]
        [AllowUser(AuthorizationPermissionKeys.InsightsScan, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> ScanNewsForTopic([FromRoute] string id)
        {
            var topic = await _insightsService.GetTopicByIdAsync(id, SelectedChannelId);
            if (topic == null)
                return NotFound();

            var discoveredNews = await _insightAgentService.DiscoverNewsAsync(topic);

            // Save discovered news items
            foreach (var newsItem in discoveredNews)
            {
                await _insightsService.SaveNewsItemAsync(newsItem, SelectedChannelId);
            }

            // Rank the items
            var rankedNews = await _insightAgentService.RankNewsItemsAsync(discoveredNews);

            return Ok(rankedNews.Select(ContractUtils.Convert));
        }

        /// <summary>
        /// Ingests posts collected by the interactive scanner app for a manually triggered run.
        /// Protected via API key since it is called by the desktop scanner, not by the SPA.
        /// </summary>
        [HttpPost("topics/{id}/manual-scan")]
        [ApiKeyAuth]
        public async Task<IActionResult> ManualScanTopic([FromRoute] string id, [FromBody] ManualScanRequest request)
        {
            var topic = await _insightsService.GetTopicByIdAsync(id, SelectedChannelId);
            if (topic == null)
                return NotFound();

            var result = await _insightIngestionService.ProcessManualScanAsync(topic, request);
            return Ok(result);
        }

        /// <summary>
        /// Analyzes recent YouTube comments for a channel and derives ShortContent insight items (ideas/hints)
        /// for the topic, replacing the retired ScraperController flow. Uses classic backoffice authentication
        /// since it is called from the admin SPA.
        /// </summary>
        [HttpPost("topics/{id}/scan-short-content")]
        [AllowUser(AuthorizationPermissionKeys.InsightsScan, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> ScanShortContentForTopic([FromRoute] string id, [FromBody] ScanShortContentRequest request)
        {
            var topic = await _insightsService.GetTopicByIdAsync(id, SelectedChannelId);
            if (topic == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(request.ChannelName))
                return BadRequest("ChannelName is required");

            var result = await _insightIngestionService.ProcessShortContentScanAsync(topic, request.ChannelName, request.Videos, request.CommentsNumber, SelectedChannelId);
            return Ok(result);
        }

        [HttpPost("topics/{id}/analyze-comments")]
        [AllowUser(AuthorizationPermissionKeys.InsightsScan, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> AnalyzeComments([FromRoute] string id, [FromBody] AnalyzeInsightCommentsRequest request)
        {
            var topic = await _insightsService.GetTopicByIdAsync(id, SelectedChannelId);
            if (topic == null)
                return NotFound();

            if (request.SourceType != InsightCommentSourceType.DirectVideoId && string.IsNullOrWhiteSpace(request.ChannelId))
                return BadRequest("ChannelId is required for a stored channel source");
            if (request.SourceType != InsightCommentSourceType.DirectVideoId && request.ChannelId != SelectedChannelId)
                return NotFound();
            if (request.SourceType != InsightCommentSourceType.StoredChannel && string.IsNullOrWhiteSpace(request.VideoId))
                return BadRequest("VideoId is required for a video source");

            var result = await _insightIngestionService.AnalyzeCommentsAsync(topic, request, SelectedChannelId);
            return Ok(result);
        }

        [HttpGet("news")]
        [AllowUser(AuthorizationPermissionKeys.InsightsView, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> GetAllNews([FromQuery] InsightSourceKind? sourceKind = null)
        {
            var newsItems = await _insightsService.GetNewsItemsAsync(SelectedChannelId);

            if (sourceKind.HasValue)
            {
                newsItems = newsItems.Where(n => n.SourceKind == sourceKind.Value).ToList();
            }

            return Ok(newsItems.Select(ContractUtils.Convert));
        }

        [HttpGet("topics/{id}/news")]
        [AllowUser(AuthorizationPermissionKeys.InsightsView, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> GetNewsForTopic([FromRoute] string id, [FromQuery] InsightNewsStatus? status = null, [FromQuery] InsightSourceKind? sourceKind = null)
        {
            var topic = await _insightsService.GetTopicByIdAsync(id, SelectedChannelId);
            if (topic == null)
                return NotFound();

            var newsItems = await _insightsService.GetNewsItemsByTopicIdAsync(id, SelectedChannelId);

            if (status.HasValue)
            {
                newsItems = newsItems.Where(n => n.Status == status.Value).ToList();
            }

            if (sourceKind.HasValue)
            {
                newsItems = newsItems.Where(n => n.SourceKind == sourceKind.Value).ToList();
            }

            return Ok(newsItems.Select(ContractUtils.Convert));
        }

        [HttpGet("news/{id}")]
        [AllowUser(AuthorizationPermissionKeys.InsightsView, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> GetNewsById([FromRoute] string id)
        {
            var newsItem = await _insightsService.GetNewsItemByIdAsync(id, SelectedChannelId);
            if (newsItem == null)
                return NotFound();

            return Ok(ContractUtils.Convert(newsItem));
        }

        [HttpPut("news/{id}/review")]
        [AllowUser(AuthorizationPermissionKeys.InsightsUpdate, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> ReviewNewsItem([FromRoute] string id, [FromBody] ReviewNewsItemRequest request)
        {
            var newsItem = await _insightsService.GetNewsItemByIdAsync(id, SelectedChannelId);
            if (newsItem == null)
                return NotFound();

            var updated = newsItem;

            if (request.Status.HasValue)
            {
                updated = updated.UpdateStatus(request.Status.Value);
            }

            if (request.StarRating.HasValue)
            {
                updated = updated.UpdateStarRating(request.StarRating.Value);
            }

            var effectiveReason = request.Reason ?? updated.ReviewReason;
            if ((updated.Status == InsightNewsStatus.Accepted || updated.Status == InsightNewsStatus.Rejected) && string.IsNullOrWhiteSpace(effectiveReason))
            {
                return BadRequest("A reason is required when accepting or rejecting a news item.");
            }

            if (request.Reason != null)
            {
                updated = updated with { ReviewReason = request.Reason.Trim() };
            }

            await _insightsService.UpdateNewsItemAsync(updated, SelectedChannelId);
            return Ok(ContractUtils.Convert(updated));
        }

        [HttpDelete("news/{id}")]
        [AllowUser(AuthorizationPermissionKeys.InsightsDelete, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> DeleteNewsItem([FromRoute] string id)
        {
            var existing = await _insightsService.GetNewsItemByIdAsync(id, SelectedChannelId);
            if (existing == null)
                return NotFound();

            await _insightsService.DeleteNewsItemAsync(id, SelectedChannelId);
            return NoContent();
        }

        #endregion

        #region Content Plans

        [HttpPost("content-plans")]
        [AllowUser(AuthorizationPermissionKeys.InsightsCreate, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> GenerateContentPlan([FromBody] GenerateContentPlanRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TopicId) || request.NewsItemIds.Count == 0 || request.TargetPlatforms.Count == 0)
                return BadRequest("Topic, at least one news item, and at least one target platform are required.");

            var topic = await _insightsService.GetTopicByIdAsync(request.TopicId, SelectedChannelId);
            if (topic == null)
                return NotFound("Topic not found");

            var selectedNewsItems = new List<InsightNewsItem>();
            foreach (var newsItemId in request.NewsItemIds)
            {
                var newsItem = await _insightsService.GetNewsItemByIdAsync(newsItemId, SelectedChannelId);
                if (newsItem == null)
                    return NotFound($"News item {newsItemId} not found");
                if (newsItem.TopicId != request.TopicId)
                    return NotFound($"News item {newsItemId} not found for this topic");
                if (newsItem.Status != InsightNewsStatus.Accepted)
                    return BadRequest($"News item {newsItemId} must be accepted before generating a content plan.");
                selectedNewsItems.Add(newsItem);
            }

            var contentPlan = await _insightAgentService.GenerateContentPlanAsync(
                request.TopicId,
                request.NewsItemIds,
                request.ContentType,
                request.TargetPlatforms);

            await _insightsService.SaveContentPlanAsync(contentPlan, SelectedChannelId);

            // Mark news items as generated
            foreach (var newsItem in selectedNewsItems)
            {
                var updated = newsItem.UpdateStatus(InsightNewsStatus.Generated);
                await _insightsService.UpdateNewsItemAsync(updated, SelectedChannelId);
            }

            return CreatedAtAction(nameof(GetContentPlanById), new { id = contentPlan.Id }, ContractUtils.Convert(contentPlan));
        }

        [HttpGet("topics/{id}/export")]
        [AllowUser(AuthorizationPermissionKeys.InsightsView, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> ExportTopic([FromRoute] string id)
        {
            var topic = await _insightsService.GetTopicByIdAsync(id, SelectedChannelId);
            if (topic == null)
                return NotFound();

            var newsItems = await _insightsService.GetNewsItemsByTopicIdAsync(id, SelectedChannelId);
            var contentPlans = await _insightsService.GetContentPlansByTopicIdAsync(id, SelectedChannelId);
            var headers = new[] { "recordType", "topicId", "topicTitle", "topicDescription", "seedArguments", "preferredSources", "newsId", "newsTitle", "newsSummary", "newsStatus", "newsSourceUrl", "newsSourceName", "newsSourceKind", "newsAiRelevanceScore", "newsStarRating", "planId", "planTitle", "planType", "planOutline", "planTargetPlatforms" };
            var rows = new List<string[]> { headers };
            rows.Add(new[] { "topic", topic.Id ?? id, topic.Title, topic.Description, string.Join("; ", topic.SeedArguments), string.Join("; ", topic.PreferredSources), "", "", "", "", "", "", "", "", "", "", "", "", "", "" });
            rows.AddRange(newsItems.Select(item => new[] { "news", id, "", "", "", "", item.Id ?? "", item.Title, item.Summary, item.Status.ToString(), item.SourceUrl, item.SourceName, item.SourceKind.ToString(), item.AIRelevanceScore.ToString(CultureInfo.InvariantCulture), item.StarRating.ToString(CultureInfo.InvariantCulture), "", "", "", "", "" }));
            rows.AddRange(contentPlans.Select(plan => new[] { "contentPlan", id, "", "", "", "", "", "", "", "", "", "", "", "", "", plan.Id ?? "", plan.Title, plan.Type.ToString(), plan.Outline, string.Join("; ", plan.TargetPlatforms) }));

            var csv = string.Join("\r\n", rows.Select(row => string.Join(",", row.Select(EscapeCsv)))) + "\r\n";
            return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", $"insight-topic-{id}.csv");
        }

        private static string EscapeCsv(string value)
        {
            var escaped = value.Replace("\"", "\"\"");
            return escaped.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? $"\"{escaped}\"" : escaped;
        }

        [HttpGet("content-plans")]
        [AllowUser(AuthorizationPermissionKeys.InsightsView, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> GetAllContentPlans()
        {
            var plans = await _insightsService.GetContentPlansAsync(SelectedChannelId);
            return Ok(plans.Select(ContractUtils.Convert));
        }

        [HttpGet("topics/{id}/content-plans")]
        [AllowUser(AuthorizationPermissionKeys.InsightsView, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> GetContentPlansForTopic([FromRoute] string id)
        {
            var topic = await _insightsService.GetTopicByIdAsync(id, SelectedChannelId);
            if (topic == null)
                return NotFound();

            var plans = await _insightsService.GetContentPlansByTopicIdAsync(id, SelectedChannelId);
            return Ok(plans.Select(ContractUtils.Convert));
        }

        [HttpGet("content-plans/{id}")]
        [AllowUser(AuthorizationPermissionKeys.InsightsView, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> GetContentPlanById([FromRoute] string id)
        {
            var plan = await _insightsService.GetContentPlanByIdAsync(id, SelectedChannelId);
            if (plan == null)
                return NotFound();

            return Ok(ContractUtils.Convert(plan));
        }

        [HttpPut("content-plans/{id}")]
        [AllowUser(AuthorizationPermissionKeys.InsightsUpdate, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> UpdateContentPlan([FromRoute] string id, [FromBody] UpdateContentPlanRequest request)
        {
            var existing = await _insightsService.GetContentPlanByIdAsync(id, SelectedChannelId);
            if (existing == null)
                return NotFound();

            var updated = existing;

            if (!string.IsNullOrEmpty(request.Title))
            {
                updated = updated.UpdateTitle(request.Title);
            }

            if (!string.IsNullOrEmpty(request.Outline))
            {
                updated = updated.UpdateOutline(request.Outline);
            }

            if (request.TargetPlatforms != null)
            {
                updated = updated with { TargetPlatforms = request.TargetPlatforms };
            }

            await _insightsService.UpdateContentPlanAsync(updated, SelectedChannelId);
            return Ok(ContractUtils.Convert(updated));
        }

        [HttpDelete("content-plans/{id}")]
        [AllowUser(AuthorizationPermissionKeys.InsightsDelete, AuthorizationPermissionKeys.InsightsManage)]
        public async Task<IActionResult> DeleteContentPlan([FromRoute] string id)
        {
            var existing = await _insightsService.GetContentPlanByIdAsync(id, SelectedChannelId);
            if (existing == null)
                return NotFound();

            await _insightsService.DeleteContentPlanAsync(id, SelectedChannelId);
            return NoContent();
        }

        #endregion
    }

    #region Request DTOs

    public class CreateInsightTopicRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[]? SeedArguments { get; set; }
        public string[]? PreferredSources { get; set; }
    }

    public class UpdateInsightTopicRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string[]? SeedArguments { get; set; }
        public string[]? PreferredSources { get; set; }
    }

    public class ReviewNewsItemRequest
    {
        public InsightNewsStatus? Status { get; set; }
        public int? StarRating { get; set; }
        public string? Reason { get; set; }
    }

    public class GenerateContentPlanRequest
    {
        public string TopicId { get; set; } = string.Empty;
        public List<string> NewsItemIds { get; set; } = new();
        public ContentPlanType ContentType { get; set; }
        public List<string> TargetPlatforms { get; set; } = new();
    }

    public class UpdateContentPlanRequest
    {
        public string? Title { get; set; }
        public string? Outline { get; set; }
        public string[]? TargetPlatforms { get; set; }
    }

    #endregion
}