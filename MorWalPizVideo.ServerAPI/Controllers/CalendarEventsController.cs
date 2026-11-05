using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MorWalPiz.Contracts;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [AllowAnonymous] // ADR-002: explicit public read access
    public class CalendarEventsController : ApplicationController
    {
        private readonly ICalendarService _calendarService;
        private readonly IContentService _contentService;
        public CalendarEventsController(
            IMorWalPizCache _memoryCache,
            ICalendarService calendarService,
            IContentService contentService) : base(_memoryCache)
        {
            _calendarService = calendarService;
            _contentService = contentService;
        }

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.CalendarEvents])]
        public async Task<IActionResult> Index()
        {
            return Ok(await cache.GetOrCreateAsync(CacheKeys.CalendarEvents, async () =>
            {
                var elements = await _calendarService.GetRecentAsync(DateTime.Now.AddDays(-10), 250);
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
                    var videoId = match?.IsLink == true ? match.VideoRefs[0].YoutubeId : null;
                    var enriched = match == null ? entity : entity with { MatchUrl = videoId ?? match.Url };
                    return ContractUtils.ConvertPublic(enriched) with { MatchVideoId = videoId };
                }).ToList();
            }));
        }
    }
}
