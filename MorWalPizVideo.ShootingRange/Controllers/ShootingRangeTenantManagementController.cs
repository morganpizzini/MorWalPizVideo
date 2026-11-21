using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MorWalPizVideo.ShootingRange.Contracts;
using MorWalPizVideo.ShootingRange.Security;
using MorWalPizVideo.ShootingRange.Services;

namespace MorWalPizVideo.ShootingRange.Controllers;

[ApiController]
[Route("api/shooting-range/management/tenants")]
[Authorize(AuthenticationSchemes = ShootingRangeManagementAuthenticationHandler.AuthenticationScheme)]
public sealed class ShootingRangeTenantManagementController(ShootingRangeTenantProvisioningService service) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("range-tenant-provision")]
    [ProducesResponseType<ShootingRangeTenantDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        CreateShootingRangeTenantRequest request,
        CancellationToken cancellationToken)
    {
        var tenant = await service.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, tenant);
    }
}
