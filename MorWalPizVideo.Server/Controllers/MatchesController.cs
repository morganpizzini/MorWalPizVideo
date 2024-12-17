using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    public class MatchesController : ApplicationController
    {
        public MatchesController(
            DataService _dataService, IExternalDataService _extDataService, MyMemoryCache _memoryCache) : base(_dataService, _extDataService, _memoryCache)
        {
        }
        [OutputCache(Tags = [CacheKeys.Match])]
        [HttpGet]
        public async Task<IActionResult> Index() => Ok(await FetchMatches());

        [HttpGet("{url}")]
        [OutputCache(Tags = [CacheKeys.Match],VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> Detail(string url) =>
            Ok((await FetchMatches())?.FirstOrDefault(x => x.Url == url));
    }
}
