using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Controllers;

[Route("api/script-studio")]
[ApiController]
[RequireChannelScope]
public sealed class ScriptStudioController(IScriptStudioService service) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.ScriptsStudio)]
    public async Task<ActionResult<ScriptStudioDocumentDto>> Get(CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(HttpContext.GetChannelContext().ChannelId, cancellationToken);
        return Ok(ToDto(item));
    }

    [HttpPut]
    [AllowUser(AuthorizationPermissionKeys.ScriptsStudio)]
    public async Task<ActionResult<ScriptStudioDocumentDto>> Save(ScriptStudioDocumentDto document, CancellationToken cancellationToken)
    {
        try
        {
            var item = await service.SaveAsync(HttpContext.GetChannelContext().ChannelId, document, cancellationToken);
            return Ok(ToDto(item));
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpPost("generate")]
    [AllowUser(AuthorizationPermissionKeys.ScriptsStudio)]
    public async Task<ActionResult<ScriptStudioGenerationResponse>> Generate(ScriptStudioGenerationRequest request, CancellationToken cancellationToken)
    {
        var userId = ImpersonationClaimsTransformation.GetEffectiveUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        try
        {
            var result = await service.GenerateAsync(userId, HttpContext.GetChannelContext().ChannelId, request,
                HttpContext.TraceIdentifier, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("quota", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new { code = "quota_exceeded", message = exception.Message });
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpGet("global-prompt")]
    [AllowUser(AuthorizationPermissionKeys.ScriptsGlobalPromptManage)]
    public async Task<ActionResult<ScriptStudioGlobalPromptDto>> GetGlobalPrompt(CancellationToken cancellationToken)
    {
        var prompt = await service.GetGlobalPromptAsync(cancellationToken);
        return Ok(new ScriptStudioGlobalPromptDto { Prompt = prompt.Prompt, Version = prompt.Version });
    }

    [HttpPut("global-prompt")]
    [AllowUser(AuthorizationPermissionKeys.ScriptsGlobalPromptManage)]
    public async Task<ActionResult<ScriptStudioGlobalPromptDto>> SaveGlobalPrompt(ScriptStudioGlobalPromptDto request, CancellationToken cancellationToken)
    {
        var prompt = await service.SaveGlobalPromptAsync(request.Prompt, cancellationToken);
        return Ok(new ScriptStudioGlobalPromptDto { Prompt = prompt.Prompt, Version = prompt.Version });
    }

    private static ScriptStudioDocumentDto ToDto(MorWalPizVideo.Models.Models.ScriptStudioChannelData item) => new()
    {
        ChannelId = item.ChannelId, Script = item.Script, SavedPrompt = item.SavedPrompt, Examples = item.Examples,
        Style = item.Style, GeneralContext = item.GeneralContext, SavedResult = item.SavedResult, Format = item.Format
    };
}

[Route("api/script-studio/quota")]
[ApiController]
public sealed class ScriptStudioQuotaController(IScriptStudioService service) : ControllerBase
{
    [HttpPut("{userId}")]
    [AllowUser(AuthorizationPermissionKeys.ScriptsQuotaManage)]
    public async Task<IActionResult> Set(string userId, ScriptStudioQuotaDto request, CancellationToken cancellationToken)
    {
        try
        {
            await service.SetQuotaAsync(userId, request.MonthlyQuota, cancellationToken);
            return NoContent();
        }
        catch (ArgumentOutOfRangeException exception) { return BadRequest(new { message = exception.Message }); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
    }
}