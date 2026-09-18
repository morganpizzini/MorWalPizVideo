using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using SharedContractUtils = MorWalPiz.Contracts.ContractUtils;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Contracts;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.ServerAPI.Services;
using MorWalPizVideo.Domain;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [AllowAnonymous] // ADR-002: explicit public read access
    public class SponsorsController : ApplicationController
    {
        private readonly IRecaptchaService recaptchaService;
        private readonly BlobStorageOptions blobOptions;
        private readonly IConfiguration configuration;
        private readonly ILogger<SponsorsController> logger;
        public SponsorsController(
            IGenericDataService _dataService, IMorWalPizCache _memoryCache,
            IRecaptchaService _recaptchaService, IOptions<BlobStorageOptions> _blobOptions,
            IConfiguration _configuration, ILogger<SponsorsController> _logger) : base(_dataService,_memoryCache)
        {
            recaptchaService = _recaptchaService;
            blobOptions = _blobOptions.Value;
            configuration = _configuration;
            logger = _logger;
        }

        [HttpGet]
        [OutputCache(Tags = [ApiTagCacheKeys.Sponsors])]
        public async Task<IActionResult> Index()
        {
            var channelId = GetYouTubeChannelId();
            if (string.IsNullOrWhiteSpace(channelId))
                return Ok(Array.Empty<SponsorContract>());

            var entities = await cache.GetOrCreateAsync(CacheKeys.Sponsors, async () =>
            {
                var sponsors = await dataService.GetSponsors(channelId);
                var resolved = new List<Sponsor>(sponsors.Count);
                foreach (var sponsor in sponsors)
                {
                    var shortLink = string.IsNullOrWhiteSpace(sponsor.ShortLinkId)
                        ? null
                        : await dataService.GetShortLink(sponsor.ShortLinkId);
                    resolved.Add(shortLink is null || shortLink.SponsorId != sponsor.Id
                        ? sponsor
                        : sponsor with { Url = shortLink.Target });
                }
                return resolved;
            });
           
            return Ok(entities.Select(x => SharedContractUtils.Convert(x, $"{blobOptions.Endpoint}/{blobOptions.SponsorContainerName}")));
        }

        private string? GetYouTubeChannelId()
        {
            var channelId = configuration["YouTubeChannelId"]?.Trim();
            if (string.IsNullOrWhiteSpace(channelId))
                logger.LogError("Public sponsors endpoint is missing the YouTubeChannelId configuration");
            return channelId;
        }

        [HttpPost]
        public async Task<IActionResult> Create(SponsorRequest request)
        {
            var host = HttpContext.Request.Host.Value ?? string.Empty;
            var verified = await recaptchaService.VerifyAsync(request.Token, host, "sponsorForm");
            if (!verified)
            {
                return BadRequest("Recaptcha failed");
            }
            var sponsor = new SponsorApply(request.Name, request.Email, request.Description);
            await dataService.SaveSponsorApplies(sponsor);
            return NoContent();
        }
    }
}
