using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using SharedContractUtils = MorWalPiz.Contracts.ContractUtils;
using MorWalPiz.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Contracts;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.ServerAPI.Services;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [AllowAnonymous] // ADR-002: explicit public read access
    public class SponsorsController : ApplicationController
    {
        private readonly IRecaptchaService recaptchaService;
        private readonly BlobStorageOptions blobOptions;
        public SponsorsController(
            IGenericDataService _dataService, IMorWalPizCache _memoryCache,
            IRecaptchaService _recaptchaService, IOptions<BlobStorageOptions> _blobOptions) : base(_dataService,_memoryCache)
        {
            recaptchaService = _recaptchaService;
            blobOptions = _blobOptions.Value;
        }

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.Sponsors])]
        public async Task<IActionResult> Index()
        {
            var entities = await cache.GetOrCreateAsync(CacheKeys.Sponsors, dataService.GetSponsors);
           
            return Ok(entities.Select(x => SharedContractUtils.Convert(x, $"{blobOptions.Endpoint}/{blobOptions.SponsorContainerName}")));
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
