using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Services.Ai;

public interface IAiPromptOperationsService
{
    Task<IList<AiPromptDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken);
    Task<IList<AiPromptRevision>> GetRevisionsAsync(string workflowKey, string stageKey, CancellationToken cancellationToken);
    Task<IList<AiPromptExperiment>> GetExperimentsAsync(string workflowKey, string stageKey, CancellationToken cancellationToken);
    Task<IList<AiExecutionRecord>> GetExecutionsAsync(string workflowKey, string stageKey, int take, CancellationToken cancellationToken);
    Task<int> BootstrapAsync(string actorId, CancellationToken cancellationToken);
    Task<AiPromptRevision> CreateDraftAsync(string workflowKey, string stageKey, CreateAiPromptRevisionRequest request, string actorId, CancellationToken cancellationToken);
    Task<AiEvaluationCase> AddEvaluationCaseAsync(CreateAiEvaluationCaseRequest request, string actorId, CancellationToken cancellationToken);
    Task<AiPromptExperiment> EvaluateAsync(string workflowKey, string stageKey, EvaluateAiPromptRequest request, string actorId, CancellationToken cancellationToken);
    Task<AiPromptDefinition> StartCanaryAsync(string workflowKey, string stageKey, StartAiPromptCanaryRequest request, string actorId, CancellationToken cancellationToken);
    Task<AiPromptDefinition> PromoteCanaryAsync(string workflowKey, string stageKey, PromoteAiPromptCanaryRequest request, string actorId, CancellationToken cancellationToken);
    Task<AiPromptDefinition> CancelCanaryAsync(string workflowKey, string stageKey, CancelAiPromptCanaryRequest request, string actorId, CancellationToken cancellationToken);
    Task<AiPromptDefinition> RollbackAsync(string workflowKey, string stageKey, RollbackAiPromptRequest request, string actorId, CancellationToken cancellationToken);
}

