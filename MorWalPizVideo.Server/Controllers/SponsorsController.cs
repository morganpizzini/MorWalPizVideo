using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Contracts;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{

    public class SponsorsController : ApplicationController
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;
        private readonly BlobStorageOptions blobOptions;
        public SponsorsController(
            DataService _dataService, IExternalDataService _extDataService, MyMemoryCache _memoryCache,
            IHttpClientFactory _httpClientFactory, IConfiguration _configuration, IOptions<BlobStorageOptions> _blobOptions) : base(_dataService,_extDataService,_memoryCache)
        {
            configuration = _configuration;
            httpClientFactory = _httpClientFactory;
            blobOptions = _blobOptions.Value;
        }

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.Sponsors])]
        public async Task<IActionResult> Index()
        {
            if (memoryCache.Cache.TryGetValue(CacheKeys.Sponsors, out IList<Sponsor>? entities))
            {
                return Ok(entities);
            }
            entities = await dataService.GetSponsors();
            memoryCache.Cache.Set(CacheKeys.Sponsors, entities, new MemoryCacheEntryOptions
            {
                Size = 1
            });
            return Ok(entities.Select(x=>(x with { ImgSrc = $"{blobOptions.Endpoint}/{blobOptions.SponsorContainerName}/{x.ImgSrc}" })));
        }

        [HttpPost]
        public async Task<IActionResult> Create(SponsorRequest request)
        {
            using var client = httpClientFactory.CreateClient("Recaptcha");
            var host = HttpContext.Request.Host.Value;
            var parameters = new Dictionary<string, string> { { "secret", configuration["RecaptchaSecretKey"] ?? string.Empty }, { "response", request.Token }, { "remoteip", host } };
            var encodedContent = new FormUrlEncodedContent(parameters);

            var response = await client.PostAsync("", encodedContent);
            var result = System.Text.Json.JsonSerializer.Deserialize<RecaptchaResponse>(await response.Content.ReadAsStringAsync());
            if (result == null || !result.success || result.action != "sponsorForm")
            {
                return BadRequest("Recaptcha failed");
            }
            var sponsor = new SponsorApply(request.Name, request.Email, request.Description);
            await dataService.SaveSponsorApplies(sponsor);
            return NoContent();
        }
    }
}
