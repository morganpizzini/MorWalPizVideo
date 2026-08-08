using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Controllers;

[Route("api/dashboard")]
[AllowUser("perm:" + AuthorizationPermissionKeys.BackofficeAccess)]
[RequireChannelScope]
public sealed class DashboardController(IDashboardService dashboardService) : ApplicationControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary() => Ok(await dashboardService.GetSummaryAsync(HttpContext.GetChannelContext().ChannelId));

    [HttpGet("video-publications")]
    public async Task<IActionResult> VideoPublications([FromQuery] int days = 21)
        => Ok(await dashboardService.GetVideoPublicationsAsync(days, HttpContext.GetChannelContext().ChannelId));
}