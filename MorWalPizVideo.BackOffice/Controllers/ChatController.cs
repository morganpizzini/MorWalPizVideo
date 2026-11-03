using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Ai;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiKeyAuth]
public sealed class ChatController(
    IAiWorkflowService workflowService,
    IChannelContextResolver channelContextResolver) : ApplicationControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GetReviewDetails([FromBody] ReviewRequest request, CancellationToken cancellationToken)
    {
        var channel = await channelContextResolver.ResolveAsync(HttpContext);
        try
        {
            var result = await workflowService.ReviewAsync(
                request,
                channel.Context?.ChannelId,
                channel.Context?.Channel.Terminology,
                cancellationToken);
            Response.Headers["X-AI-Execution-Id"] = result.ExecutionId;
            return Ok(result.Value);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid chat review request",
                Detail = exception.Message
            });
        }
        catch (AiPromptUnavailableException)
        {
            return PromptUnavailable();
        }
        catch (TimeoutException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "AI provider timed out.");
        }
    }

    [HttpPost("translate")]
    public async Task<IActionResult> TranslateVideoContent([FromBody] VideoTranslationRequest request, CancellationToken cancellationToken)
    {
        var channel = await channelContextResolver.ResolveAsync(HttpContext);
        try
        {
            var result = await workflowService.TranslateAsync(request, channel.Context?.ChannelId, cancellationToken);
            Response.Headers["X-AI-Execution-Id"] = result.ExecutionId;
            return Ok(result.Value);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid translation request",
                Detail = exception.Message
            });
        }
        catch (AiPromptUnavailableException)
        {
            return PromptUnavailable();
        }
        catch (TimeoutException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "AI provider timed out.");
        }
    }

    [HttpPost("transcript-analysis")]
    public async Task<IActionResult> AnalyzeTranscript([FromBody] TranscriptAnalysisRequest request, CancellationToken cancellationToken)
    {
        var channel = await channelContextResolver.ResolveAsync(HttpContext);
        try
        {
            var result = await workflowService.AnalyzeTranscriptAsync(request, channel.Context?.ChannelId, cancellationToken);
            Response.Headers["X-AI-Execution-Id"] = result.ExecutionId;
            return Ok(result.Value);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid transcript request",
                Detail = exception.Message
            });
        }
        catch (AiPromptUnavailableException)
        {
            return PromptUnavailable();
        }
        catch (TimeoutException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "AI provider timed out.");
        }
    }

    private ObjectResult PromptUnavailable()
        => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "The AI workflow prompt is not initialized.");
}
