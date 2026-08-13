using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Configuration;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Responses;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [AllowAnonymous] // ADR-002: explicit public read access
    public class MatchesController : ApplicationController
    {
        private readonly IBlobService _blobService;
        private readonly IContentService _contentService;
        private readonly IConfiguration _configuration;
        public MatchesController(IGenericDataService _dataService,
            IMorWalPizCache _memoryCache,
            IBlobService blobService,
            IContentService contentService,
            IConfiguration configuration) : base(_dataService, _memoryCache)
        {
            _blobService = blobService;
            _contentService = contentService;
            _configuration = configuration;
        }

        [OutputCache(Tags = [CacheKeys.Matches])]
        [HttpGet]
        public async Task<IActionResult> Index(int skip = 0, int take = 23)
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var channelId = GetYouTubeChannelId();
            if (string.IsNullOrWhiteSpace(channelId))
            {
                return Ok(new BaseResponse<IList<YouTubeContent>>([], 0, string.Empty));
            }

            var normalChannel = (await _contentService.GetChannelsAsync())
                .FirstOrDefault(channel => channel.ChannelId == channelId);
            if (normalChannel == null)
            {
                return Ok(new BaseResponse<IList<YouTubeContent>>([], 0, string.Empty));
            }

            var count = await _contentService.CountPublicMatchesForChannelAsync(channelId, includePrivate: isAuthenticated);
            var entities = await _contentService.GetPublicMatchesForChannelAsync(channelId, includePrivate: isAuthenticated, skip: skip, take: take);
            var next = skip > 0 ? take * skip : take;
            return Ok(new BaseResponse<IList<YouTubeContent>>(entities, count, $"skip={next}&take={take}"));
        }

        [HttpGet("{url}")]
        [OutputCache(Tags = [CacheKeys.Matches], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> Detail(string url)
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var match = await _contentService.GetMatchByUrlAsync(url, includePrivate: isAuthenticated);
            if (match == null || !await IsCanonicalChannelMatchAsync(match)) return NotFound();
            return Ok(match);
        }

        [HttpGet("{url}/images")]
        [OutputCache(Tags = [CacheKeys.Matches], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> FetchImages(string url)
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var match = await _contentService.GetMatchByUrlAsync(url, includePrivate: isAuthenticated);
            if (match == null || !await IsCanonicalChannelMatchAsync(match))
                return NotFound();
            var images = await _blobService.GetImagesInFolderAsync(url);

            return Ok(images);
        }

        private async Task<bool> IsCanonicalChannelMatchAsync(YouTubeContent match)
        {
            var channelId = GetYouTubeChannelId();
            if (string.IsNullOrWhiteSpace(channelId))
            {
                return false;
            }

            var normalChannel = (await _contentService.GetChannelsAsync())
                .FirstOrDefault(channel => channel.ChannelId == channelId);
            return normalChannel != null && match.VideoRefs.Any(video => video.ChannelIds.Contains(channelId));
        }

        private string? GetYouTubeChannelId() => _configuration["YouTubeChannelId"]?.Trim();

    }
}
