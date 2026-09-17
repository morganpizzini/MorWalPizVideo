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

    public static ChannelTerminologyContract ToContract(ChannelTerminologyConfiguration configuration) => new(
        configuration.ItalianToEnglish.Select(ToContract).ToArray(),
        configuration.InvariantEnglish.Select(ToContract).ToArray());

    private static TerminologyMappingContract ToContract(TerminologyMapping mapping) =>
        new(mapping.Source, mapping.Target);
}