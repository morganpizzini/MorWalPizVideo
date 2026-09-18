using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [AllowAnonymous] // ADR-002: explicit public read access
    public class ProductsController : ApplicationController
    {
        public ProductsController(
            IGenericDataService dataService,
            IMorWalPizCache memoryCache,
            IConfiguration configuration,
            ILogger<ProductsController> logger) : base(dataService, memoryCache)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private readonly IConfiguration _configuration;
        private readonly ILogger<ProductsController> _logger;

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.Products])]
        public async Task<IActionResult> Index()
        {
            var channelId = GetYouTubeChannelId();
            if (string.IsNullOrWhiteSpace(channelId))
                return Ok(Array.Empty<ProductContract>());

            var entities = await dataService.GetProducts(channelId);
            return Ok(entities.Select(ContractUtils.Convert));
        }

        private string? GetYouTubeChannelId()
        {
            var channelId = _configuration["YouTubeChannelId"]?.Trim();
            if (string.IsNullOrWhiteSpace(channelId))
                _logger.LogError("Public products endpoint is missing the YouTubeChannelId configuration");

            return channelId;
        }
    }
}
