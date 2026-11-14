using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Domain.Feedback;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/v1/feedback")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class FeedbackController(FeedbackService feedbackService) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.FeedbackView, AuthorizationPermissionKeys.FeedbackManage)]
    public async Task<ActionResult<FeedbackPageContract>> List(
        [FromQuery] FeedbackStatus? status,
        [FromQuery] string? application,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > 10000 || pageSize is < 1 or > 50) return BadRequest("Invalid pagination.");
        var items = await feedbackService.ListAsync(status, application, page, pageSize, cancellationToken);
        var total = await feedbackService.CountAsync(status, application, cancellationToken);
        return Ok(new FeedbackPageContract(items.Select(ToContract).ToArray(), page, pageSize, total));
    }

    [HttpGet("{id}")]
    [AllowUser(AuthorizationPermissionKeys.FeedbackView, AuthorizationPermissionKeys.FeedbackManage)]
    public async Task<ActionResult<FeedbackItemContract>> Get(string id, CancellationToken cancellationToken)
    {
        var feedback = await feedbackService.GetAsync(id, cancellationToken);
        return feedback is null ? NotFound() : Ok(ToContract(feedback));
    }

    [HttpPatch("{id}/status")]
    [AllowUser(AuthorizationPermissionKeys.FeedbackManage)]
    public async Task<ActionResult<FeedbackItemContract>> UpdateStatus(
        string id,
        UpdateFeedbackStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<FeedbackStatus>(request.Status, true, out var status))
            return BadRequest("Invalid feedback status.");
        var updated = await feedbackService.UpdateStatusAsync(id, status, request.ExpectedRevision, User, cancellationToken);
        if (!updated)
        {
            var current = await feedbackService.GetAsync(id, cancellationToken);
            return current is null ? NotFound() : Conflict(new ProblemDetails { Title = "Feedback was changed by another user." });
        }
        return Ok(ToContract((await feedbackService.GetAsync(id, cancellationToken))!));
    }

    [HttpDelete("{id}")]
    [AllowUser(AuthorizationPermissionKeys.FeedbackManage)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken) =>
        await feedbackService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    private static FeedbackItemContract ToContract(Feedback item) => new(
        item.Id, item.RequestId, item.Application, item.Description, item.PagePath, item.ComponentKey,
        item.ClientRelease, item.UserAgent, item.Language, item.Timezone, item.ViewportWidth,
        item.ViewportHeight, item.Online, item.ReporterUsername, item.Status.ToString(),
        item.Revision, item.CreationDateTime, item.UpdatedAtUtc, item.ExpiresAtUtc,
        item.UpdatedByAccountId, item.UpdatedByUsername);
}
