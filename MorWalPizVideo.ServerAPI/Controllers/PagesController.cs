using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [AllowAnonymous] // ADR-002: explicit public read access
    public class PagesController : ApplicationController
    {
        private readonly ICatalogService _catalogService;
        private readonly BlobStorageOptions blobOptions;
        public PagesController(
            IGenericDataService _dataService,
            IMorWalPizCache _memoryCache,
            ICatalogService catalogService,
            IOptions<BlobStorageOptions> _blobOptions) : base(_dataService, _memoryCache)
        {
            _catalogService = catalogService;
            blobOptions = _blobOptions.Value;
        }

        [HttpGet("{url}")]
        [OutputCache(Tags = [CacheKeys.Pages], VaryByRouteValueNames = ["url"])]
        public async Task<IActionResult> Detail(string url) {

            var page = await _catalogService.GetPageByUrlAsync(url);
            if(page == null)
                return NotFound();
            return Ok(page with { ThumbnailUrl = $"{blobOptions.Endpoint}/{blobOptions.PageContainerName}/{page.Url}/thumbnail.jpg" });
        }
    }
}
