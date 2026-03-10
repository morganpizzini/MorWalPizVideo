using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    public class CompilationsController : ApplicationController
    {
        public CompilationsController(
            IGenericDataService _dataService, 
            IMorWalPizCache _memoryCache) : base(_dataService, _memoryCache)
        {
        }

        [HttpGet("{url}")]
        [OutputCache(Tags = [CacheKeys.Compilations], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> Detail(string url)
        {
            var compilation = await FindCompilation(url);
            return compilation == null ? NotFound() : Ok(compilation);
        }

        private async Task<Compilation?> FindCompilation(string url) => 
            (await cache.GetOrCreateAsync(CacheKeys.Compilations, dataService.GetCompilations))
                ?.FirstOrDefault(x => x.Url == url);
    }
}
