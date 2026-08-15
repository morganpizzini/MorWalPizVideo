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
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [AllowAnonymous] // ADR-002: explicit public read access
    public class MatchesController : ApplicationController
    {
        private readonly IBlobService _blobService;
        private readonly IContentService _contentService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MatchesController> _logger;
        public MatchesController(IGenericDataService _dataService,
            IMorWalPizCache _memoryCache,
            IBlobService blobService,
            IContentService contentService,
            IConfiguration configuration,
            ILogger<MatchesController> logger) : base(_dataService, _memoryCache)
        {
            _blobService = blobService;
            _contentService = contentService;
            _configuration = configuration;
            _logger = logger;
        }

        [OutputCache(Tags = [CacheKeys.Matches], VaryByQueryKeys = ["skip", "take"])]
        [HttpGet]
        public async Task<IActionResult> Index(int skip = 0, int take = 23)
        {
            var channelId = GetYouTubeChannelId();
            if (string.IsNullOrWhiteSpace(channelId))
            {
                return Ok(new BaseResponse<IList<YouTubeContent>>([], 0, string.Empty));
            }

            var normalChannel = await _contentService.GetChannelByIdAsync(channelId);
            if (normalChannel == null)
            {
                return Ok(new BaseResponse<IList<YouTubeContent>>([], 0, string.Empty));
            }

            var safeSkip = Math.Max(0, skip);
            var safeTake = Math.Clamp(take, 1, 200);
            var count = await _contentService.CountPublicMatchesForChannelAsync(channelId);
            var entities = await _contentService.GetPublicMatchesForChannelAsync(channelId, safeSkip, safeTake);
            var next = safeSkip + safeTake;
            return Ok(new BaseResponse<IList<PublicYouTubeContentContract>>(
                entities.Select(ContractUtils.ConvertPublic).ToList(), count, $"skip={next}&take={safeTake}"));
        }

        [HttpGet("{url}")]
        [OutputCache(Tags = [CacheKeys.Matches], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> Detail(string url)
        {
            var match = await _contentService.GetMatchByUrlAsync(url, includePrivate: false);
            if (match == null || !await IsCanonicalChannelMatchAsync(match)) return NotFound();
            return Ok(ContractUtils.ConvertPublic(match));
        }

        [HttpGet("{url}/images")]
        [OutputCache(Tags = [CacheKeys.Matches], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> FetchImages(string url)
        {
            var match = await _contentService.GetMatchByUrlAsync(url, includePrivate: false);
            if (match == null || !await IsCanonicalChannelMatchAsync(match))
                return NotFound();
            var images = await _blobService.GetImagesInFolderAsync(url, HttpContext.RequestAborted);

            return Ok(images);
        }

        private async Task<bool> IsCanonicalChannelMatchAsync(YouTubeContent match)
        {
            var channelId = GetYouTubeChannelId();
            if (string.IsNullOrWhiteSpace(channelId))
            {
                return false;
            }

            var normalChannel = await _contentService.GetChannelByIdAsync(channelId);
            return normalChannel != null && match.VideoRefs.Any(video => video.ChannelIds.Contains(channelId));
        }

        private string? GetYouTubeChannelId()
        {
            var channelId = _configuration["YouTubeChannelId"]?.Trim();
            if (string.IsNullOrWhiteSpace(channelId))
            {
                _logger.LogError("Public matches endpoint is missing the YouTubeChannelId configuration");
            }

            return channelId;
        }

    }
}
