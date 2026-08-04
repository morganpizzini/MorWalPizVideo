using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [AllowAnonymous] // ADR-002: explicit public read access
    public class CompilationsController : ApplicationController
    {
        private readonly ICatalogService _catalogService;
        public CompilationsController(
            IGenericDataService _dataService, 
            IMorWalPizCache _memoryCache,
            ICatalogService catalogService) : base(_dataService, _memoryCache)
        {
            _catalogService = catalogService;
        }

        [HttpGet("{url}")]
        [OutputCache(Tags = [CacheKeys.Compilations], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> Detail(string url)
        {
            var compilation = await _catalogService.GetCompilationByUrlAsync(url);
            return compilation == null ? NotFound() : Ok(compilation);
        }
    }
}
