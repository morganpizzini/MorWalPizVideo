using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Controllers;

public sealed record FaqVoteReconciliationRequest(IReadOnlyList<string>? AnswerIds = null);

[ApiController]
[Route("api/faq-maintenance")]
public sealed class FaqMaintenanceController(IFaqVoteReconciliationService reconciliationService) : ControllerBase
{
    [HttpPost("votes/reconcile")]
    [AllowUser(AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<ActionResult<FaqVoteReconciliationResult>> ReconcileVotes(
        FaqVoteReconciliationRequest? request,
        CancellationToken cancellationToken)
        => Ok(await reconciliationService.ReconcileAsync(request?.AnswerIds, cancellationToken));
}