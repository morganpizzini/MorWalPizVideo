using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using MorWalPizVideo.ShootingRange.Contracts;
using MorWalPizVideo.ShootingRange.Security;
using MorWalPizVideo.ShootingRange.Services;

namespace MorWalPizVideo.ShootingRange.Controllers;

[ApiController]
[Route("api/shooting-range/management/tenants")]
[Authorize(AuthenticationSchemes = ShootingRangeManagementAuthenticationHandler.AuthenticationScheme)]
public sealed class ShootingRangeTenantManagementController(
    ShootingRangeTenantProvisioningService service,
    IConfiguration configuration,
    IHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ShootingRangeTenantDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> FetchAll(CancellationToken cancellationToken) =>
        Ok(await service.FetchAllAsync(cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<ShootingRangeTenantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Fetch(string id, CancellationToken cancellationToken)
    {
        var tenant = await service.FetchAsync(id, cancellationToken);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpGet("local-settings")]
    [AllowAnonymous]
    [ProducesResponseType<ShootingRangeLocalTenantSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult FetchLocalSettings()
    {
        if (!environment.IsDevelopment())
            return NotFound();

        return Ok(new ShootingRangeLocalTenantSettingsDto(
            configuration["ShootingRange:LocalTenantSlug"] ?? "development",
            configuration["ShootingRange:LocalTenantOrigin"] ?? "http://localhost:5177"));
    }

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
