using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Services;
using YoutubeContentType = MorWalPizVideo.Server.Models.YoutubeContentType;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    public class CalendarEventsController : ApplicationController
    {
        public CalendarEventsController(
            IGenericDataService _dataService, IMorWalPizCache _memoryCache) : base(_dataService, _memoryCache)
        {
        }

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.CalendarEvents])]
        public async Task<IActionResult> Index()
        {
            return Ok(await cache.GetOrCreateAsync(CacheKeys.BioLinks, async () =>
            {
                var elements = (await dataService.GetCalendarEvents())
                .Where(x => x.CreationDateTime >= DateTime.Now.AddDays(-10))
                .OrderByDescending(x => x.CreationDateTime).ToList();

                var matches = await FetchMatches();

                return elements.Select(entity =>
                {
                    var match = matches.FirstOrDefault(x => x.Id == entity.MatchId);
                    return match == null ? entity : entity with { MatchUrl = match.ContentType == YoutubeContentType.SingleVideo ? match.ContentId : match.Url };
                }).ToList();
            }));
        }
    }
}
