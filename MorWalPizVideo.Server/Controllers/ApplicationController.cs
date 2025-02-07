using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class ApplicationController : ControllerBase, IDisposable
    {   
        protected readonly IExternalDataService externalDataService;
        protected readonly DataService dataService;
        protected readonly IMorWalPizCache cache;
        protected ApplicationController(DataService _dataService, IExternalDataService _extDataService, IMorWalPizCache _memoryCache)
        {
            dataService = _dataService;
            externalDataService = _extDataService;
            cache = _memoryCache;

        }
        protected async Task<int> CountMatches()
        {
            var entities = cache.Get<IList<Match>>(CacheKeys.Matches);
            if (entities!= null)
                return entities?.Count ?? 0;

            return (await this.FetchMatches()).Count;
        }
        protected async Task<IList<Match>> FetchMatches(int skip = 0, int take = int.MaxValue)
        {
            return (await cache.GetOrCreateAsync<IList<Match>>(CacheKeys.Matches, async () =>
            {
                return (await externalDataService.FetchMatches())
                            .OrderByDescending(x => x.CreationDateTime)
                            .ToList();
            })).Skip(skip).Take(take).ToList();
        }

        public virtual void Dispose()
        {
        }
    }
}
