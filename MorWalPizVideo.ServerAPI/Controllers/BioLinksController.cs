using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [AllowAnonymous] // ADR-002: explicit public read access
    public class BioLinksController : ApplicationController
    {
        public BioLinksController(
            IGenericDataService _dataService, IMorWalPizCache _memoryCache) : base(_dataService, _memoryCache)
        {
        }

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.BioLinks])]
        public async Task<IActionResult> Index()
        {
            var entities = await cache.GetOrCreateAsync(CacheKeys.BioLinks, dataService.GetBioLinks);
            return Ok(entities);
        }
    }
}
