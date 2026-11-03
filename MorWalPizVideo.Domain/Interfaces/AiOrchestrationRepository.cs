using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain.Interfaces;

public interface IAiOrchestrationRepository
{
    Task<AiPromptDefinition?> GetDefinitionAsync(string workflowKey, string stageKey, CancellationToken cancellationToken = default);
    Task<IList<AiPromptDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<AiPromptDefinition> AddDefinitionIfMissingAsync(AiPromptDefinition definition, CancellationToken cancellationToken = default);
    Task<AiPromptRevision?> GetRevisionAsync(string revisionId, CancellationToken cancellationToken = default);
    Task<IList<AiPromptRevision>> GetRevisionsAsync(string workflowKey, string stageKey, CancellationToken cancellationToken = default);
    Task AddRevisionAsync(AiPromptRevision revision, CancellationToken cancellationToken = default);
    Task<bool> TrySetActiveRevisionAsync(string workflowKey, string stageKey, string? expectedRevisionId, string revisionId, CancellationToken cancellationToken = default);
    Task<bool> TryStartCanaryAsync(string workflowKey, string stageKey, string expectedRevisionId, string revisionId, string experimentId, int percentage, CancellationToken cancellationToken = default);
    Task<bool> TryPromoteCanaryAsync(string workflowKey, string stageKey, string expectedRevisionId, string expectedCanaryRevisionId, CancellationToken cancellationToken = default);
    Task<bool> TryCancelCanaryAsync(string workflowKey, string stageKey, string expectedRevisionId, string expectedCanaryRevisionId, CancellationToken cancellationToken = default);
    Task AddExecutionAsync(AiExecutionRecord record, CancellationToken cancellationToken = default);
    Task<IList<AiExecutionRecord>> GetExecutionsAsync(string workflowKey, string stageKey, int take, CancellationToken cancellationToken = default);
    Task<IList<AiExecutionRecord>> GetExecutionsForRevisionAsync(string workflowKey, string stageKey, string revisionId, DateTime? fromInclusiveUtc, int take, CancellationToken cancellationToken = default);
    Task AddEvaluationCaseAsync(AiEvaluationCase evaluationCase, CancellationToken cancellationToken = default);
    Task<IList<AiEvaluationCase>> GetEvaluationCasesAsync(string workflowKey, string stageKey, string datasetVersion, IReadOnlyCollection<string>? caseIds = null, CancellationToken cancellationToken = default);
    Task AddExperimentAsync(AiPromptExperiment experiment, CancellationToken cancellationToken = default);
    Task<AiPromptExperiment?> GetExperimentAsync(string experimentId, CancellationToken cancellationToken = default);
    Task<IList<AiPromptExperiment>> GetExperimentsAsync(string workflowKey, string stageKey, CancellationToken cancellationToken = default);
    Task AddAuditEventAsync(AiPromptAuditEvent auditEvent, CancellationToken cancellationToken = default);
}

public sealed class AiOrchestrationRepository(IMongoDatabase database) : IAiOrchestrationRepository
{
    private readonly IMongoCollection<AiPromptDefinition> definitions = database.GetCollection<AiPromptDefinition>(DbCollections.AiPromptDefinitions);
    private readonly IMongoCollection<AiPromptRevision> revisions = database.GetCollection<AiPromptRevision>(DbCollections.AiPromptRevisions);
    private readonly IMongoCollection<AiExecutionRecord> executions = database.GetCollection<AiExecutionRecord>(DbCollections.AiExecutionRecords);
    private readonly IMongoCollection<AiEvaluationCase> evaluationCases = database.GetCollection<AiEvaluationCase>(DbCollections.AiEvaluationCases);
    private readonly IMongoCollection<AiPromptExperiment> experiments = database.GetCollection<AiPromptExperiment>(DbCollections.AiPromptExperiments);
    private readonly IMongoCollection<AiPromptAuditEvent> auditEvents = database.GetCollection<AiPromptAuditEvent>(DbCollections.AiPromptAudit);

    public Task<AiPromptDefinition?> GetDefinitionAsync(string workflowKey, string stageKey, CancellationToken cancellationToken = default)
        => definitions.Find(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey).FirstOrDefaultAsync(cancellationToken);

    public async Task<IList<AiPromptDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
        => await definitions.Find(Builders<AiPromptDefinition>.Filter.Empty).SortBy(item => item.WorkflowKey).ThenBy(item => item.StageKey).ToListAsync(cancellationToken);

