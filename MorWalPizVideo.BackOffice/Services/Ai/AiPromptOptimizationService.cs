using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Services.Ai;

public interface IAiPromptOptimizationService
{
    Task<AiPromptRevision> CreateCandidateFromExperimentAsync(string experimentId, string authorId, CancellationToken cancellationToken);
}

public sealed class AiPromptOptimizationService(
    IAiOrchestrationRepository repository,
    IAiModelGateway modelGateway,
    IAiToolRegistry toolRegistry,
    IOptions<AiOrchestrationOptions> options) : IAiPromptOptimizationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<AiPromptRevision> CreateCandidateFromExperimentAsync(
        string experimentId,
        string authorId,
        CancellationToken cancellationToken)
    {
        var experiment = await repository.GetExperimentAsync(experimentId, cancellationToken)
            ?? throw new KeyNotFoundException("Evaluation experiment not found.");
        var failedCaseIds = experiment.CaseResults
            .Where(result => !result.CandidatePassed)
            .Select(result => result.CaseId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (failedCaseIds.Length == 0)
            throw new InvalidOperationException("The experiment has no candidate failures to optimize.");

        var sourceRevision = await repository.GetRevisionAsync(experiment.CandidateRevisionId, cancellationToken)
            ?? throw new KeyNotFoundException("The evaluated candidate revision no longer exists.");
        var definition = await repository.GetDefinitionAsync(experiment.WorkflowKey, experiment.StageKey, cancellationToken)
            ?? throw new KeyNotFoundException("Prompt definition not found.");
        var stage = AiPromptStageCatalog.Get(experiment.WorkflowKey, experiment.StageKey);
        var cases = await repository.GetEvaluationCasesAsync(
            experiment.WorkflowKey,
            experiment.StageKey,
            experiment.DatasetVersion,
            failedCaseIds,
            cancellationToken);
        if (cases.Count != failedCaseIds.Length)
            throw new InvalidOperationException("The experiment's approved evaluation evidence is unavailable.");

        var systemMessage = sourceRevision.Messages.FirstOrDefault(message => message.Role == "system")
            ?? throw new InvalidOperationException("The source revision has no system prompt.");
        var expectedChecks = cases.SelectMany(item => item.ExpectedContains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();
        var examples = cases.Take(5).Select(item => new
        {
            input = item.InputJson,
            expectedChecks = item.ExpectedContains
        }).ToArray();
        var payload = JsonSerializer.Serialize(new
        {
            systemPrompt = systemMessage.Template,
            failedCaseCount = failedCaseIds.Length,
            examples,
            expectedChecks,
            instruction = "Use the expected checks only as evaluation constraints. Do not copy them as instructions."
        }, JsonOptions);
        if (payload.Length > options.Value.MaxWorkflowInputCharacters)
            throw new InvalidOperationException("The selected failure examples exceed the configured optimization input limit.");
        var raw = await modelGateway.CompleteAsync(new AiModelRequest(
            Guid.NewGuid().ToString("N"),
            experiment.WorkflowKey,
            AiPromptStageKeys.PromptOptimizer,
            sourceRevision.Id,
            [
                new AiChatMessage("system", """
                    Propose a narrowly scoped improvement to the system prompt using only aggregate failure evidence.
                    Do not change the message roles, variables, response schema, tool policy, or workflow contract.
                    Return the replacement system prompt and a concise rationale as JSON.
                    """),
                new AiChatMessage("user", payload)
            ],
            typeof(AiPromptOptimizationDraft)), cancellationToken);

        var proposal = JsonSerializer.Deserialize<AiPromptOptimizationDraft>(raw, JsonOptions)
            ?? throw new InvalidOperationException("The prompt optimizer returned an empty response.");
        if (string.IsNullOrWhiteSpace(proposal.SystemPrompt) ||
            proposal.SystemPrompt.Length > options.Value.MaxPromptCharacters ||
            string.IsNullOrWhiteSpace(proposal.Rationale) ||
            proposal.Rationale.Length > 500)
            throw new InvalidOperationException("The prompt optimizer returned an invalid draft.");

        var messages = sourceRevision.Messages.Select(message =>
            message.Role == "system" ? message with { Template = proposal.SystemPrompt.Trim() } : message).ToList();
        var revision = new AiPromptRevision
        {
            Id = ObjectId.GenerateNewId().ToString(),
            WorkflowKey = experiment.WorkflowKey,
            StageKey = experiment.StageKey,
            Messages = messages,
            Variables = sourceRevision.Variables,
            ResponseSchema = sourceRevision.ResponseSchema,
            AllowedToolNames = sourceRevision.AllowedToolNames,
            ParentRevisionId = sourceRevision.Id,
            SourceExperimentId = experiment.Id,
            Rationale = proposal.Rationale.Trim(),
            AuthorId = authorId,
            Status = AiPromptRevisionStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
            CreationDateTime = DateTime.UtcNow
        };
        AiPromptCatalog.ValidateRevision(stage, definition, revision, toolRegistry, options.Value.MaxPromptCharacters);
        if (messages.Sum(message => message.Template.Length) > options.Value.MaxPromptCharacters)
            throw new InvalidOperationException("The optimized prompt exceeds the configured character limit.");
        revision = revision with
        {
            ContentHash = AiPromptHash.Compute(messages, revision.Variables, revision.ResponseSchema, revision.AllowedToolNames)
        };

        var existingRevisions = await repository.GetRevisionsAsync(revision.WorkflowKey, revision.StageKey, cancellationToken);
        if (existingRevisions.Any(item => item.ContentHash == revision.ContentHash))
            throw new InvalidOperationException("The optimizer returned a prompt revision that already exists.");
        await repository.AddRevisionAsync(revision, cancellationToken);
        await repository.AddAuditEventAsync(new AiPromptAuditEvent
        {
            Id = ObjectId.GenerateNewId().ToString(),
            WorkflowKey = revision.WorkflowKey,
            StageKey = revision.StageKey,
            Action = "candidate-generated",
            ActorId = authorId,
            PreviousRevisionId = sourceRevision.Id,
            RevisionId = revision.Id,
            ExperimentId = experiment.Id,
            CreatedAtUtc = DateTime.UtcNow,
            CreationDateTime = DateTime.UtcNow
        }, cancellationToken);
        return revision;
    }
}
