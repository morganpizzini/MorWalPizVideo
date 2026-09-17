using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/social-assets")]
[ApiKeyAuth]
[RequireChannelScope]
public sealed class SocialAssetsController(SocialAssetService assetService) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<ActionResult<SocialAssetContract>> Upload(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (!User.Claims.Any(claim => claim.Type == "scope" &&
            string.Equals(claim.Value, AuthorizationPermissionKeys.SocialAssetsUpload, StringComparison.OrdinalIgnoreCase)))
            return Forbid();
        if (file is null)
            return BadRequest(new { code = "file_required", message = "A multipart file is required." });

        try
        {
            var result = await assetService.UploadAsync(
                HttpContext.GetChannelContext().ChannelId,
                idempotencyKey ?? string.Empty,
                file,
                cancellationToken);
            if (result.IsConflict)
                return Conflict(new { code = "idempotency_conflict", message = result.ConflictReason });
            return Ok(result.Contract);
        }
        catch (SocialAssetValidationException exception)
        {
            return BadRequest(new { code = "invalid_asset", message = exception.Message });
        }
    }
}
