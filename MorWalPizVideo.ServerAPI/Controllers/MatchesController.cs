using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Responses;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [AllowAnonymous] // ADR-002: explicit public read access
    public class MatchesController : ApplicationController
    {
        private readonly IBlobService _blobService;
        private readonly IContentService _contentService;
        public MatchesController(IGenericDataService _dataService,
            IMorWalPizCache _memoryCache,
            IBlobService blobService,
            IContentService contentService) : base(_dataService, _memoryCache)
        {
            _blobService = blobService;
            _contentService = contentService;
        }

        [OutputCache(Tags = [CacheKeys.Matches])]
        [HttpGet]
        public async Task<IActionResult> Index(int skip = 0, int take = 23)
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var count = await _contentService.CountMatchesAsync(includePrivate: isAuthenticated);
            var entities = await _contentService.GetMatchesPageAsync(includePrivate: isAuthenticated, skip: skip, take: take);
            var next = skip > 0 ? take * skip : take;
            return Ok(new BaseResponse<IList<YouTubeContent>>(entities, count, $"skip={next}&take={take}"));
        }

        [HttpGet("{url}")]
        [OutputCache(Tags = [CacheKeys.Matches], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> Detail(string url)
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var match = await _contentService.GetMatchByUrlAsync(url, includePrivate: isAuthenticated);
            if (match == null) return NotFound();
            return Ok(match);
        }

        [HttpGet("{url}/images")]
        [OutputCache(Tags = [CacheKeys.Matches], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> FetchImages(string url)
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var match = await _contentService.GetMatchByUrlAsync(url, includePrivate: isAuthenticated);
            if (match == null)
                return NotFound();
            var images = await _blobService.GetImagesInFolderAsync(url);

            return Ok(images);
        }

    }
}
