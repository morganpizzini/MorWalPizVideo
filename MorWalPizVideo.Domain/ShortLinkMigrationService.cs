using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public sealed record ShortLinkMigrationOptions(
    bool DryRun = false,
    bool Resume = true,
    int BatchSize = 100,
    bool InventoryOnly = false,
    bool RollbackOnFailure = false);

public sealed record ShortLinkMigrationReport(
    bool DryRun,
    bool Completed,
    string? LastProcessedMatchId,
    int InspectedMatches,
    int CanonicalLinksCreated,
    int EmbeddedLinksRemoved,
    IReadOnlyList<string> Conflicts,
    IReadOnlyList<string> MissingReferences,
    IReadOnlyList<string>? DuplicateCodes = null,
    IReadOnlyList<string>? MalformedCodes = null,
    IReadOnlyList<string>? ArchivalExceptions = null,
    int CanonicalLinksReplaced = 0,
    IReadOnlyList<string>? Failures = null,
    string? ResumeMarker = null,
    string? RollbackStatus = null)
{
    public IReadOnlyList<string> DuplicateCodes { get; } = DuplicateCodes ?? [];
    public IReadOnlyList<string> MalformedCodes { get; } = MalformedCodes ?? [];
    public IReadOnlyList<string> ArchivalExceptions { get; } = ArchivalExceptions ?? [];
    public IReadOnlyList<string> Failures { get; } = Failures ?? [];
    public bool ArchivalReady => ArchivalExceptions.Count == 0;
    public bool RollbackAttempted => RollbackStatus is not null;
}

public interface IShortLinkMigrationService
{
    Task<ShortLinkMigrationReport> RunAsync(ShortLinkMigrationOptions options, CancellationToken cancellationToken = default);
}

