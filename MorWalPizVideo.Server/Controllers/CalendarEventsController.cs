using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    public class CalendarEventsController : ApplicationController
    {
        public CalendarEventsController(
            DataService _dataService, IExternalDataService _extDataService, MyMemoryCache _memoryCache) : base(_dataService, _extDataService, _memoryCache)
        {
        }

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.CalendarEvents])]
        public async Task<IActionResult> Index()
        {
            if (memoryCache.Cache.TryGetValue(CacheKeys.CalendarEvents, out IList<CalendarEvent>? entities))
            {
                return Ok(entities);
            }
            entities = (await dataService.GetCalendarEvents()).OrderBy(x => x.Date).ToList();

            var matches = await FetchMatches();

            entities = entities.Select(entity =>
            {
                var match = matches.FirstOrDefault(x => x.MatchId == entity.MatchId);
                return match == null ? entity : entity with { MatchUrl = match.IsLink ? match.ThumbnailUrl : match.Url };
            }).Where(x => !string.IsNullOrEmpty(x.MatchUrl)).ToList();

            memoryCache.Cache.Set(CacheKeys.CalendarEvents, entities, new MemoryCacheEntryOptions
            {
                Size = 1
            });

            return Ok(entities);
        }
    }
}
