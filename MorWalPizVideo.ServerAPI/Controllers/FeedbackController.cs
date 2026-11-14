using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain.Feedback;
using MorWalPizVideo.MvcHelpers.Authentication;

namespace MorWalPizVideo.ServerAPI.Controllers;

[ApiController]
[EnableCors("public-account")]
[Route("api/v1/feedback")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class FeedbackController(FeedbackService feedbackService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("public-feedback")]
    [RequestSizeLimit(16 * 1024)]
    public async Task<ActionResult<FeedbackReceiptContract>> Submit(
        SubmitFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!PublicAccountSecurity.HasValidCsrf(Request))
            return StatusCode(StatusCodes.Status403Forbidden);

        var authentication = await HttpContext.AuthenticateAsync(PublicAccountSecurity.Scheme);
        var principal = authentication.Succeeded ? authentication.Principal : null;
        try
        {
            var result = await feedbackService.SubmitAsync(new FeedbackSubmissionCommand(
                request.RequestId, request.Description, request.Context.Application, request.Context.PagePath,
                request.Context.ComponentKey, request.Context.ClientRelease, request.Context.UserAgent,
                request.Context.Language, request.Context.Timezone, request.Context.ViewportWidth,
                request.Context.ViewportHeight, request.Context.Online), principal, cancellationToken);
            return result.Existing
                ? Ok(new FeedbackReceiptContract(result.Feedback.Id, result.Feedback.CreationDateTime, result.Feedback.ReporterAccountId is not null))
                : Created(string.Empty, new FeedbackReceiptContract(result.Feedback.Id, result.Feedback.CreationDateTime, result.Feedback.ReporterAccountId is not null));
        }
        catch (FeedbackValidationException exception) { return BadRequest(new ProblemDetails { Title = exception.Message }); }
        catch (FeedbackConflictException) { return Conflict(new ProblemDetails { Title = "The feedback request conflicts with an existing submission." }); }
    }
}
