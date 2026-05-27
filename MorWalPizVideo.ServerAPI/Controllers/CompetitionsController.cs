using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    public class CompetitionsController : ApplicationController
    {
        public CompetitionsController(
            IGenericDataService dataService,
            IMorWalPizCache memoryCache) : base(dataService, memoryCache)
        {
        }

        [HttpGet]
        [OutputCache(Tags = [CacheKeys.Competitions])]
        public async Task<IActionResult> Index()
        {
            var entities = await cache.GetOrCreateAsync(CacheKeys.Competitions, dataService.GetCompetitions);
            return Ok(entities);
        }

        [HttpGet("open")]
        [OutputCache(Tags = [CacheKeys.Competitions])]
        public async Task<IActionResult> Open()
        {
            var entities = await dataService.GetCompetitionsByStatus(CompetitionStatus.RegistrationOpen);
            return Ok(entities);
        }

        [HttpGet("{id}")]
        [OutputCache(Tags = [CacheKeys.Competitions], VaryByRouteValueNames = ["id"])]
        public async Task<IActionResult> Detail(string id)
        {
            var entity = await dataService.GetCompetitionById(id);
            return entity == null ? NotFound() : Ok(entity);
        }
    }
}
