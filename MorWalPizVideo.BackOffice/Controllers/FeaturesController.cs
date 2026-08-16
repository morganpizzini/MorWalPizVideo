using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Utils;

namespace MorWalPizVideo.BackOffice.Controllers;

public sealed class FeaturesController(IFeatureManager featureManager) : ApplicationControllerBase
{
    [HttpGet]
    [AllowUser("perm:" + AuthorizationPermissionKeys.BackofficeAccess)]
    public async Task<ActionResult<FeatureStateResponse>> Get()
    {
        return Ok(new FeatureStateResponse(
            await featureManager.IsEnabledAsync(MyFeatureFlags.EnableVideoBulkImport)));
    }
}