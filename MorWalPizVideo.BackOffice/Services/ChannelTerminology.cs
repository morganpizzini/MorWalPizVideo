using MorWalPizVideo.Server.Models;
using MorWalPizVideo.BackOffice.DTOs;

namespace MorWalPizVideo.BackOffice.Services;

public static class ChannelTerminologyRules
{
    public const int MaxMappingsPerGroup = 100;
    public const int MaxTermLength = 80;

    public static bool TryNormalize(
        IEnumerable<DTOs.TerminologyMappingRequest>? requests,
        out List<TerminologyMapping> mappings,
        out string? error)
    {
        mappings = [];
        error = null;
        var values = requests?.ToList() ?? [];
        if (values.Count > MaxMappingsPerGroup)
        {
            error = $"A terminology group cannot contain more than {MaxMappingsPerGroup} mappings.";
            return false;
        }

        foreach (var request in values)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Source))
            {
                error = "Terminology source is required.";
                return false;
            }
            var source = request.Source.Trim();
            var target = request.Target?.Trim() ?? string.Empty;
            if (source.Length == 0 || source.Length > MaxTermLength || target.Length > MaxTermLength)
            {
                error = $"Terminology terms must be between 1 and {MaxTermLength} characters.";
                return false;
            }

            if (mappings.Any(item => string.Equals(item.Source, source, StringComparison.OrdinalIgnoreCase)))
            {
                error = $"The terminology source '{source}' is duplicated.";
                return false;
            }

            mappings.Add(new TerminologyMapping { Source = source, Target = target });
        }

        return true;
    }

    public static bool TryToContract(ChannelTerminologyConfiguration? configuration,
        out ChannelTerminologyContract contract, out string? error)
    {
        var normalized = ChannelTerminologyNormalizer.Normalize(configuration);
        if (!normalized.IsValid)
        {
            contract = new([], []);
            error = normalized.Error;
            return false;
        }

        contract = new(
            normalized.Configuration.ItalianToEnglish!.Select(ToContract).ToArray(),
            normalized.Configuration.InvariantEnglish!.Select(ToContract).ToArray());
        error = null;
        return true;
    }

    private static TerminologyMappingContract ToContract(TerminologyMapping mapping) =>
        new(mapping.Source ?? string.Empty, mapping.Target ?? string.Empty);
}