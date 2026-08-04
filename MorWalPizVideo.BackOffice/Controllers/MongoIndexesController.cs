using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiKeyAuth]
public class MongoIndexesController(IMongoIndexOperationsService operationsService) : ApplicationControllerBase
{
    public sealed class ApplyMongoIndexesRequest
    {
        public string ApprovalToken { get; set; } = string.Empty;
        public string[] ApprovedKeys { get; set; } = [];
    }

    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery] string[]? keys, CancellationToken cancellationToken)
    {
        var audit = await operationsService.AuditAsync(keys, cancellationToken);
        return Ok(audit);
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyMongoIndexesRequest request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.ApprovalToken, "apply-approved-indexes", StringComparison.Ordinal))
        {
            return BadRequest("ApprovalToken must be 'apply-approved-indexes'.");
        }

        if (request.ApprovedKeys.Length == 0)
        {
            return BadRequest("At least one approved index key is required.");
        }

        var applied = await operationsService.ApplyAsync(request.ApprovedKeys, cancellationToken);
        return Ok(applied);
    }
}
