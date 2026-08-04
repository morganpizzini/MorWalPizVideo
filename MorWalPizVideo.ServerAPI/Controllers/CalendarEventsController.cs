using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Services;
using YoutubeContentType = MorWalPizVideo.Server.Models.YoutubeContentType;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [AllowAnonymous] // ADR-002: explicit public read access
    public class CalendarEventsController : ApplicationController
    {
        private readonly ICatalogService _catalogService;
        private readonly IContentService _contentService;
        public CalendarEventsController(
            IGenericDataService _dataService,
            IMorWalPizCache _memoryCache,
            ICatalogService catalogService,
            IContentService contentService) : base(_dataService, _memoryCache)
        {
            _catalogService = catalogService;
            _contentService = contentService;
        }

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.CalendarEvents])]
        public async Task<IActionResult> Index()
        {
            return Ok(await cache.GetOrCreateAsync(CacheKeys.BioLinks, async () =>
            {
                var elements = await _catalogService.GetRecentCalendarEventsAsync(DateTime.Now.AddDays(-10), 250);
                var matchIds = elements
                    .Select(x => x.MatchId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                var matches = await _contentService.GetMatchesByIdsAsync(matchIds, includePrivate: false);
                var matchesById = matches.ToDictionary(x => x.Id, StringComparer.Ordinal);

                return elements.Select(entity =>
                {
                    matchesById.TryGetValue(entity.MatchId ?? string.Empty, out var match);
                    return match == null ? entity : entity with { MatchUrl = match.ContentType == YoutubeContentType.SingleVideo ? match.ContentId : match.Url };
                }).ToList();
            }));
        }
    }
}
