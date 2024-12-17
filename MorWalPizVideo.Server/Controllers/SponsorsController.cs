using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
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
        public SponsorsController(
            DataService _dataService, IExternalDataService _extDataService, MyMemoryCache _memoryCache,
            IHttpClientFactory _httpClientFactory, IConfiguration _configuration) : base(_dataService,_extDataService,_memoryCache)
        {
            configuration = _configuration;
            httpClientFactory = _httpClientFactory;
        }

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.Sponsor])]
        public async Task<IActionResult> Index()
        {
            if (memoryCache.Cache.TryGetValue(CacheKeys.Sponsor, out IList<Sponsor>? entities))
            {
                return Ok(entities);
            }
            entities = await dataService.GetSponsors();
            memoryCache.Cache.Set(CacheKeys.Sponsor, entities, new MemoryCacheEntryOptions
            {
                Size = 1
            });
            return Ok(entities);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SponsorRequest request)
        {
            using var client = httpClientFactory.CreateClient();
            var host = HttpContext.Request.Host.Value;
            var url = "https://www.google.com/recaptcha/api/siteverify";
            var parameters = new Dictionary<string, string> { { "secret", configuration["RecaptchaSecretKey"] ?? string.Empty }, { "response", request.Token }, { "remoteip", host } };
            var encodedContent = new FormUrlEncodedContent(parameters);

            var response = await client.PostAsync(url, encodedContent);
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
