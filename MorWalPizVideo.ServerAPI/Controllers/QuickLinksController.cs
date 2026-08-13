using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MorWalPiz.Contracts;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.ServerAPI.Controllers;

[AllowAnonymous]
public sealed class QuickLinksController(
    IGenericDataService dataService,
    IMorWalPizCache cache,
    IQuickLinksService quickLinksService) : ApplicationController(dataService, cache)
{
    [HttpGet("{url}")]
    [OutputCache(Tags = [CacheKeys.QuickLinks], VaryByRouteValueNames = ["url"])]
    public async Task<IActionResult> Detail(string url)
    {
        var entity = await quickLinksService.GetByUrlAsync(url);
        return entity is null ? NotFound() : Ok(ContractUtils.ConvertPublic(entity));
    }
}
