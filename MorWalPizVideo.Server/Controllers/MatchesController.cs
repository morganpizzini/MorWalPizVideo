using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    public class MatchesController : ApplicationController
    {
        private readonly IBlobService _blobService;
        public MatchesController(DataService _dataService,
            IExternalDataService _extDataService,
            MyMemoryCache _memoryCache,
            IBlobService blobService) : base(_dataService, _extDataService, _memoryCache)
        {
            _blobService = blobService;
        }

        [OutputCache(Tags = [CacheKeys.Match])]
        [HttpGet]
        public async Task<IActionResult> Index() => Ok(await FetchMatches());

        [HttpGet("{url}")]
        [OutputCache(Tags = [CacheKeys.Match], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> Detail(string url)
        {
            var match = await FindMatch(url);
            return match == null ? NotFound() : Ok(match);
        }

        [HttpGet("{url}/images")]
        [OutputCache(Tags = [CacheKeys.Match], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> FetchImages(string url)
        {
            var match = FindMatch(url);
            if (match == null)
                return NotFound();
            var images = await _blobService.GetImagesInFolderAsync(url);

            return Ok(images);
        }

        private async Task<Match?> FindMatch(string url) => (await FetchMatches())?.FirstOrDefault(x => x.Url == url);
    }
}