public sealed class ShortLinkMigrationService(
    IYouTubeContentRepository matchRepository,
    IShortLinkRepository shortLinkRepository,
    IConfigurationRepository configurationRepository) : IShortLinkMigrationService
{
    private const string MarkerKey = "migration.short-links.v1";

    public async Task<ShortLinkMigrationReport> RunAsync(
        ShortLinkMigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        var batchSize = Math.Clamp(options.BatchSize, 1, 1000);
        var matches = (await matchRepository.GetItemsAsync())
            .OrderBy(match => match.Id, StringComparer.Ordinal)
            .ToList();
        var standalone = await shortLinkRepository.GetItemsAsync();
        var duplicateCodes = standalone
            .GroupBy(link => link.NormalizedCode, StringComparer.Ordinal)
            .Where(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1)
            .Select(group => string.IsNullOrWhiteSpace(group.Key)
                ? $"empty-code:{string.Join(',', group.Select(link => link.Id).Order(StringComparer.Ordinal))}"
                : $"code:{group.Key}:{string.Join(',', group.Select(link => link.Id).Order(StringComparer.Ordinal))}")
            .Order(StringComparer.Ordinal)
            .ToArray();
        var malformedCodes = standalone
            .Where(link => string.IsNullOrWhiteSpace(link.NormalizedCode))
            .Select(link => link.Id)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var marker = options.Resume ? await ReadMarkerAsync() : null;
        if (marker == "complete")
        {
            var markerExceptions = duplicateCodes.Concat(malformedCodes).ToArray();
            return new(options.DryRun, markerExceptions.Length == 0, null, 0, 0, 0, [], [],
                duplicateCodes, malformedCodes, markerExceptions, ResumeMarker: marker);
        }

        var candidates = matches
            .Where(match => marker is null || string.CompareOrdinal(match.Id, marker) > 0)
            .Take(options.DryRun || options.InventoryOnly ? int.MaxValue : batchSize)
            .ToList();
        var conflicts = new List<string>();
        var missingReferences = new List<string>();
        var failures = new List<string>();
        var created = 0;
        var replaced = 0;
        var removed = 0;
        var rollbackStatuses = new List<string>();

        foreach (var match in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var embeddedVideoLinks = match.ShortLinks
                .Where(link => link.LinkType == LinkType.YouTubeVideo)
                .OrderBy(link => link.Id, StringComparer.Ordinal)
                .ToArray();
            var linksToRemove = new List<ShortLink>();
            var changes = new List<(ShortLink? Previous, ShortLink Persisted)>();
            var matchFailures = new List<string>();

            foreach (var embedded in embeddedVideoLinks)
            {
                if (!match.VideoRefs.Any(video => video.YoutubeId == embedded.Target))
                {
                    missingReferences.Add($"{match.Id}:{embedded.Id}:{embedded.Target}");
                    continue;
                }

                var normalizedCode = ShortLink.NormalizeCode(embedded.Code);
                if (normalizedCode.Length == 0)
                {
                    conflicts.Add($"empty-code:{match.Id}:{embedded.Id}");
                    continue;
                }

                var existing = standalone
                    .Where(link => link.MatchesCode(normalizedCode))
                    .OrderBy(link => link.Id, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (existing is not null &&
                    (existing.Target != embedded.Target || existing.ContentId != match.Id))
                {
                    conflicts.Add($"code:{normalizedCode}:{existing.Id}:{embedded.Id}");
                }

                var canonical = MergeCanonical(existing, embedded, match);
                if (existing is null)
                    created++;
                else if (existing.Target != canonical.Target || existing.ContentId != canonical.ContentId)
                    replaced++;

                if (!options.DryRun)
                {
                    try
                    {
                        var persisted = await shortLinkRepository.ReplaceByNormalizedCodeAsync(
                            canonical,
                            cancellationToken);
                        if (!IsValidCanonical(persisted, match.Id, embedded.Target, normalizedCode))
                            throw new InvalidOperationException($"Canonical validation failed for code '{normalizedCode}'.");

                        changes.Add((existing, persisted));
                        standalone = standalone
                            .Where(link => !link.MatchesCode(normalizedCode))
                            .ToList();
                        standalone.Add(persisted);
                        linksToRemove.Add(embedded);
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        matchFailures.Add($"{match.Id}:{embedded.Id}:{exception.GetType().Name}:{exception.Message}");
                    }
                }
                else if (IsValidCanonical(canonical, match.Id, embedded.Target, normalizedCode))
                {
                    linksToRemove.Add(embedded);
                }
            }

            if (matchFailures.Count == 0 && !options.DryRun && linksToRemove.Count > 0)
            {
                try
                {
                    await matchRepository.UpdateItemAsync(match with
                    {
                        ShortLinks = match.ShortLinks.Except(linksToRemove).ToArray()
                    });
                    removed += linksToRemove.Count;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    matchFailures.Add($"{match.Id}:embedded-cleanup:{exception.GetType().Name}:{exception.Message}");
                }
            }
            else if (options.DryRun)
            {
                removed += linksToRemove.Count;
            }

            if (matchFailures.Count > 0)
            {
                failures.AddRange(matchFailures);
                if (!options.DryRun && options.RollbackOnFailure)
                    rollbackStatuses.Add(await RollbackAsync(changes, cancellationToken));
            }
            else if (!options.DryRun)
            {
                await WriteMarkerAsync(match.Id);
            }
        }

        var archivalExceptions = duplicateCodes
            .Concat(malformedCodes)
            .Concat(missingReferences)
            .Concat(failures)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var completed =
            candidates.Count < (options.DryRun || options.InventoryOnly ? int.MaxValue : batchSize) &&
            archivalExceptions.Length == 0 &&
            failures.Count == 0;
        if (!options.DryRun && completed)
            await WriteMarkerAsync("complete");

        return new(options.DryRun, completed, candidates.LastOrDefault()?.Id, candidates.Count, created, removed,
            conflicts.Order(StringComparer.Ordinal).ToArray(), missingReferences.Order(StringComparer.Ordinal).ToArray(),
            duplicateCodes, malformedCodes, archivalExceptions, replaced, failures,
            candidates.LastOrDefault()?.Id, rollbackStatuses.Count == 0 ? null : string.Join(';', rollbackStatuses));
    }

    private static ShortLink MergeCanonical(ShortLink? existing, ShortLink embedded, YouTubeContent match)
        => embedded with
        {
            Id = existing?.Id ?? embedded.Id,
            Code = ShortLink.NormalizeCode(embedded.Code),
            ContentId = match.Id,
            ClicksCount = Math.Max(existing?.ClicksCount ?? 0, embedded.ClicksCount),
            QueryLinks = embedded.QueryLinks is { Count: > 0 }
                ? embedded.QueryLinks
                : existing?.QueryLinks ?? [],
            ChannelId = embedded.ChannelId ?? existing?.ChannelId,
            CampaignId = embedded.CampaignId ?? existing?.CampaignId,
            SponsorId = embedded.SponsorId ?? existing?.SponsorId,
            ManagementChannelId = embedded.ManagementChannelId ?? existing?.ManagementChannelId ?? match.OwnerChannelId,
            CreationDateTime = existing?.CreationDateTime ?? embedded.CreationDateTime
        };

    private static bool IsValidCanonical(ShortLink link, string contentId, string target, string normalizedCode)
        => link.LinkType == LinkType.YouTubeVideo &&
           link.ContentId == contentId &&
           link.Target == target &&
           link.NormalizedCode == normalizedCode;

    private async Task<string> RollbackAsync(
        IReadOnlyList<(ShortLink? Previous, ShortLink Persisted)> changes,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var change in changes.Reverse())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (change.Previous is null)
                    await shortLinkRepository.DeleteItemAsync(change.Persisted.Id);
                else
                    await shortLinkRepository.ReplaceByNormalizedCodeAsync(change.Previous, cancellationToken);
            }

            return changes.Count == 0 ? "not-needed" : "completed";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return $"failed:{exception.GetType().Name}:{exception.Message}";
        }
    }

    private async Task<string?> ReadMarkerAsync()
        => (await configurationRepository.GetItemsAsync(item => item.Key == MarkerKey)).FirstOrDefault()?.Value?.ToString();

    private async Task WriteMarkerAsync(string value)
    {
        var existing = (await configurationRepository.GetItemsAsync(item => item.Key == MarkerKey)).FirstOrDefault();
        var marker = new MorWalPizConfiguration(MarkerKey, value, "string", "Short-link V1 migration progress")
        {
            Id = existing?.Id ?? string.Empty
        };
        if (existing is null)
            await configurationRepository.AddItemAsync(marker);
        else
            await configurationRepository.UpdateItemAsync(marker);
    }
}