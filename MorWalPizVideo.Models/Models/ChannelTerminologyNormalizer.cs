namespace MorWalPizVideo.Server.Models;

public sealed record ChannelTerminologyNormalization(
    ChannelTerminologyConfiguration Configuration,
    string? Error)
{
    public bool IsValid => Error is null;
}

public static class ChannelTerminologyNormalizer
{
    public static ChannelTerminologyNormalization Normalize(ChannelTerminologyConfiguration? configuration)
    {
        if (configuration is null)
            return Empty();

        var italian = NormalizeGroup(configuration.ItalianToEnglish, "italianToEnglish");
        if (!italian.IsValid) return italian;
        var invariant = NormalizeGroup(configuration.InvariantEnglish, "invariantEnglish");
        if (!invariant.IsValid) return invariant;

        return new(
            new ChannelTerminologyConfiguration
            {
                ItalianToEnglish = italian.Configuration.ItalianToEnglish,
                InvariantEnglish = invariant.Configuration.InvariantEnglish
            }, null);
    }

    private static ChannelTerminologyNormalization NormalizeGroup(
        IEnumerable<TerminologyMapping>? mappings, string group)
    {
        var normalized = new List<TerminologyMapping>();
        foreach (var mapping in mappings ?? [])
        {
            if (mapping is null || string.IsNullOrWhiteSpace(mapping.Source))
                return Invalid(group);

            var source = mapping.Source.Trim();
            var target = mapping.Target?.Trim() ?? string.Empty;
            if (normalized.Any(item => string.Equals(item.Source, source, StringComparison.OrdinalIgnoreCase)))
                return Invalid($"{group}:duplicate");

            normalized.Add(new() { Source = source, Target = target });
        }

        return new(new ChannelTerminologyConfiguration
        {
            ItalianToEnglish = group == "italianToEnglish" ? normalized : [],
            InvariantEnglish = group == "invariantEnglish" ? normalized : []
        }, null);
    }

    private static ChannelTerminologyNormalization Invalid(string reason) =>
        new(new ChannelTerminologyConfiguration(), reason);

    private static ChannelTerminologyNormalization Empty() =>
        new(new ChannelTerminologyConfiguration(), null);
}