public sealed class AiPromptOperationsService(
    IAiOrchestrationRepository repository,
    IAiPromptCatalog promptCatalog,
    IAiToolRegistry toolRegistry,
    IAiModelGateway modelGateway,
    IOptions<AiOrchestrationOptions> options) : IAiPromptOperationsService
{
    private const string ScoringPolicyVersion = "deterministic-quality-latency-v1";
    private readonly AiOrchestrationOptions settings = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<IList<AiPromptDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken)
        => repository.GetDefinitionsAsync(cancellationToken);

    public Task<IList<AiPromptRevision>> GetRevisionsAsync(string workflowKey, string stageKey, CancellationToken cancellationToken)
        => repository.GetRevisionsAsync(workflowKey, stageKey, cancellationToken);

    public Task<IList<AiPromptExperiment>> GetExperimentsAsync(string workflowKey, string stageKey, CancellationToken cancellationToken)
        => repository.GetExperimentsAsync(workflowKey, stageKey, cancellationToken);

    public Task<IList<AiExecutionRecord>> GetExecutionsAsync(string workflowKey, string stageKey, int take, CancellationToken cancellationToken)
        => repository.GetExecutionsAsync(workflowKey, stageKey, take, cancellationToken);

    public async Task<int> BootstrapAsync(string actorId, CancellationToken cancellationToken)
    {
        var created = 0;
        foreach (var stage in AiPromptStageCatalog.All)
        {
            var existing = await repository.GetDefinitionAsync(stage.WorkflowKey, stage.StageKey, cancellationToken);
            if (existing?.ActiveRevisionId is not null)
                continue;

            var messages = AiPromptStageCatalog.CreateBaselineMessages(stage.WorkflowKey, stage.StageKey).ToList();
            var revision = new AiPromptRevision
            {
                Id = ObjectId.GenerateNewId().ToString(),
                WorkflowKey = stage.WorkflowKey,
                StageKey = stage.StageKey,
                Messages = messages,
                Variables = ["payload"],
                ResponseSchema = stage.ResponseSchema,
                AllowedToolNames = [],
                Rationale = "Initial reviewed baseline migrated from the existing Chat workflow.",
                AuthorId = actorId,
                Status = AiPromptRevisionStatus.Active,
                ContentHash = AiPromptHash.Compute(messages, ["payload"], stage.ResponseSchema, []),
                CreatedAtUtc = DateTime.UtcNow,
                CreationDateTime = DateTime.UtcNow
            };
            await repository.AddRevisionAsync(revision, cancellationToken);

            var definition = existing ?? new AiPromptDefinition
            {
                Id = ObjectId.GenerateNewId().ToString(),
                WorkflowKey = stage.WorkflowKey,
                StageKey = stage.StageKey,
                DisplayName = stage.DisplayName,
                AllowedToolNames = [],
                UpdatedAtUtc = DateTime.UtcNow,
                CreationDateTime = DateTime.UtcNow
            };

            if (existing is null)
            {
                await repository.AddDefinitionIfMissingAsync(definition with { ActiveRevisionId = revision.Id }, cancellationToken);
            }
            else if (!await repository.TrySetActiveRevisionAsync(stage.WorkflowKey, stage.StageKey, null, revision.Id, cancellationToken))
            {
                throw new AiPromptConflictException("The prompt was initialized concurrently. Reload the workflow and retry.");
            }

            await AddAuditAsync(stage.WorkflowKey, stage.StageKey, "bootstrap", actorId, null, revision.Id, null, cancellationToken);
            created++;
        }

        return created;
    }

    public async Task<AiPromptRevision> CreateDraftAsync(
        string workflowKey,
        string stageKey,
        CreateAiPromptRevisionRequest request,
        string actorId,
        CancellationToken cancellationToken)
    {
        var stage = AiPromptStageCatalog.Get(workflowKey, stageKey);
        var definition = await repository.GetDefinitionAsync(workflowKey, stageKey, cancellationToken)
            ?? throw new KeyNotFoundException("Prompt definition not found. Run the operator-invoked bootstrap first.");
        if (string.IsNullOrWhiteSpace(request.Rationale) || request.Rationale.Trim().Length > 500)
            throw new ArgumentException("A change rationale of 1 to 500 characters is required.", nameof(request));
        var messages = request.Messages.Select(message => new AiPromptMessage { Role = message.Role.Trim().ToLowerInvariant(), Template = message.Template }).ToList();
        var variables = request.Variables.Select(value => value.Trim()).ToList();
        var tools = request.AllowedToolNames.Select(value => value.Trim()).Distinct(StringComparer.Ordinal).ToList();
        var revision = new AiPromptRevision
        {
            Id = ObjectId.GenerateNewId().ToString(),
            WorkflowKey = workflowKey,
            StageKey = stageKey,
            Messages = messages,
            Variables = variables,
            ResponseSchema = request.ResponseSchema.Trim(),
            AllowedToolNames = tools,
            ParentRevisionId = definition.ActiveRevisionId,
            Rationale = request.Rationale.Trim(),
            AuthorId = actorId,
            Status = AiPromptRevisionStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
            CreationDateTime = DateTime.UtcNow
        };
        AiPromptCatalog.ValidateRevision(stage, definition, revision, toolRegistry, settings.MaxPromptCharacters);
        if (revision.Messages.Sum(message => message.Template.Length) > settings.MaxPromptCharacters)
            throw new ArgumentException("The combined prompt exceeds the configured character limit.", nameof(request));

        var contentHash = AiPromptHash.Compute(revision.Messages, revision.Variables, revision.ResponseSchema, revision.AllowedToolNames);
        var existingRevisions = await repository.GetRevisionsAsync(workflowKey, stageKey, cancellationToken);
        if (existingRevisions.Any(item => item.ContentHash == contentHash))
            throw new ArgumentException("An identical prompt revision already exists.", nameof(request));

        revision = revision with { ContentHash = contentHash };
        await repository.AddRevisionAsync(revision, cancellationToken);
        await AddAuditAsync(workflowKey, stageKey, "draft-created", actorId, definition.ActiveRevisionId, revision.Id, null, cancellationToken);
        return revision;
    }

    public async Task<AiEvaluationCase> AddEvaluationCaseAsync(
        CreateAiEvaluationCaseRequest request,
        string actorId,
        CancellationToken cancellationToken)
    {
        AiPromptStageCatalog.Get(request.WorkflowKey, request.StageKey);
        if (string.IsNullOrWhiteSpace(request.DatasetVersion) || request.DatasetVersion.Length > 80)
            throw new ArgumentException("A dataset version between 1 and 80 characters is required.", nameof(request));
        if (!request.ContentConsentGranted || !request.ContentRedacted)
            throw new ArgumentException("Evaluation input requires explicit consent and redaction.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.SourceClassification) ||
            request.SourceClassification.Length > 40 ||
            request.InputJson.Length > settings.MaxEvaluationInputCharacters)
            throw new ArgumentException("The evaluation case metadata or input is invalid.", nameof(request));
        if (request.ExpectedContains.Count is < 1 or > 20 ||
            request.ExpectedContains.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 200))
            throw new ArgumentException("Each evaluation case requires 1 to 20 non-empty expected checks of 200 characters or fewer.", nameof(request));
        using (JsonDocument.Parse(request.InputJson))
        {
        }

        var existingExperiments = await repository.GetExperimentsAsync(request.WorkflowKey, request.StageKey, cancellationToken);
        if (existingExperiments.Any(experiment => experiment.DatasetVersion == request.DatasetVersion))
            throw new ArgumentException("An evaluated dataset version is immutable. Create a new version for additional cases.", nameof(request));

        var now = DateTime.UtcNow;
        var item = new AiEvaluationCase
        {
            Id = ObjectId.GenerateNewId().ToString(),
            WorkflowKey = request.WorkflowKey,
            StageKey = request.StageKey,
            DatasetVersion = request.DatasetVersion.Trim(),
            InputJson = request.InputJson,
            ExpectedContains = request.ExpectedContains.Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            SourceClassification = request.SourceClassification.Trim(),
            RedactionStatus = "approved",
            CreatedBy = actorId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(Math.Max(1, settings.EvaluationCaseRetentionDays)),
            CreationDateTime = now
        };
        await repository.AddEvaluationCaseAsync(item, cancellationToken);
        await AddAuditAsync(
            request.WorkflowKey,
            request.StageKey,
            "evaluation-case-created",
            actorId,
            null,
            null,
            null,
            cancellationToken);
        return item;
    }

    public async Task<AiPromptExperiment> EvaluateAsync(
        string workflowKey,
        string stageKey,
        EvaluateAiPromptRequest request,
        string actorId,
        CancellationToken cancellationToken)
    {
        var stage = AiPromptStageCatalog.Get(workflowKey, stageKey);
        var definition = await repository.GetDefinitionAsync(workflowKey, stageKey, cancellationToken)
            ?? throw new KeyNotFoundException("Prompt definition not found.");
        var baselineId = definition.ActiveRevisionId
            ?? throw new InvalidOperationException("The workflow has no active baseline revision.");
        if (baselineId == request.CandidateRevisionId)
            throw new ArgumentException("The candidate revision must differ from the active baseline.", nameof(request));
        var candidate = await repository.GetRevisionAsync(request.CandidateRevisionId, cancellationToken)
            ?? throw new KeyNotFoundException("Candidate revision not found.");
        if (candidate.WorkflowKey != workflowKey || candidate.StageKey != stageKey || candidate.Status != AiPromptRevisionStatus.Draft)
            throw new ArgumentException("Only a draft revision for this workflow stage can be evaluated.", nameof(request));

        var cases = await repository.GetEvaluationCasesAsync(workflowKey, stageKey, request.DatasetVersion, request.CaseIds, cancellationToken);
        if (cases.Count > Math.Clamp(settings.MaxEvaluationCasesPerRun, 1, 1000))
            throw new ArgumentException("The evaluation dataset exceeds the configured per-run case limit.", nameof(request));
        if (request.CaseIds.Count > 0 && cases.Count != request.CaseIds.Distinct(StringComparer.Ordinal).Count())
            throw new ArgumentException("One or more requested evaluation cases are missing or not approved.", nameof(request));
        if (cases.Count == 0)
            throw new ArgumentException("The selected dataset contains no approved evaluation cases.", nameof(request));

        var results = new List<AiPromptExperimentCaseResult>(cases.Count);
        foreach (var evaluationCase in cases)
        {
            var baseline = await ExecuteEvaluationCaseAsync(stage, evaluationCase, baselineId, cancellationToken);
            var candidateResult = await ExecuteEvaluationCaseAsync(stage, evaluationCase, candidate.Id, cancellationToken);
            results.Add(new AiPromptExperimentCaseResult
            {
                CaseId = evaluationCase.Id,
                BaselinePassed = baseline.Passed,
                CandidatePassed = candidateResult.Passed,
                BaselineDurationMilliseconds = baseline.DurationMilliseconds,
                CandidateDurationMilliseconds = candidateResult.DurationMilliseconds
            });
        }

        var baselinePasses = results.Count(result => result.BaselinePassed);
        var candidatePasses = results.Count(result => result.CandidatePassed);
        var baselineDuration = results.Sum(result => result.BaselineDurationMilliseconds);
        var candidateDuration = results.Sum(result => result.CandidateDurationMilliseconds);
        var enoughCases = results.Count >= Math.Max(1, settings.MinimumReviewedCases);
        var minimumPasses = (int)Math.Ceiling(
            results.Count * Math.Clamp(settings.MinimumEvaluationPassRatePercent, 1, 100) / 100d);
        var latencyWithinPolicy = baselineDuration == 0 ||
            candidateDuration <= baselineDuration * (1d + Math.Clamp(settings.MaximumLatencyRegressionPercent, 0, 1000) / 100d);
        var status = !enoughCases ? "Inconclusive" :
            candidatePasses < minimumPasses || candidatePasses < baselinePasses || !latencyWithinPolicy ? "Rejected" : "Passed";
        var now = DateTime.UtcNow;
        var experiment = new AiPromptExperiment
        {
            Id = ObjectId.GenerateNewId().ToString(),
            WorkflowKey = workflowKey,
            StageKey = stageKey,
            BaselineRevisionId = baselineId,
            CandidateRevisionId = candidate.Id,
            DatasetVersion = request.DatasetVersion,
            ScoringPolicyVersion = ScoringPolicyVersion,
            CaseResults = results,
            Status = status,
            CreatedBy = actorId,
            CreatedAtUtc = now,
            CreationDateTime = now
        };
        await repository.AddExperimentAsync(experiment, cancellationToken);
        await AddAuditAsync(workflowKey, stageKey, "evaluation-completed", actorId, baselineId, candidate.Id, experiment.Id, cancellationToken);
        return experiment;
    }

    public async Task<AiPromptDefinition> StartCanaryAsync(
        string workflowKey,
        string stageKey,
        StartAiPromptCanaryRequest request,
        string actorId,
        CancellationToken cancellationToken)
    {
        EnsureActivationEnabled();
        if (request.Percentage is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(request), "Canary allocation must be between 1 and 50 percent.");

        var definition = await repository.GetDefinitionAsync(workflowKey, stageKey, cancellationToken)
            ?? throw new KeyNotFoundException("Prompt definition not found.");
        if (definition.ActiveRevisionId != request.ExpectedActiveRevisionId)
            throw new AiPromptConflictException("The active revision changed. Reload the workflow and retry.");
        if (definition.CanaryRevisionId is not null)
            throw new AiPromptConflictException("A canary is already active for this workflow stage.");
        var candidate = await repository.GetRevisionAsync(request.CandidateRevisionId, cancellationToken)
            ?? throw new KeyNotFoundException("Candidate revision not found.");
        if (candidate.WorkflowKey != workflowKey || candidate.StageKey != stageKey || candidate.Status != AiPromptRevisionStatus.Draft)
            throw new ArgumentException("Only a draft revision for this workflow stage can be canaried.", nameof(request));
        var experiment = await GetPassingExperimentAsync(request.ExperimentId, candidate.Id, definition.ActiveRevisionId, cancellationToken);

        if (!await repository.TryStartCanaryAsync(workflowKey, stageKey, request.ExpectedActiveRevisionId, candidate.Id,
                experiment.Id, request.Percentage, cancellationToken))
            throw new AiPromptConflictException("The prompt changed while the canary was being started.");
        var updated = await repository.GetDefinitionAsync(workflowKey, stageKey, cancellationToken)
            ?? throw new InvalidOperationException("The canary prompt definition could not be reloaded.");
        await AddAuditAsync(workflowKey, stageKey, "canary-started", actorId, definition.ActiveRevisionId, candidate.Id, experiment.Id, cancellationToken);
        return updated;
    }

    public async Task<AiPromptDefinition> PromoteCanaryAsync(
        string workflowKey,
        string stageKey,
        PromoteAiPromptCanaryRequest request,
        string actorId,
        CancellationToken cancellationToken)
    {
        EnsureActivationEnabled();
        var definition = await repository.GetDefinitionAsync(workflowKey, stageKey, cancellationToken)
            ?? throw new KeyNotFoundException("Prompt definition not found.");
        if (definition.ActiveRevisionId != request.ExpectedActiveRevisionId ||
            definition.CanaryRevisionId != request.ExpectedCanaryRevisionId ||
            definition.CanaryExperimentId != request.ExperimentId)
            throw new AiPromptConflictException("The active prompt or canary changed. Reload the workflow and retry.");
        var experiment = await GetPassingExperimentAsync(
            request.ExperimentId,
            request.ExpectedCanaryRevisionId,
            request.ExpectedActiveRevisionId,
            cancellationToken);
        await ValidateCanaryEvidenceAsync(definition, cancellationToken);

        if (!await repository.TryPromoteCanaryAsync(
                workflowKey, stageKey, request.ExpectedActiveRevisionId, request.ExpectedCanaryRevisionId, cancellationToken))
            throw new AiPromptConflictException("The prompt changed while the canary was being promoted.");
        var updated = await repository.GetDefinitionAsync(workflowKey, stageKey, cancellationToken)
            ?? throw new InvalidOperationException("The promoted prompt definition could not be reloaded.");
        await AddAuditAsync(workflowKey, stageKey, "canary-promoted", actorId, definition.ActiveRevisionId,
            request.ExpectedCanaryRevisionId, experiment.Id, cancellationToken);
        return updated;
    }

    public async Task<AiPromptDefinition> CancelCanaryAsync(
        string workflowKey,
        string stageKey,
        CancelAiPromptCanaryRequest request,
        string actorId,
        CancellationToken cancellationToken)
    {
        var definition = await repository.GetDefinitionAsync(workflowKey, stageKey, cancellationToken)
            ?? throw new KeyNotFoundException("Prompt definition not found.");
        if (definition.ActiveRevisionId != request.ExpectedActiveRevisionId ||
            definition.CanaryRevisionId != request.ExpectedCanaryRevisionId)
            throw new AiPromptConflictException("The active prompt or canary changed. Reload the workflow and retry.");
        if (!await repository.TryCancelCanaryAsync(
                workflowKey, stageKey, request.ExpectedActiveRevisionId, request.ExpectedCanaryRevisionId, cancellationToken))
            throw new AiPromptConflictException("The prompt changed while the canary was being cancelled.");
        var updated = await repository.GetDefinitionAsync(workflowKey, stageKey, cancellationToken)
            ?? throw new InvalidOperationException("The cancelled prompt definition could not be reloaded.");
        await AddAuditAsync(workflowKey, stageKey, "canary-cancelled", actorId, definition.ActiveRevisionId,
            request.ExpectedCanaryRevisionId, definition.CanaryExperimentId, cancellationToken);
        return updated;
    }

    public async Task<AiPromptDefinition> RollbackAsync(
        string workflowKey,
        string stageKey,
        RollbackAiPromptRequest request,
        string actorId,
        CancellationToken cancellationToken)
    {
        var definition = await repository.GetDefinitionAsync(workflowKey, stageKey, cancellationToken)
            ?? throw new KeyNotFoundException("Prompt definition not found.");
        if (definition.CanaryRevisionId is not null)
            throw new AiPromptConflictException("Cancel the active canary before rolling back the active revision.");
        var rollbackRevisionId = definition.PreviousRevisionId
            ?? throw new InvalidOperationException("No previous active revision is available for rollback.");
        if (definition.ActiveRevisionId != request.ExpectedActiveRevisionId)
            throw new AiPromptConflictException("The active revision changed. Reload the workflow and retry.");
        if (!await repository.TrySetActiveRevisionAsync(workflowKey, stageKey, request.ExpectedActiveRevisionId, rollbackRevisionId, cancellationToken))
            throw new AiPromptConflictException("The active revision changed while rollback was being applied.");

        var updated = await repository.GetDefinitionAsync(workflowKey, stageKey, cancellationToken)
            ?? throw new InvalidOperationException("The rolled-back prompt definition could not be reloaded.");
        await AddAuditAsync(workflowKey, stageKey, "rollback", actorId, request.ExpectedActiveRevisionId, rollbackRevisionId, null, cancellationToken);
        return updated;
    }

    private async Task<AiPromptExperiment> GetPassingExperimentAsync(
        string experimentId,
        string candidateRevisionId,
        string baselineRevisionId,
        CancellationToken cancellationToken)
    {
        var experiment = await repository.GetExperimentAsync(experimentId, cancellationToken)
            ?? throw new KeyNotFoundException("Evaluation experiment not found.");
        if (experiment.Status != "Passed" || experiment.CandidateRevisionId != candidateRevisionId ||
            experiment.BaselineRevisionId != baselineRevisionId)
            throw new ArgumentException("A passing evaluation against the current baseline is required before activation.");
        return experiment;
    }

    private void EnsureActivationEnabled()
    {
        if (!settings.ActivationEnabled)
            throw new AiPromptActivationDisabledException();
    }

    private async Task ValidateCanaryEvidenceAsync(AiPromptDefinition definition, CancellationToken cancellationToken)
    {
        var minimumSamples = Math.Clamp(settings.MinimumCanaryExecutions, 1, 1000);
        var candidate = await repository.GetExecutionsForRevisionAsync(
            definition.WorkflowKey,
            definition.StageKey,
            definition.CanaryRevisionId!,
            definition.CanaryStartedAtUtc,
            1000,
            cancellationToken);
        var baseline = await repository.GetExecutionsForRevisionAsync(
            definition.WorkflowKey,
            definition.StageKey,
            definition.ActiveRevisionId!,
            definition.CanaryStartedAtUtc,
            1000,
            cancellationToken);
        if (candidate.Count < minimumSamples || baseline.Count < minimumSamples)
            throw new AiPromptCanaryEvidenceException(
                $"Canary promotion requires at least {minimumSamples} executions for both baseline and candidate.");

        var failurePercent = candidate.Count(item => item.Status != "Succeeded") * 100d / candidate.Count;
        if (failurePercent > Math.Clamp(settings.MaximumCanaryFailurePercent, 0, 100))
            throw new AiPromptCanaryEvidenceException("Canary failure rate exceeds the configured promotion threshold.");

        var baselineAverage = baseline.Average(item => item.DurationMilliseconds);
        var candidateAverage = candidate.Average(item => item.DurationMilliseconds);
        if (baselineAverage > 0 &&
            candidateAverage > baselineAverage *
            (1d + Math.Clamp(settings.MaximumCanaryLatencyRegressionPercent, 0, 1000) / 100d))
            throw new AiPromptCanaryEvidenceException("Canary latency regression exceeds the configured promotion threshold.");
    }

    private async Task<(bool Passed, long DurationMilliseconds)> ExecuteEvaluationCaseAsync(
        AiPromptStageDefinition stage,
        AiEvaluationCase evaluationCase,
        string revisionId,
        CancellationToken cancellationToken)
    {
        var prompt = await promptCatalog.ResolveAsync(stage.WorkflowKey, stage.StageKey, revisionId, cancellationToken: cancellationToken);
        var messages = promptCatalog.Render(prompt.Revision, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["payload"] = evaluationCase.InputJson
        });
        var started = Stopwatch.GetTimestamp();
        var output = await modelGateway.CompleteAsync(new AiModelRequest(
            Guid.NewGuid().ToString("N"),
            stage.WorkflowKey,
            stage.StageKey,
            revisionId,
            messages,
            AiPromptStageCatalog.GetResponseType(stage.ResponseSchema)), cancellationToken);
        var duration = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        var valid = IsValidOutput(stage.ResponseSchema, output);
        var expectedTextPresent = evaluationCase.ExpectedContains.All(value => output.Contains(value, StringComparison.OrdinalIgnoreCase));
        return (valid && expectedTextPresent, duration);
    }

    private static bool IsValidOutput(string responseSchema, string output)
    {
        if (responseSchema == "text")
            return !string.IsNullOrWhiteSpace(output);
        try
        {
            var value = JsonSerializer.Deserialize(output, AiPromptStageCatalog.GetResponseType(responseSchema)!, JsonOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private Task AddAuditAsync(
        string workflowKey,
        string stageKey,
        string action,
        string actorId,
        string? previousRevisionId,
        string? revisionId,
        string? experimentId,
        CancellationToken cancellationToken)
        => repository.AddAuditEventAsync(new AiPromptAuditEvent
        {
            Id = ObjectId.GenerateNewId().ToString(),
            WorkflowKey = workflowKey,
            StageKey = stageKey,
            Action = action,
            ActorId = actorId,
            PreviousRevisionId = previousRevisionId,
            RevisionId = revisionId,
            ExperimentId = experimentId,
            CreatedAtUtc = DateTime.UtcNow,
            CreationDateTime = DateTime.UtcNow
        }, cancellationToken);
}

public sealed class AiPromptConflictException(string message) : InvalidOperationException(message);

public sealed class AiPromptActivationDisabledException()
    : InvalidOperationException("Production prompt activation is disabled until an approved rollout policy is configured.");

public sealed class AiPromptCanaryEvidenceException(string message) : InvalidOperationException(message);
