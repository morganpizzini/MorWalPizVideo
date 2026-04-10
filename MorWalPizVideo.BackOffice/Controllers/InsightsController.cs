using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers
{
    public class InsightsController : ApplicationControllerBase
    {
        private readonly DataService _dataService;
        private readonly IInsightAgentService _insightAgentService;

        public InsightsController(DataService dataService, IInsightAgentService insightAgentService)
        {
            _dataService = dataService;
            _insightAgentService = insightAgentService;
        }

        #region Topics

        [HttpGet("topics")]
        public async Task<IActionResult> GetTopics()
        {
            var topics = await _dataService.GetInsightTopics();
            return Ok(topics);
        }

        [HttpGet("topics/{id}")]
        public async Task<IActionResult> GetTopicById([FromRoute] string id)
        {
            var topic = await _dataService.GetInsightTopicById(id);
            if (topic == null)
                return NotFound();

            return Ok(topic);
        }

        [HttpPost("topics")]
        public async Task<IActionResult> CreateTopic([FromBody] CreateInsightTopicRequest request)
        {
            var topic = new InsightTopic(
                title: request.Title,
                description: request.Description,
                seedArguments: request.SeedArguments ?? Array.Empty<string>(),
                preferredSources: request.PreferredSources ?? Array.Empty<string>()
            )
            {
                Id = Guid.NewGuid().ToString()
            };

            await _dataService.SaveInsightTopic(topic);
            return CreatedAtAction(nameof(GetTopicById), new { id = topic.Id }, topic);
        }

        [HttpPut("topics/{id}")]
        public async Task<IActionResult> UpdateTopic([FromRoute] string id, [FromBody] UpdateInsightTopicRequest request)
        {
            var existing = await _dataService.GetInsightTopicById(id);
            if (existing == null)
                return NotFound();

            var updated = existing with
            {
                Title = request.Title ?? existing.Title,
                Description = request.Description ?? existing.Description,
                SeedArguments = request.SeedArguments ?? existing.SeedArguments,
                PreferredSources = request.PreferredSources ?? existing.PreferredSources
            };

            await _dataService.UpdateInsightTopic(updated);
            return Ok(updated);
        }

        [HttpDelete("topics/{id}")]
        public async Task<IActionResult> DeleteTopic([FromRoute] string id)
        {
            var existing = await _dataService.GetInsightTopicById(id);
            if (existing == null)
                return NotFound();

            await _dataService.DeleteInsightTopic(id);
            return NoContent();
        }

        #endregion

        #region News Discovery

        [HttpPost("topics/{id}/scan-news")]
        public async Task<IActionResult> ScanNewsForTopic([FromRoute] string id)
        {
            var topic = await _dataService.GetInsightTopicById(id);
            if (topic == null)
                return NotFound();

            var discoveredNews = await _insightAgentService.DiscoverNewsAsync(topic);

            // Save discovered news items
            foreach (var newsItem in discoveredNews)
            {
                await _dataService.SaveInsightNewsItem(newsItem);
            }

            // Rank the items
            var rankedNews = await _insightAgentService.RankNewsItemsAsync(discoveredNews);

            return Ok(rankedNews);
        }

        [HttpGet("news")]
        public async Task<IActionResult> GetAllNews()
        {
            var newsItems = await _dataService.GetInsightNewsItems();
            return Ok(newsItems);
        }

        [HttpGet("topics/{id}/news")]
        public async Task<IActionResult> GetNewsForTopic([FromRoute] string id, [FromQuery] InsightNewsStatus? status = null)
        {
            var topic = await _dataService.GetInsightTopicById(id);
            if (topic == null)
                return NotFound();

            var newsItems = await _dataService.GetInsightNewsItemsByTopicId(id);

            if (status.HasValue)
            {
                newsItems = newsItems.Where(n => n.Status == status.Value).ToList();
            }

            return Ok(newsItems);
        }

        [HttpGet("news/{id}")]
        public async Task<IActionResult> GetNewsById([FromRoute] string id)
        {
            var newsItem = await _dataService.GetInsightNewsItemById(id);
            if (newsItem == null)
                return NotFound();

            return Ok(newsItem);
        }

        [HttpPut("news/{id}/review")]
        public async Task<IActionResult> ReviewNewsItem([FromRoute] string id, [FromBody] ReviewNewsItemRequest request)
        {
            var newsItem = await _dataService.GetInsightNewsItemById(id);
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

            await _dataService.UpdateInsightNewsItem(updated);
            return Ok(updated);
        }

        [HttpDelete("news/{id}")]
        public async Task<IActionResult> DeleteNewsItem([FromRoute] string id)
        {
            var existing = await _dataService.GetInsightNewsItemById(id);
            if (existing == null)
                return NotFound();

            await _dataService.DeleteInsightNewsItem(id);
            return NoContent();
        }

        #endregion

        #region Content Plans

        [HttpPost("content-plans")]
        public async Task<IActionResult> GenerateContentPlan([FromBody] GenerateContentPlanRequest request)
        {
            var topic = await _dataService.GetInsightTopicById(request.TopicId);
            if (topic == null)
                return NotFound("Topic not found");

            // Verify all news items exist
            foreach (var newsItemId in request.NewsItemIds)
            {
                var newsItem = await _dataService.GetInsightNewsItemById(newsItemId);
                if (newsItem == null)
                    return NotFound($"News item {newsItemId} not found");
            }

            var contentPlan = await _insightAgentService.GenerateContentPlanAsync(
                request.TopicId,
                request.NewsItemIds,
                request.ContentType,
                request.TargetPlatforms);

            await _dataService.SaveInsightContentPlan(contentPlan);

            // Mark news items as generated
            foreach (var newsItemId in request.NewsItemIds)
            {
                var newsItem = await _dataService.GetInsightNewsItemById(newsItemId);
                if (newsItem != null)
                {
                    var updated = newsItem.UpdateStatus(InsightNewsStatus.Generated);
                    await _dataService.UpdateInsightNewsItem(updated);
                }
            }

            return CreatedAtAction(nameof(GetContentPlanById), new { id = contentPlan.Id }, contentPlan);
        }

        [HttpGet("content-plans")]
        public async Task<IActionResult> GetAllContentPlans()
        {
            var plans = await _dataService.GetInsightContentPlans();
            return Ok(plans);
        }

        [HttpGet("topics/{id}/content-plans")]
        public async Task<IActionResult> GetContentPlansForTopic([FromRoute] string id)
        {
            var topic = await _dataService.GetInsightTopicById(id);
            if (topic == null)
                return NotFound();

            var plans = await _dataService.GetInsightContentPlansByTopicId(id);
            return Ok(plans);
        }

        [HttpGet("content-plans/{id}")]
        public async Task<IActionResult> GetContentPlanById([FromRoute] string id)
        {
            var plan = await _dataService.GetInsightContentPlanById(id);
            if (plan == null)
                return NotFound();

            return Ok(plan);
        }

        [HttpPut("content-plans/{id}")]
        public async Task<IActionResult> UpdateContentPlan([FromRoute] string id, [FromBody] UpdateContentPlanRequest request)
        {
            var existing = await _dataService.GetInsightContentPlanById(id);
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

            await _dataService.UpdateInsightContentPlan(updated);
            return Ok(updated);
        }

        [HttpDelete("content-plans/{id}")]
        public async Task<IActionResult> DeleteContentPlan([FromRoute] string id)
        {
            var existing = await _dataService.GetInsightContentPlanById(id);
            if (existing == null)
                return NotFound();

            await _dataService.DeleteInsightContentPlan(id);
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