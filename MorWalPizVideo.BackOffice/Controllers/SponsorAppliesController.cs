using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Services;
namespace MorWalPizVideo.BackOffice.Controllers;

public class SponsorAppliesController : ApplicationController
{
    private readonly DataService dataService;
    public SponsorAppliesController(DataService _dataService)
    {
        dataService = _dataService;
    }
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await dataService.GetSponsorApplies();
        return Ok(result);
    }
}
