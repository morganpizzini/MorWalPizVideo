using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Responses;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    public class MatchesController : ApplicationController
    {
        private readonly IBlobService _blobService;
        public MatchesController(DataService _dataService,
            IExternalDataService _extDataService,
            IMorWalPizCache _memoryCache,
            IBlobService blobService) : base(_dataService, _extDataService, _memoryCache)
        {
            _blobService = blobService;
        }

        [OutputCache(Tags = [CacheKeys.Matches])]
        [HttpGet]
        public async Task<IActionResult> Index(int skip = 0, int take = 23) {
            var count = await CountMatches();
            var entities = await FetchMatches(skip, take);
            var next = skip > 0 ? take * skip : take;
            return Ok(new BaseResponse<IList<YouTubeContent>>(entities,count,$"skip={next}&take={take}"));
        }

        [HttpGet("{url}")]
        [OutputCache(Tags = [CacheKeys.Matches], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> Detail(string url)
        {
            var match = await FindMatch(url);
            return match == null ? NotFound() : Ok(match);
        }

        [HttpGet("{url}/images")]
        [OutputCache(Tags = [CacheKeys.Matches], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> FetchImages(string url)
        {
            var match = FindMatch(url);
            if (match == null)
                return NotFound();
            var images = await _blobService.GetImagesInFolderAsync(url);

            return Ok(images);
        }

        private async Task<YouTubeContent?> FindMatch(string url) => (await FetchMatches())?.FirstOrDefault(x => x.Url == url);
    }
}
