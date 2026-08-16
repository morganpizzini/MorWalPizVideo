using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MorWalPiz.Contracts;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [AllowAnonymous] // ADR-002: explicit public read access
    public class PagesController : ApplicationController
    {
        private readonly IPageService _pageService;
        public PagesController(
            IGenericDataService _dataService,
            IMorWalPizCache _memoryCache,
            IPageService pageService) : base(_dataService, _memoryCache)
        {
            _pageService = pageService;
        }

        [HttpGet("{url}")]
        [OutputCache(Tags = [CacheKeys.Pages], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> Detail(string url) {
            var page = await _pageService.GetPublishedByUrlAsync(url);
            return page is null ? NotFound() : Ok(ContractUtils.ConvertPublic(page));
        }
    }
}
