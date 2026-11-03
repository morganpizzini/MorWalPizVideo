using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Services.Ai;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Controllers;

[Route("api/v1/ai/workflows")]
[ApiController]
[BlockImpersonation]
public sealed class AiPromptsController(
    IAiPromptOperationsService service,
    IAiPromptOptimizationService optimizer) : ControllerBase
{
    [HttpGet]
    [AllowUser(AuthorizationPermissionKeys.AiPromptsView, AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<ActionResult<IReadOnlyList<AiPromptDefinitionDto>>> GetDefinitions(CancellationToken cancellationToken)
    {
        var definitions = await service.GetDefinitionsAsync(cancellationToken);
        return Ok(definitions.Select(ToDto).ToArray());
    }

    [HttpGet("{workflowKey}/{stageKey}/revisions")]
    [AllowUser(AuthorizationPermissionKeys.AiPromptsView, AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<ActionResult<IReadOnlyList<AiPromptRevisionDto>>> GetRevisions(
        string workflowKey,
        string stageKey,
        CancellationToken cancellationToken)
    {
        try
        {
            AiPromptStageCatalog.Get(workflowKey, stageKey);
            var definition = (await service.GetDefinitionsAsync(cancellationToken))
                .FirstOrDefault(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey);
            if (definition is null)
                return NotFound();
            var revisions = await service.GetRevisionsAsync(workflowKey, stageKey, cancellationToken);
            return Ok(revisions.Select(item => ToDto(item, definition)).ToArray());
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Unknown AI workflow stage", Detail = exception.Message });
        }
    }

    [HttpPost("bootstrap")]
    [AllowUser(AuthorizationPermissionKeys.AiPromptsEdit, AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<IActionResult> Bootstrap(CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId is null)
            return Unauthorized();
        try
        {
            var created = await service.BootstrapAsync(actorId, cancellationToken);
            return Ok(new { created });
        }
        catch (AiPromptConflictException exception)
        {
            return Conflict(new ProblemDetails { Status = 409, Title = "AI prompt bootstrap conflict", Detail = exception.Message });
        }
    }

    [HttpPost("{workflowKey}/{stageKey}/revisions")]
    [AllowUser(AuthorizationPermissionKeys.AiPromptsEdit, AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<ActionResult<AiPromptRevisionDto>> CreateDraft(
        string workflowKey,
        string stageKey,
        CreateAiPromptRevisionRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId is null)
            return Unauthorized();
        try
        {
            var revision = await service.CreateDraftAsync(workflowKey, stageKey, request, actorId, cancellationToken);
            var definition = (await service.GetDefinitionsAsync(cancellationToken))
                .First(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey);
            return CreatedAtAction(nameof(GetRevisions), new { workflowKey, stageKey },
                ToDto(revision, definition));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid AI prompt revision", Detail = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "AI prompt definition not found", Detail = exception.Message });
        }
    }

    [HttpPost("~/api/v1/ai/experiments/{experimentId}/candidate")]
    [AllowUser(AuthorizationPermissionKeys.AiPromptsEdit, AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<ActionResult<AiPromptRevisionDto>> CreateCandidate(
        string experimentId,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId is null)
            return Unauthorized();
        try
        {
            var revision = await optimizer.CreateCandidateFromExperimentAsync(experimentId, actorId, cancellationToken);
            var definition = (await service.GetDefinitionsAsync(cancellationToken))
                .First(item => item.WorkflowKey == revision.WorkflowKey && item.StageKey == revision.StageKey);
            return CreatedAtAction(nameof(GetRevisions), new
            {
                workflowKey = revision.WorkflowKey,
                stageKey = revision.StageKey
            }, ToDto(revision, definition));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "AI evaluation experiment not found", Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Status = 409, Title = "AI prompt candidate was not created", Detail = exception.Message });
        }
    }

    [HttpPost("{workflowKey}/{stageKey}/evaluation-cases")]
    [AllowUser(AuthorizationPermissionKeys.AiPromptsEvaluate, AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<IActionResult> AddEvaluationCase(
        string workflowKey,
        string stageKey,
        CreateAiEvaluationCaseRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId is null)
            return Unauthorized();
        request.WorkflowKey = workflowKey;
        request.StageKey = stageKey;
        try
        {
            var item = await service.AddEvaluationCaseAsync(request, actorId, cancellationToken);
            return Created(string.Empty, new { id = item.Id, item.DatasetVersion, item.ExpiresAtUtc });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid AI evaluation case", Detail = exception.Message });
        }
    }

    [HttpPost("{workflowKey}/{stageKey}/evaluate")]
    [AllowUser(AuthorizationPermissionKeys.AiPromptsEvaluate, AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<ActionResult<AiPromptExperimentDto>> Evaluate(
        string workflowKey,
        string stageKey,
        EvaluateAiPromptRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId is null)
            return Unauthorized();
        try
        {
            var experiment = await service.EvaluateAsync(workflowKey, stageKey, request, actorId, cancellationToken);
            return Ok(ToDto(experiment));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid AI prompt evaluation", Detail = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "AI prompt evaluation resource not found", Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Status = 409, Title = "AI prompt evaluation cannot run", Detail = exception.Message });
        }
    }

    [HttpGet("{workflowKey}/{stageKey}/experiments")]
    [AllowUser(AuthorizationPermissionKeys.AiPromptsView, AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<ActionResult<IReadOnlyList<AiPromptExperimentDto>>> GetExperiments(
        string workflowKey,
        string stageKey,
        CancellationToken cancellationToken)
    {
        try
        {
            AiPromptStageCatalog.Get(workflowKey, stageKey);
            var experiments = await service.GetExperimentsAsync(workflowKey, stageKey, cancellationToken);
            return Ok(experiments.Select(ToDto).ToArray());
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Unknown AI workflow stage", Detail = exception.Message });
        }
    }

    [HttpGet("{workflowKey}/{stageKey}/executions")]
    [AllowUser(AuthorizationPermissionKeys.AiPromptsView, AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<ActionResult<IReadOnlyList<AiExecutionRecordDto>>> GetExecutions(
        string workflowKey,
        string stageKey,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            AiPromptStageCatalog.Get(workflowKey, stageKey);
            var records = await service.GetExecutionsAsync(workflowKey, stageKey, take, cancellationToken);
            return Ok(records.Select(ToDto).ToArray());
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Unknown AI workflow stage", Detail = exception.Message });
        }
    }

    [HttpPost("{workflowKey}/{stageKey}/canary")]
    [AllowUser(AuthorizationPermissionKeys.AiPromptsActivate, AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<ActionResult<AiPromptDefinitionDto>> StartCanary(
        string workflowKey,
        string stageKey,
        StartAiPromptCanaryRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId is null)
            return Unauthorized();
        try
        {
            return Ok(ToDto(await service.StartCanaryAsync(workflowKey, stageKey, request, actorId, cancellationToken)));
        }
        catch (AiPromptActivationDisabledException exception)
        {
            return ActivationDisabled(exception);
        }
        catch (AiPromptConflictException exception)
        {
            return Conflict(new ProblemDetails { Status = 409, Title = "Prompt canary conflict", Detail = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Prompt canary rejected", Detail = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "AI prompt canary resource not found", Detail = exception.Message });
        }
    }

    [HttpPost("{workflowKey}/{stageKey}/canary/promote")]
    [AllowUser(AuthorizationPermissionKeys.AiPromptsActivate, AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<ActionResult<AiPromptDefinitionDto>> PromoteCanary(
        string workflowKey,
        string stageKey,
        PromoteAiPromptCanaryRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId is null)
            return Unauthorized();
        try
        {
            return Ok(ToDto(await service.PromoteCanaryAsync(workflowKey, stageKey, request, actorId, cancellationToken)));
        }
        catch (AiPromptActivationDisabledException exception)
        {
            return ActivationDisabled(exception);
        }
        catch (AiPromptConflictException exception)
        {
            return Conflict(new ProblemDetails { Status = 409, Title = "Prompt canary promotion conflict", Detail = exception.Message });
        }
        catch (AiPromptCanaryEvidenceException exception)
        {
            return Conflict(new ProblemDetails { Status = 409, Title = "Canary promotion evidence is insufficient", Detail = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Prompt canary promotion rejected", Detail = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "AI prompt canary resource not found", Detail = exception.Message });
        }
    }

    [HttpPost("{workflowKey}/{stageKey}/canary/cancel")]
    [AllowUser(AuthorizationPermissionKeys.AiPromptsActivate, AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<ActionResult<AiPromptDefinitionDto>> CancelCanary(
        string workflowKey,
        string stageKey,
        CancelAiPromptCanaryRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId is null)
            return Unauthorized();
        try
        {
            return Ok(ToDto(await service.CancelCanaryAsync(workflowKey, stageKey, request, actorId, cancellationToken)));
        }
        catch (AiPromptConflictException exception)
        {
            return Conflict(new ProblemDetails { Status = 409, Title = "Prompt canary cancellation conflict", Detail = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "AI prompt canary resource not found", Detail = exception.Message });
        }
    }

    [HttpPost("{workflowKey}/{stageKey}/rollback")]
    [AllowUser(AuthorizationPermissionKeys.AiPromptsActivate, AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<ActionResult<AiPromptDefinitionDto>> Rollback(
        string workflowKey,
        string stageKey,
        RollbackAiPromptRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        if (actorId is null)
            return Unauthorized();
        try
        {
            return Ok(ToDto(await service.RollbackAsync(workflowKey, stageKey, request, actorId, cancellationToken)));
        }
        catch (AiPromptConflictException exception)
        {
            return Conflict(new ProblemDetails { Status = 409, Title = "Prompt rollback conflict", Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Status = 409, Title = "Prompt rollback unavailable", Detail = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "AI prompt definition not found", Detail = exception.Message });
        }
    }

    private string? GetActorId()
        => ImpersonationClaimsTransformation.GetEffectiveUserId(User);

    private ObjectResult ActivationDisabled(AiPromptActivationDisabledException exception)
        => Conflict(new ProblemDetails { Status = 409, Title = "Prompt activation is disabled", Detail = exception.Message });

    private static AiPromptDefinitionDto ToDto(AiPromptDefinition item)
        => new()
        {
            WorkflowKey = item.WorkflowKey,
            StageKey = item.StageKey,
            DisplayName = item.DisplayName,
            ResponseSchema = AiPromptStageCatalog.Get(item.WorkflowKey, item.StageKey).ResponseSchema,
            ActiveRevisionId = item.ActiveRevisionId,
            PreviousRevisionId = item.PreviousRevisionId,
            CanaryRevisionId = item.CanaryRevisionId,
            CanaryPercentage = item.CanaryPercentage,
            CanaryExperimentId = item.CanaryExperimentId,
            CanaryStartedAtUtc = item.CanaryStartedAtUtc,
            AllowedToolNames = item.AllowedToolNames
        };

    private static AiPromptRevisionDto ToDto(AiPromptRevision item, AiPromptDefinition definition)
        => new()
        {
            Id = item.Id,
            WorkflowKey = item.WorkflowKey,
            StageKey = item.StageKey,
            Messages = item.Messages.Select(message => new AiPromptMessageDto { Role = message.Role, Template = message.Template }).ToList(),
            Variables = item.Variables,
            ResponseSchema = item.ResponseSchema,
            AllowedToolNames = item.AllowedToolNames,
            ParentRevisionId = item.ParentRevisionId,
            SourceExperimentId = item.SourceExperimentId,
            Rationale = item.Rationale,
            AuthorId = item.AuthorId,
            ContentHash = item.ContentHash,
            Lifecycle = item.Id == definition.ActiveRevisionId ? AiPromptRevisionStatus.Active :
                item.Id == definition.CanaryRevisionId ? "Canary" :
                item.Id == definition.PreviousRevisionId ? AiPromptRevisionStatus.Retired : item.Status,
            CreatedAtUtc = item.CreatedAtUtc
        };

    private static AiPromptExperimentDto ToDto(AiPromptExperiment item)
        => new()
        {
            Id = item.Id,
            WorkflowKey = item.WorkflowKey,
            StageKey = item.StageKey,
            BaselineRevisionId = item.BaselineRevisionId,
            CandidateRevisionId = item.CandidateRevisionId,
            DatasetVersion = item.DatasetVersion,
            ScoringPolicyVersion = item.ScoringPolicyVersion,
            Status = item.Status,
            CreatedBy = item.CreatedBy,
            CreatedAtUtc = item.CreatedAtUtc,
            CasesEvaluated = item.CaseResults.Count,
            BaselinePassCount = item.CaseResults.Count(result => result.BaselinePassed),
            CandidatePassCount = item.CaseResults.Count(result => result.CandidatePassed),
            BaselineDurationMilliseconds = item.CaseResults.Sum(result => result.BaselineDurationMilliseconds),
            CandidateDurationMilliseconds = item.CaseResults.Sum(result => result.CandidateDurationMilliseconds)
        };

    private static AiExecutionRecordDto ToDto(AiExecutionRecord item)
        => new()
        {
            ExecutionId = item.ExecutionId,
            WorkflowKey = item.WorkflowKey,
            StageKey = item.StageKey,
            RevisionId = item.RevisionId,
            ChannelId = item.ChannelId,
            Status = item.Status,
            ErrorCode = item.ErrorCode,
            DurationMilliseconds = item.DurationMilliseconds,
            InputCount = item.InputCount,
            OutputCount = item.OutputCount,
            CreatedAtUtc = item.CreatedAtUtc
        };
}
