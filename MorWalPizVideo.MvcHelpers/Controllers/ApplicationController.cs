using Microsoft.Extensions.Caching.Memory;
using MorWalPizVideo.BackOffice.Controllers;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    public abstract class ApplicationController : ApplicationControllerBase
    {   
        protected readonly IGenericDataService dataService;
        protected readonly IMorWalPizCache cache;
        
        protected ApplicationController(IGenericDataService _dataService, IMorWalPizCache _memoryCache)
        {
            cache = _memoryCache;
            dataService = _dataService;
        }
        protected async Task<int> CountMatches()
        {
            var entities = cache.Get<IList<YouTubeContent>>(CacheKeys.Matches);
            if (entities != null)
                return entities?.Count ?? 0;

            return (await this.FetchMatches()).Count;
        }
        protected async Task<IList<YouTubeContent>> FetchMatches(int skip = 0, int take = int.MaxValue)
        {
            return (await cache.GetOrCreateAsync<IList<YouTubeContent>>(CacheKeys.Matches, async () =>
            {
                return (await dataService.FetchMatches())
                            .OrderByDescending(x => x.CreationDateTime)
                            .ToList();
            })).Skip(skip).Take(take).ToList();
        }
    }
}
