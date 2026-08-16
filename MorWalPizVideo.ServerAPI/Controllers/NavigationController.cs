using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using MorWalPiz.Contracts;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.ServerAPI.Controllers;

[AllowAnonymous]
public sealed class NavigationController(
    IGenericDataService dataService,
    IMorWalPizCache cache,
    IChannelNavigationService navigationService) : ApplicationController(dataService, cache)
{
    [HttpGet]
    [OutputCache(Tags = [CacheKeys.Navigation])]
    public async Task<IActionResult> Get()
    {
        try
        {
            var navigation = await navigationService.GetPublicAsync();
            return navigation is null ? Ok(null) : Ok(ContractUtils.Convert(navigation));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }
}