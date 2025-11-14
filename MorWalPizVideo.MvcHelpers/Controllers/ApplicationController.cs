using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MorWalPizVideo.BackOffice.Controllers;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MorWalPizVideo.Server.Controllers
{
    public abstract class ApplicationExternalController : ApplicationControllerBase, IDisposable
    {
        protected readonly IExternalDataService externalDataService;
        protected readonly IMorWalPizCache cache;
        protected ApplicationExternalController(IExternalDataService _extDataService, IMorWalPizCache _memoryCache)
        {
            externalDataService = _extDataService;
            cache = _memoryCache;

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
                return (await externalDataService.FetchMatches())
                            .OrderByDescending(x => x.CreationDateTime)
                            .ToList();
            })).Skip(skip).Take(take).ToList();
        }

        public virtual void Dispose()
        {
        }
    }
    public abstract class ApplicationController : ApplicationExternalController, IDisposable
    {   
        protected readonly DataService dataService;
        protected ApplicationController(IGenericDataService _dataService, IExternalDataService _extDataService, IMorWalPizCache _memoryCache)
             : base(_extDataService, _memoryCache)
        {
            var cast = _dataService as DataService;
            if (cast != null)
                dataService = cast;
            else
                throw new NullReferenceException($"DataService cannot be casted");
        }
        
        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
