using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.ServerAPI.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/competitions")]
public sealed class CompetitionV1Controller(
    IGenericDataService dataService,
    IMorWalPizCache cache) : ControllerBase
{
    [HttpGet]
    [OutputCache(Tags = [CacheKeys.Competitions])]
    public async Task<ActionResult<IReadOnlyList<CompetitionContract>>> Index()
    {
        var entities = await cache.GetOrCreateAsync(CacheKeys.Competitions, dataService.GetCompetitions);
        return Ok(entities.Select(ContractUtils.Convert).ToArray());
    }

    [HttpGet("open")]
    [OutputCache(Tags = [CacheKeys.Competitions])]
    public async Task<ActionResult<IReadOnlyList<CompetitionContract>>> Open()
    {
        var entities = await dataService.GetCompetitionsByStatus(CompetitionStatus.RegistrationOpen);
        return Ok(entities.Select(ContractUtils.Convert).ToArray());
    }

    [HttpGet("{id}")]
    [OutputCache(Tags = [CacheKeys.Competitions], VaryByRouteValueNames = ["id"])]
    public async Task<ActionResult<CompetitionContract>> Detail(string id)
    {
        var entity = await dataService.GetCompetitionById(id);
        return entity is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Competition not found",
                detail: $"No competition was found for id '{id}'.")
            : Ok(ContractUtils.Convert(entity));
    }
}
