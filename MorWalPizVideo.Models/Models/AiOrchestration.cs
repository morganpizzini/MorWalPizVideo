using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public sealed record AiPromptDefinition : BaseEntity
{
    [BsonElement("workflowKey")] public string WorkflowKey { get; init; } = string.Empty;
    [BsonElement("stageKey")] public string StageKey { get; init; } = string.Empty;
    [BsonElement("displayName")] public string DisplayName { get; init; } = string.Empty;
    [BsonElement("activeRevisionId")] public string? ActiveRevisionId { get; init; }
    [BsonElement("previousRevisionId")] public string? PreviousRevisionId { get; init; }
    [BsonElement("canaryRevisionId")] public string? CanaryRevisionId { get; init; }
    [BsonElement("canaryPercentage")] public int CanaryPercentage { get; init; }
    [BsonElement("canaryExperimentId")] public string? CanaryExperimentId { get; init; }
    [BsonElement("canaryStartedAtUtc")] public DateTime? CanaryStartedAtUtc { get; init; }
    [BsonElement("allowedToolNames")] public List<string> AllowedToolNames { get; init; } = [];
    [BsonElement("updatedAtUtc")] public DateTime UpdatedAtUtc { get; init; }
}

public sealed record AiPromptMessage
{
    [BsonElement("role")] public string Role { get; init; } = string.Empty;
    [BsonElement("template")] public string Template { get; init; } = string.Empty;
}

public sealed record AiPromptRevision : BaseEntity
{
    [BsonElement("workflowKey")] public string WorkflowKey { get; init; } = string.Empty;
    [BsonElement("stageKey")] public string StageKey { get; init; } = string.Empty;
    [BsonElement("messages")] public List<AiPromptMessage> Messages { get; init; } = [];
    [BsonElement("variables")] public List<string> Variables { get; init; } = [];
    [BsonElement("responseSchema")] public string ResponseSchema { get; init; } = string.Empty;
    [BsonElement("allowedToolNames")] public List<string> AllowedToolNames { get; init; } = [];
    [BsonElement("parentRevisionId")] public string? ParentRevisionId { get; init; }
    [BsonElement("sourceExperimentId")] public string? SourceExperimentId { get; init; }
    [BsonElement("rationale")] public string Rationale { get; init; } = string.Empty;
    [BsonElement("authorId")] public string AuthorId { get; init; } = string.Empty;
    [BsonElement("status")] public string Status { get; init; } = AiPromptRevisionStatus.Draft;
    [BsonElement("contentHash")] public string ContentHash { get; init; } = string.Empty;
    [BsonElement("createdAtUtc")] public DateTime CreatedAtUtc { get; init; }
}

public static class AiPromptRevisionStatus
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Retired = "Retired";
    public const string Rejected = "Rejected";
}

public sealed record AiExecutionRecord : BaseEntity
{
    [BsonElement("executionId")] public string ExecutionId { get; init; } = string.Empty;
    [BsonElement("workflowKey")] public string WorkflowKey { get; init; } = string.Empty;
    [BsonElement("stageKey")] public string StageKey { get; init; } = string.Empty;
    [BsonElement("revisionId")] public string RevisionId { get; init; } = string.Empty;
    [BsonElement("channelId")] public string? ChannelId { get; init; }
    [BsonElement("status")] public string Status { get; init; } = string.Empty;
    [BsonElement("errorCode")] public string? ErrorCode { get; init; }
    [BsonElement("durationMilliseconds")] public long DurationMilliseconds { get; init; }
    [BsonElement("inputCount")] public int InputCount { get; init; }
    [BsonElement("outputCount")] public int OutputCount { get; init; }
    [BsonElement("createdAtUtc")] public DateTime CreatedAtUtc { get; init; }
    [BsonElement("expiresAtUtc")] public DateTime ExpiresAtUtc { get; init; }
}

public sealed record AiEvaluationCase : BaseEntity
{
    [BsonElement("workflowKey")] public string WorkflowKey { get; init; } = string.Empty;
    [BsonElement("stageKey")] public string StageKey { get; init; } = string.Empty;
    [BsonElement("datasetVersion")] public string DatasetVersion { get; init; } = string.Empty;
    [BsonElement("inputJson")] public string InputJson { get; init; } = string.Empty;
    [BsonElement("expectedContains")] public List<string> ExpectedContains { get; init; } = [];
    [BsonElement("sourceClassification")] public string SourceClassification { get; init; } = string.Empty;
    [BsonElement("redactionStatus")] public string RedactionStatus { get; init; } = string.Empty;
    [BsonElement("createdBy")] public string CreatedBy { get; init; } = string.Empty;
    [BsonElement("createdAtUtc")] public DateTime CreatedAtUtc { get; init; }
    [BsonElement("expiresAtUtc")] public DateTime ExpiresAtUtc { get; init; }
}

public sealed record AiPromptExperimentCaseResult
{
    [BsonElement("caseId")] public string CaseId { get; init; } = string.Empty;
    [BsonElement("baselinePassed")] public bool BaselinePassed { get; init; }
    [BsonElement("candidatePassed")] public bool CandidatePassed { get; init; }
    [BsonElement("baselineDurationMilliseconds")] public long BaselineDurationMilliseconds { get; init; }
    [BsonElement("candidateDurationMilliseconds")] public long CandidateDurationMilliseconds { get; init; }
}

public sealed record AiPromptExperiment : BaseEntity
{
    [BsonElement("workflowKey")] public string WorkflowKey { get; init; } = string.Empty;
    [BsonElement("stageKey")] public string StageKey { get; init; } = string.Empty;
    [BsonElement("baselineRevisionId")] public string BaselineRevisionId { get; init; } = string.Empty;
    [BsonElement("candidateRevisionId")] public string CandidateRevisionId { get; init; } = string.Empty;
    [BsonElement("datasetVersion")] public string DatasetVersion { get; init; } = string.Empty;
    [BsonElement("scoringPolicyVersion")] public string ScoringPolicyVersion { get; init; } = string.Empty;
    [BsonElement("caseResults")] public List<AiPromptExperimentCaseResult> CaseResults { get; init; } = [];
    [BsonElement("status")] public string Status { get; init; } = string.Empty;
    [BsonElement("createdBy")] public string CreatedBy { get; init; } = string.Empty;
    [BsonElement("createdAtUtc")] public DateTime CreatedAtUtc { get; init; }
}

public sealed record AiPromptAuditEvent : BaseEntity
{
    [BsonElement("workflowKey")] public string WorkflowKey { get; init; } = string.Empty;
    [BsonElement("stageKey")] public string StageKey { get; init; } = string.Empty;
    [BsonElement("action")] public string Action { get; init; } = string.Empty;
    [BsonElement("actorId")] public string ActorId { get; init; } = string.Empty;
    [BsonElement("previousRevisionId")] public string? PreviousRevisionId { get; init; }
    [BsonElement("revisionId")] public string? RevisionId { get; init; }
    [BsonElement("experimentId")] public string? ExperimentId { get; init; }
    [BsonElement("createdAtUtc")] public DateTime CreatedAtUtc { get; init; }
}
