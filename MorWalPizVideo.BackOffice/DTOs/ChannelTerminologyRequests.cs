using System.ComponentModel.DataAnnotations;

namespace MorWalPizVideo.BackOffice.DTOs;

public sealed class ChannelTerminologyRequest
{
    public List<TerminologyMappingRequest> ItalianToEnglish { get; set; } = [];
    public List<TerminologyMappingRequest> InvariantEnglish { get; set; } = [];
}

public sealed class TerminologyMappingRequest
{
    [Required]
    public string Source { get; set; } = string.Empty;

    public string? Target { get; set; }
}

public sealed record ChannelTerminologyContract(
    IReadOnlyList<TerminologyMappingContract> ItalianToEnglish,
    IReadOnlyList<TerminologyMappingContract> InvariantEnglish);

public sealed record TerminologyMappingContract(string Source, string Target);