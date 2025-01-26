using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
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
        protected readonly MyMemoryCache memoryCache;
        protected ApplicationController(DataService _dataService, IExternalDataService _extDataService, MyMemoryCache _memoryCache)
        {
            dataService = _dataService;
            externalDataService = _extDataService;
            memoryCache = _memoryCache;

        }
        protected async Task<int> CountMatches()
        {
            if (memoryCache.Cache.TryGetValue(CacheKeys.Match, out IList<Match>? entities))
                return entities?.Count ?? 0;

            return (await this.FetchMatches()).Count;
        }
        protected async Task<IList<Match>> FetchMatches(int skip = 0, int take = int.MaxValue)
        {
            if (memoryCache.Cache.TryGetValue(CacheKeys.Match, out IList<Match>? entities))
                return entities ?? [];

            entities = (await externalDataService.FetchMatches())
                            .OrderByDescending(x => x.CreationDateTime)
                            .ToList();

            memoryCache.Cache.Set(CacheKeys.Match, entities, new MemoryCacheEntryOptions
            {
                Size = 1
            });

            return entities.Skip(skip).Take(take).ToList();
        }

        public virtual void Dispose()
        {
        }
    }
}
