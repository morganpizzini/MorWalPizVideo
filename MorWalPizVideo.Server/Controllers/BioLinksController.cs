﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.Server.Controllers
{
    public class BioLinksController : ApplicationController
    {
        public BioLinksController(
            DataService _dataService, IExternalDataService _extDataService, IMorWalPizCache _memoryCache) : base(_dataService, _extDataService, _memoryCache)
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