    public async Task<AiPromptDefinition> AddDefinitionIfMissingAsync(AiPromptDefinition definition, CancellationToken cancellationToken = default)
    {
        var existing = await GetDefinitionAsync(definition.WorkflowKey, definition.StageKey, cancellationToken);
        if (existing is not null)
            return existing;

        try
        {
            await definitions.InsertOneAsync(definition, cancellationToken: cancellationToken);
            return definition;
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return await GetDefinitionAsync(definition.WorkflowKey, definition.StageKey, cancellationToken)
                ?? throw new InvalidOperationException("The prompt definition was concurrently created but could not be reloaded.", exception);
        }
    }

    public Task<AiPromptRevision?> GetRevisionAsync(string revisionId, CancellationToken cancellationToken = default)
        => revisions.Find(item => item.Id == revisionId).FirstOrDefaultAsync(cancellationToken);

    public async Task<IList<AiPromptRevision>> GetRevisionsAsync(string workflowKey, string stageKey, CancellationToken cancellationToken = default)
        => await revisions.Find(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey)
            .SortByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task AddRevisionAsync(AiPromptRevision revision, CancellationToken cancellationToken = default)
        => revisions.InsertOneAsync(revision, cancellationToken: cancellationToken);

    public async Task<bool> TrySetActiveRevisionAsync(string workflowKey, string stageKey, string? expectedRevisionId, string revisionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AiPromptDefinition>.Filter.And(
            Builders<AiPromptDefinition>.Filter.Eq(item => item.WorkflowKey, workflowKey),
            Builders<AiPromptDefinition>.Filter.Eq(item => item.StageKey, stageKey),
            Builders<AiPromptDefinition>.Filter.Eq(item => item.ActiveRevisionId, expectedRevisionId),
            Builders<AiPromptDefinition>.Filter.Eq(item => item.CanaryRevisionId, null));
        var update = Builders<AiPromptDefinition>.Update
            .Set(item => item.PreviousRevisionId, expectedRevisionId)
            .Set(item => item.ActiveRevisionId, revisionId)
            .Set(item => item.UpdatedAtUtc, DateTime.UtcNow);
        var result = await definitions.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<bool> TryStartCanaryAsync(string workflowKey, string stageKey, string expectedRevisionId, string revisionId, string experimentId, int percentage, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AiPromptDefinition>.Filter.And(
            Builders<AiPromptDefinition>.Filter.Eq(item => item.WorkflowKey, workflowKey),
            Builders<AiPromptDefinition>.Filter.Eq(item => item.StageKey, stageKey),
            Builders<AiPromptDefinition>.Filter.Eq(item => item.ActiveRevisionId, expectedRevisionId),
            Builders<AiPromptDefinition>.Filter.Eq(item => item.CanaryRevisionId, null));
        var update = Builders<AiPromptDefinition>.Update
            .Set(item => item.CanaryRevisionId, revisionId)
            .Set(item => item.CanaryExperimentId, experimentId)
            .Set(item => item.CanaryPercentage, percentage)
            .Set(item => item.CanaryStartedAtUtc, DateTime.UtcNow)
            .Set(item => item.UpdatedAtUtc, DateTime.UtcNow);
        var result = await definitions.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<bool> TryPromoteCanaryAsync(string workflowKey, string stageKey, string expectedRevisionId, string expectedCanaryRevisionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AiPromptDefinition>.Filter.And(
            Builders<AiPromptDefinition>.Filter.Eq(item => item.WorkflowKey, workflowKey),
            Builders<AiPromptDefinition>.Filter.Eq(item => item.StageKey, stageKey),
            Builders<AiPromptDefinition>.Filter.Eq(item => item.ActiveRevisionId, expectedRevisionId),
            Builders<AiPromptDefinition>.Filter.Eq(item => item.CanaryRevisionId, expectedCanaryRevisionId));
        var update = Builders<AiPromptDefinition>.Update
            .Set(item => item.PreviousRevisionId, expectedRevisionId)
            .Set(item => item.ActiveRevisionId, expectedCanaryRevisionId)
            .Set(item => item.CanaryRevisionId, null)
            .Set(item => item.CanaryExperimentId, null)
            .Set(item => item.CanaryPercentage, 0)
            .Set(item => item.CanaryStartedAtUtc, null)
            .Set(item => item.UpdatedAtUtc, DateTime.UtcNow);
        var result = await definitions.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<bool> TryCancelCanaryAsync(string workflowKey, string stageKey, string expectedRevisionId, string expectedCanaryRevisionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AiPromptDefinition>.Filter.And(
            Builders<AiPromptDefinition>.Filter.Eq(item => item.WorkflowKey, workflowKey),
            Builders<AiPromptDefinition>.Filter.Eq(item => item.StageKey, stageKey),
            Builders<AiPromptDefinition>.Filter.Eq(item => item.ActiveRevisionId, expectedRevisionId),
            Builders<AiPromptDefinition>.Filter.Eq(item => item.CanaryRevisionId, expectedCanaryRevisionId));
        var update = Builders<AiPromptDefinition>.Update
            .Set(item => item.CanaryRevisionId, null)
            .Set(item => item.CanaryExperimentId, null)
            .Set(item => item.CanaryPercentage, 0)
            .Set(item => item.CanaryStartedAtUtc, null)
            .Set(item => item.UpdatedAtUtc, DateTime.UtcNow);
        var result = await definitions.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public Task AddExecutionAsync(AiExecutionRecord record, CancellationToken cancellationToken = default)
        => executions.InsertOneAsync(record, cancellationToken: cancellationToken);

    public async Task<IList<AiExecutionRecord>> GetExecutionsAsync(string workflowKey, string stageKey, int take, CancellationToken cancellationToken = default)
        => await executions.Find(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey)
            .SortByDescending(item => item.CreatedAtUtc)
            .Limit(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);

    public async Task<IList<AiExecutionRecord>> GetExecutionsForRevisionAsync(
        string workflowKey,
        string stageKey,
        string revisionId,
        DateTime? fromInclusiveUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<AiExecutionRecord>.Filter.And(
            Builders<AiExecutionRecord>.Filter.Eq(item => item.WorkflowKey, workflowKey),
            Builders<AiExecutionRecord>.Filter.Eq(item => item.StageKey, stageKey),
            Builders<AiExecutionRecord>.Filter.Eq(item => item.RevisionId, revisionId));
        if (fromInclusiveUtc.HasValue)
            filter &= Builders<AiExecutionRecord>.Filter.Gte(item => item.CreatedAtUtc, fromInclusiveUtc.Value);
        return await executions.Find(filter)
            .SortByDescending(item => item.CreatedAtUtc)
            .Limit(Math.Clamp(take, 1, 1000))
            .ToListAsync(cancellationToken);
    }

    public Task AddEvaluationCaseAsync(AiEvaluationCase evaluationCase, CancellationToken cancellationToken = default)
        => evaluationCases.InsertOneAsync(evaluationCase, cancellationToken: cancellationToken);

    public async Task<IList<AiEvaluationCase>> GetEvaluationCasesAsync(string workflowKey, string stageKey, string datasetVersion, IReadOnlyCollection<string>? caseIds = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AiEvaluationCase>.Filter.And(
            Builders<AiEvaluationCase>.Filter.Eq(item => item.WorkflowKey, workflowKey),
            Builders<AiEvaluationCase>.Filter.Eq(item => item.StageKey, stageKey),
            Builders<AiEvaluationCase>.Filter.Eq(item => item.DatasetVersion, datasetVersion),
            Builders<AiEvaluationCase>.Filter.Eq(item => item.RedactionStatus, "approved"));
        if (caseIds is { Count: > 0 })
            filter &= Builders<AiEvaluationCase>.Filter.In(item => item.Id, caseIds);

        return await evaluationCases.Find(filter).SortBy(item => item.CreationDateTime).ToListAsync(cancellationToken);
    }

    public Task AddExperimentAsync(AiPromptExperiment experiment, CancellationToken cancellationToken = default)
        => experiments.InsertOneAsync(experiment, cancellationToken: cancellationToken);

    public Task<AiPromptExperiment?> GetExperimentAsync(string experimentId, CancellationToken cancellationToken = default)
        => experiments.Find(item => item.Id == experimentId).FirstOrDefaultAsync(cancellationToken);

    public async Task<IList<AiPromptExperiment>> GetExperimentsAsync(string workflowKey, string stageKey, CancellationToken cancellationToken = default)
        => await experiments.Find(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey)
            .SortByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task AddAuditEventAsync(AiPromptAuditEvent auditEvent, CancellationToken cancellationToken = default)
        => auditEvents.InsertOneAsync(auditEvent, cancellationToken: cancellationToken);
}

public sealed class AiOrchestrationMockRepository(IMockScenario scenario) : IAiOrchestrationRepository
{
    private static readonly object MutationLock = new();

    public Task<AiPromptDefinition?> GetDefinitionAsync(string workflowKey, string stageKey, CancellationToken cancellationToken = default)
        => Task.FromResult(scenario.Read<AiPromptDefinition>(DbCollections.AiPromptDefinitions)
            .FirstOrDefault(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey));

    public Task<IList<AiPromptDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IList<AiPromptDefinition>>(scenario.Read<AiPromptDefinition>(DbCollections.AiPromptDefinitions)
            .OrderBy(item => item.WorkflowKey).ThenBy(item => item.StageKey).ToList());

    public async Task<AiPromptDefinition> AddDefinitionIfMissingAsync(AiPromptDefinition definition, CancellationToken cancellationToken = default)
    {
        lock (MutationLock)
        {
            var existing = scenario.Read<AiPromptDefinition>(DbCollections.AiPromptDefinitions)
                .FirstOrDefault(item => item.WorkflowKey == definition.WorkflowKey && item.StageKey == definition.StageKey);
            if (existing is not null)
                return existing;
            return scenario.Add(DbCollections.AiPromptDefinitions, definition);
        }
    }

    public Task<AiPromptRevision?> GetRevisionAsync(string revisionId, CancellationToken cancellationToken = default)
        => Task.FromResult(scenario.Read<AiPromptRevision>(DbCollections.AiPromptRevisions).FirstOrDefault(item => item.Id == revisionId));

    public Task<IList<AiPromptRevision>> GetRevisionsAsync(string workflowKey, string stageKey, CancellationToken cancellationToken = default)
        => Task.FromResult<IList<AiPromptRevision>>(scenario.Read<AiPromptRevision>(DbCollections.AiPromptRevisions)
            .Where(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey)
            .OrderByDescending(item => item.CreatedAtUtc).ToList());

    public Task AddRevisionAsync(AiPromptRevision revision, CancellationToken cancellationToken = default)
    {
        lock (MutationLock)
        {
            scenario.Add(DbCollections.AiPromptRevisions, revision);
        }
        return Task.CompletedTask;
    }

    public Task<bool> TrySetActiveRevisionAsync(string workflowKey, string stageKey, string? expectedRevisionId, string revisionId, CancellationToken cancellationToken = default)
    {
        lock (MutationLock)
        {
            var definition = scenario.Read<AiPromptDefinition>(DbCollections.AiPromptDefinitions)
                .FirstOrDefault(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey);
            if (definition is null || definition.ActiveRevisionId != expectedRevisionId || definition.CanaryRevisionId is not null)
                return Task.FromResult(false);

            scenario.Replace(DbCollections.AiPromptDefinitions, definition with
            {
                PreviousRevisionId = expectedRevisionId,
                ActiveRevisionId = revisionId,
                UpdatedAtUtc = DateTime.UtcNow
            });
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryStartCanaryAsync(string workflowKey, string stageKey, string expectedRevisionId, string revisionId, string experimentId, int percentage, CancellationToken cancellationToken = default)
    {
        lock (MutationLock)
        {
            var definition = scenario.Read<AiPromptDefinition>(DbCollections.AiPromptDefinitions)
                .FirstOrDefault(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey);
            if (definition is null || definition.ActiveRevisionId != expectedRevisionId || definition.CanaryRevisionId is not null)
                return Task.FromResult(false);
            scenario.Replace(DbCollections.AiPromptDefinitions, definition with
            {
                CanaryRevisionId = revisionId,
                CanaryExperimentId = experimentId,
                CanaryPercentage = percentage,
                CanaryStartedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryPromoteCanaryAsync(string workflowKey, string stageKey, string expectedRevisionId, string expectedCanaryRevisionId, CancellationToken cancellationToken = default)
    {
        lock (MutationLock)
        {
            var definition = scenario.Read<AiPromptDefinition>(DbCollections.AiPromptDefinitions)
                .FirstOrDefault(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey);
            if (definition is null || definition.ActiveRevisionId != expectedRevisionId || definition.CanaryRevisionId != expectedCanaryRevisionId)
                return Task.FromResult(false);
            scenario.Replace(DbCollections.AiPromptDefinitions, definition with
            {
                PreviousRevisionId = expectedRevisionId,
                ActiveRevisionId = expectedCanaryRevisionId,
                CanaryRevisionId = null,
                CanaryExperimentId = null,
                CanaryPercentage = 0,
                CanaryStartedAtUtc = null,
                UpdatedAtUtc = DateTime.UtcNow
            });
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryCancelCanaryAsync(string workflowKey, string stageKey, string expectedRevisionId, string expectedCanaryRevisionId, CancellationToken cancellationToken = default)
    {
        lock (MutationLock)
        {
            var definition = scenario.Read<AiPromptDefinition>(DbCollections.AiPromptDefinitions)
                .FirstOrDefault(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey);
            if (definition is null || definition.ActiveRevisionId != expectedRevisionId || definition.CanaryRevisionId != expectedCanaryRevisionId)
                return Task.FromResult(false);
            scenario.Replace(DbCollections.AiPromptDefinitions, definition with
            {
                CanaryRevisionId = null,
                CanaryExperimentId = null,
                CanaryPercentage = 0,
                CanaryStartedAtUtc = null,
                UpdatedAtUtc = DateTime.UtcNow
            });
            return Task.FromResult(true);
        }
    }

    public Task AddExecutionAsync(AiExecutionRecord record, CancellationToken cancellationToken = default)
    {
        scenario.Add(DbCollections.AiExecutionRecords, record);
        return Task.CompletedTask;
    }

    public Task<IList<AiExecutionRecord>> GetExecutionsAsync(string workflowKey, string stageKey, int take, CancellationToken cancellationToken = default)
        => Task.FromResult<IList<AiExecutionRecord>>(scenario.Read<AiExecutionRecord>(DbCollections.AiExecutionRecords)
            .Where(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 200)).ToList());

    public Task<IList<AiExecutionRecord>> GetExecutionsForRevisionAsync(
        string workflowKey,
        string stageKey,
        string revisionId,
        DateTime? fromInclusiveUtc,
        int take,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IList<AiExecutionRecord>>(scenario.Read<AiExecutionRecord>(DbCollections.AiExecutionRecords)
            .Where(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey && item.RevisionId == revisionId &&
                (!fromInclusiveUtc.HasValue || item.CreatedAtUtc >= fromInclusiveUtc.Value))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 1000)).ToList());

    public Task AddEvaluationCaseAsync(AiEvaluationCase evaluationCase, CancellationToken cancellationToken = default)
    {
        scenario.Add(DbCollections.AiEvaluationCases, evaluationCase);
        return Task.CompletedTask;
    }

    public Task<IList<AiEvaluationCase>> GetEvaluationCasesAsync(string workflowKey, string stageKey, string datasetVersion, IReadOnlyCollection<string>? caseIds = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IList<AiEvaluationCase>>(scenario.Read<AiEvaluationCase>(DbCollections.AiEvaluationCases)
            .Where(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey &&
                item.DatasetVersion == datasetVersion && item.RedactionStatus == "approved" &&
                (caseIds is not { Count: > 0 } || caseIds.Contains(item.Id)))
            .OrderBy(item => item.CreationDateTime).ToList());

    public Task AddExperimentAsync(AiPromptExperiment experiment, CancellationToken cancellationToken = default)
    {
        scenario.Add(DbCollections.AiPromptExperiments, experiment);
        return Task.CompletedTask;
    }

    public Task<AiPromptExperiment?> GetExperimentAsync(string experimentId, CancellationToken cancellationToken = default)
        => Task.FromResult(scenario.Read<AiPromptExperiment>(DbCollections.AiPromptExperiments).FirstOrDefault(item => item.Id == experimentId));

    public Task<IList<AiPromptExperiment>> GetExperimentsAsync(string workflowKey, string stageKey, CancellationToken cancellationToken = default)
        => Task.FromResult<IList<AiPromptExperiment>>(scenario.Read<AiPromptExperiment>(DbCollections.AiPromptExperiments)
            .Where(item => item.WorkflowKey == workflowKey && item.StageKey == stageKey)
            .OrderByDescending(item => item.CreatedAtUtc).ToList());

    public Task AddAuditEventAsync(AiPromptAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        scenario.Add(DbCollections.AiPromptAudit, auditEvent);
        return Task.CompletedTask;
    }
}
