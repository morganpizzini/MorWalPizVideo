namespace MorWalPiz.Contracts.DTOs;

public sealed class AiPromptMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
}

public sealed class AiPromptDefinitionDto
{
    public string WorkflowKey { get; set; } = string.Empty;
    public string StageKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ResponseSchema { get; set; } = string.Empty;
    public string? ActiveRevisionId { get; set; }
    public string? PreviousRevisionId { get; set; }
    public string? CanaryRevisionId { get; set; }
    public int CanaryPercentage { get; set; }
    public string? CanaryExperimentId { get; set; }
    public DateTime? CanaryStartedAtUtc { get; set; }
    public List<string> AllowedToolNames { get; set; } = [];
}

public sealed class AiPromptRevisionDto
{
    public string Id { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string StageKey { get; set; } = string.Empty;
    public List<AiPromptMessageDto> Messages { get; set; } = [];
    public List<string> Variables { get; set; } = [];
    public string ResponseSchema { get; set; } = string.Empty;
    public List<string> AllowedToolNames { get; set; } = [];
    public string? ParentRevisionId { get; set; }
    public string? SourceExperimentId { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string Lifecycle { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CreateAiPromptRevisionRequest
{
    public List<AiPromptMessageDto> Messages { get; set; } = [];
    public List<string> Variables { get; set; } = [];
    public string ResponseSchema { get; set; } = string.Empty;
    public List<string> AllowedToolNames { get; set; } = [];
    public string Rationale { get; set; } = string.Empty;
}

public sealed class CreateAiEvaluationCaseRequest
{
    public string WorkflowKey { get; set; } = string.Empty;
    public string StageKey { get; set; } = string.Empty;
    public string DatasetVersion { get; set; } = string.Empty;
    public string InputJson { get; set; } = string.Empty;
    public List<string> ExpectedContains { get; set; } = [];
    public string SourceClassification { get; set; } = string.Empty;
    public bool ContentConsentGranted { get; set; }
    public bool ContentRedacted { get; set; }
}

public sealed class EvaluateAiPromptRequest
{
    public string CandidateRevisionId { get; set; } = string.Empty;
    public string DatasetVersion { get; set; } = string.Empty;
    public List<string> CaseIds { get; set; } = [];
}

public sealed class RollbackAiPromptRequest
{
    public string ExpectedActiveRevisionId { get; set; } = string.Empty;
}

public sealed class StartAiPromptCanaryRequest
{
    public string CandidateRevisionId { get; set; } = string.Empty;
    public string ExpectedActiveRevisionId { get; set; } = string.Empty;
    public string ExperimentId { get; set; } = string.Empty;
    public int Percentage { get; set; }
}

public sealed class PromoteAiPromptCanaryRequest
{
    public string ExpectedActiveRevisionId { get; set; } = string.Empty;
    public string ExpectedCanaryRevisionId { get; set; } = string.Empty;
    public string ExperimentId { get; set; } = string.Empty;
}

public sealed class CancelAiPromptCanaryRequest
{
    public string ExpectedActiveRevisionId { get; set; } = string.Empty;
    public string ExpectedCanaryRevisionId { get; set; } = string.Empty;
}

public sealed class AiPromptExperimentDto
{
    public string Id { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string StageKey { get; set; } = string.Empty;
    public string BaselineRevisionId { get; set; } = string.Empty;
    public string CandidateRevisionId { get; set; } = string.Empty;
    public string DatasetVersion { get; set; } = string.Empty;
    public string ScoringPolicyVersion { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int CasesEvaluated { get; set; }
    public int BaselinePassCount { get; set; }
    public int CandidatePassCount { get; set; }
    public long BaselineDurationMilliseconds { get; set; }
    public long CandidateDurationMilliseconds { get; set; }
}

public sealed class AiExecutionRecordDto
{
    public string ExecutionId { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string StageKey { get; set; } = string.Empty;
    public string RevisionId { get; set; } = string.Empty;
    public string? ChannelId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public long DurationMilliseconds { get; set; }
    public int InputCount { get; set; }
    public int OutputCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
