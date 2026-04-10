using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Services;
namespace MorWalPizVideo.BackOffice.Controllers;

public class SponsorAppliesController : ApplicationControllerBase
{
    private readonly DataService _dataService;
    public SponsorAppliesController(DataService dataService)
    {
        _dataService = dataService;
    }
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _dataService.GetSponsorApplies();
        return Ok(result);
    }
}
