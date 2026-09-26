using System.ComponentModel.DataAnnotations;

namespace MorWalPiz.Contracts.DTOs;

public sealed class ScriptStudioDocumentDto
{
    public string ChannelId { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public string SavedPrompt { get; set; } = string.Empty;
    public string Examples { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string GeneralContext { get; set; } = string.Empty;
    public string SavedResult { get; set; } = string.Empty;
    public string Format { get; set; } = "markdown";
}

public sealed class ScriptStudioGenerationRequest
{
    [Required] public string Operation { get; set; } = string.Empty;
    [Required] public string Script { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Examples { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string GeneralContext { get; set; } = string.Empty;
    public string Format { get; set; } = "markdown";
}

public sealed class ScriptStudioGenerationResponse
{
    public string Result { get; set; } = string.Empty;
    public string Format { get; set; } = "markdown";
    public int QuotaLimit { get; set; }
    public int QuotaUsed { get; set; }
}

public sealed class ScriptStudioGlobalPromptDto
{
    public string Prompt { get; set; } = string.Empty;
    public int Version { get; set; }
}

public sealed class ScriptStudioQuotaDto
{
    [Range(0, int.MaxValue)] public int MonthlyQuota { get; set; }
}
