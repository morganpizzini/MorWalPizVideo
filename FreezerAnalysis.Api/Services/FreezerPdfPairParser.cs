using System.Globalization;
using System.Text.RegularExpressions;
using MorWalPiz.Contracts.Contracts.FreezerAnalysis;

namespace FreezerAnalysis.Api.Services;

public sealed class FreezerPdfPairParser(FreezerPdfReportDetector reportDetector)
{
    private static readonly Regex ClassificationPageTitle = new(
        @"^\s*(?<division>.+?)\s+--\s+Overall Match Results\b",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ClassificationRow = new(
        @"^\s*(?<rank>\d+)\s+(?<percent>\d{1,3}[,.]\d{2})\s+(?<points>\d{1,4}[,.]\d{4})\s+",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex VerificationEntry = new(
        @"^\s*(?<competitorNumber>\d+)\s+(?<name>.*?)\s+\bDIV\s*:\s*(?<division>\S+)\s+CLASS\s*:",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex PowerFactor = new(
        @"\bFACTOR\s*:\s*(?<factor>Major|Minor)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex VerificationStage = new(
        @"^\s*Stage\s+(?<stage>\d+)\s+(?<values>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly IReadOnlyDictionary<string, string> VerificationDivisionNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["O"] = "OPEN",
            ["OP"] = "OPTICS",
            ["P"] = "PRODUCTION",
            ["PCI"] = "PCC IRON",
            ["PCO"] = "PCC OPTICS",
            ["PO"] = "PRODUCTION OPTICS",
            ["R"] = "REVOLVER",
            ["S"] = "STANDARD"
        };

    public FreezerPdfPairPreviewResponse Preview(string firstDocument, string secondDocument)
    {
        var first = reportDetector.Classify(firstDocument);
        var second = reportDetector.Classify(secondDocument);
        var overall = first.ReportFamily == "overall-classification" ? first : second;
        var verification = first.ReportFamily == "score-verification" ? first : second;
        var diagnostics = new List<string>();

        if (overall.ReportFamily != "overall-classification" ||
            verification.ReportFamily != "score-verification")
        {
            diagnostics.Add("Select exactly one Overall classification report and one score verification report.");
            return CreateResponse(
                overall,
                verification,
                0,
                0,
                0,
                0,
                0,
                overall.DisqualifiedCompetitorCount,
                overall.DisqualifiedCompetitors
                    .Select(entry => new FreezerPdfDisqualificationPreview(
                        entry.DisplayName,
                        NormalizeDivision(entry.Division),
                        entry.PageNumber,
                        "unmatched"))
                    .ToArray(),
                diagnostics,
                [],
                [],
                0,
                0);
        }

        var classificationEntries = ParseClassificationEntries(overall.LayoutText);
        var verificationEntries = ParseVerificationEntries(
            verification.LayoutText,
            diagnostics,
            out var parsedStageResultCount,
            out var hitFactorTimeMismatchCount);
        var classificationGroups = classificationEntries
            .GroupBy(entry => PairKey(entry.NormalizedName, entry.Division), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var verificationGroups = verificationEntries
            .GroupBy(entry => PairKey(entry.NormalizedName, entry.Division), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var pairedStates = new Dictionary<string, string>(StringComparer.Ordinal);
        var unmatchedClassification = 0;
        var unmatchedVerification = 0;
        var ambiguousGroups = 0;

        foreach (var key in classificationGroups.Keys
                     .Union(verificationGroups.Keys, StringComparer.Ordinal)
                     .Order(StringComparer.Ordinal))
        {
            var classificationCount = classificationGroups.GetValueOrDefault(key);
            var verificationCount = verificationGroups.GetValueOrDefault(key);
            if (classificationCount == 1 && verificationCount == 1)
            {
                pairedStates.Add(key, "matched");
            }
            else if (classificationCount > 1 || verificationCount > 1)
            {
                ambiguousGroups++;
                pairedStates.Add(key, "ambiguous");
            }
            else
            {
                unmatchedClassification += classificationCount;
                unmatchedVerification += verificationCount;
                pairedStates.Add(
                    key,
                    classificationCount > 0 ? "classification_unmatched" : "verification_unmatched");
            }
        }

        var disqualifiedGroups = overall.DisqualifiedCompetitors
            .GroupBy(entry => PairKey(
                FreezerIdentityNormalizer.NormalizeName(entry.DisplayName),
                NormalizeDivision(entry.Division)), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var disqualifiedPreviews = overall.DisqualifiedCompetitors
            .Select(entry =>
            {
                var normalizedName = FreezerIdentityNormalizer.NormalizeName(entry.DisplayName);
                var division = NormalizeDivision(entry.Division);
                var key = PairKey(normalizedName, division);
                var classificationCount = classificationGroups.GetValueOrDefault(key);
                var verificationCount = verificationGroups.GetValueOrDefault(key);
                var ambiguous = disqualifiedGroups[key] > 1 || classificationCount > 1 || verificationCount > 1;
                var pairingStatus = ambiguous
                    ? "ambiguous"
                    : classificationCount == 1 && verificationCount == 1 ? "matched" : "unmatched";
                return new FreezerPdfDisqualificationPreview(
                    entry.DisplayName,
                    division,
                    entry.PageNumber,
                    pairingStatus);
            })
            .ToArray();
        var mappedDisqualifiedKeys = disqualifiedPreviews
            .Where(entry => entry.PairingStatus == "matched")
            .Select(entry => PairKey(
                FreezerIdentityNormalizer.NormalizeName(entry.DisplayName),
                entry.Division))
            .ToHashSet(StringComparer.Ordinal);
        var mappedDisqualifiedCount = mappedDisqualifiedKeys.Count;
        var unmatchedDisqualifiedCount = Math.Max(0, overall.DisqualifiedCompetitorCount - mappedDisqualifiedCount);
        var disqualificationMappingComplete = unmatchedDisqualifiedCount == 0;

        var classificationPreviews = classificationEntries
            .Select(entry =>
            {
                var key = PairKey(entry.NormalizedName, entry.Division);
                bool? isDisqualified = mappedDisqualifiedKeys.Contains(key)
                    ? true
                    : disqualificationMappingComplete ? false : null;
                return new FreezerPdfClassificationPreview(
                entry.DisplayName,
                entry.Division,
                entry.Rank,
                entry.Percent,
                entry.Points,
                entry.PageNumber,
                pairedStates[key],
                isDisqualified);
            })
            .ToArray();
        var verificationPreviews = verificationEntries
            .Select(entry =>
            {
                var key = PairKey(entry.NormalizedName, entry.Division);
                bool? isDisqualified = mappedDisqualifiedKeys.Contains(key)
                    ? true
                    : disqualificationMappingComplete ? false : null;
                return new FreezerPdfVerificationPreview(
                entry.CompetitorNumber,
                entry.DisplayName,
                entry.Division,
                entry.PowerFactor,
                entry.PageNumber,
                pairedStates[key],
                isDisqualified,
                entry.Stages.ToArray());
            })
            .ToArray();
        var matched = pairedStates.Count(pair => pair.Value == "matched");

        if (classificationEntries.Count != overall.CompetitorCount)
            diagnostics.Add($"Classification row coverage mismatch: {classificationEntries.Count} of {overall.CompetitorCount} counted entries were parsed.");
        if (verificationEntries.Sum(entry => entry.Stages.Count) != verification.StageResultCount)
            diagnostics.Add($"Verification row coverage mismatch: {verificationEntries.Sum(entry => entry.Stages.Count)} of {verification.StageResultCount} counted stage rows were parsed.");
        if (ambiguousGroups > 0)
            diagnostics.Add("Normalized name and division collisions require explicit review before the reports can be joined.");
        if (unmatchedClassification > 0 || unmatchedVerification > 0)
            diagnostics.Add("Some classification and verification entries have no unique counterpart.");
        if (hitFactorTimeMismatchCount > 0)
            diagnostics.Add($"{hitFactorTimeMismatchCount} parsed stage rows do not reconcile reported hit factor with reported points and time at four-decimal precision.");
        diagnostics.Add("Stage minimum rounds and per-division stage maximum points are not established by these reports; official total reconciliation and publication remain disabled.");
        if (overall.DisqualifiedCompetitorCount > 0)
        {
            diagnostics.Add(unmatchedDisqualifiedCount == 0
                ? "Disqualification appendix entries were uniquely joined to classification and Verify rows by normalized name and division."
                : $"{unmatchedDisqualifiedCount} disqualification appendix entries could not be joined uniquely; {mappedDisqualifiedCount}/{overall.DisqualifiedCompetitorCount} were matched to both classification and Verify rows, and unresolved statuses remain unverified.");
        }
        diagnostics.Add("Pairing uses normalized name and division because no shared stable competitor identifier was found in both reports.");

        return CreateResponse(
            overall,
            verification,
            matched,
            unmatchedClassification,
            unmatchedVerification,
            ambiguousGroups,
            mappedDisqualifiedCount,
            unmatchedDisqualifiedCount,
            disqualifiedPreviews,
            diagnostics,
            classificationPreviews,
            verificationPreviews,
            parsedStageResultCount,
            hitFactorTimeMismatchCount);
    }

    private static List<ClassificationEntry> ParseClassificationEntries(string text)
    {
        var entries = new List<ClassificationEntry>();
        var pages = text.Split('\f');
        for (var pageIndex = 0; pageIndex < pages.Length; pageIndex++)
        {
            var page = pages[pageIndex];
            var title = ClassificationPageTitle.Match(page);
            if (!title.Success)
                continue;
            var division = NormalizeDivision(title.Groups["division"].Value);
            var lines = page.Split('\n');
            var header = lines.FirstOrDefault(line =>
                line.Contains("Competitor", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("Cat", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("Reg", StringComparison.OrdinalIgnoreCase));
            if (header is null)
                continue;

            var nameStart = header.IndexOf("Competitor", StringComparison.OrdinalIgnoreCase);
            var nameEnd = header.IndexOf("Cat", nameStart, StringComparison.OrdinalIgnoreCase);
            if (nameStart < 0 || nameEnd <= nameStart)
                continue;

            foreach (var line in lines.Where(line => ClassificationRow.IsMatch(line)))
            {
                var row = ClassificationRow.Match(line);
                if (!row.Success || line.Length <= nameStart)
                    continue;
                var name = line.Substring(nameStart, Math.Min(nameEnd, line.Length) - nameStart).Trim();
                var normalizedName = FreezerIdentityNormalizer.NormalizeName(name);
                if (normalizedName.Length > 0)
                    entries.Add(new ClassificationEntry(
                        name,
                        normalizedName,
                        division,
                        int.Parse(row.Groups["rank"].Value, CultureInfo.InvariantCulture),
                        ParseDecimal(row.Groups["percent"].Value),
                        ParseDecimal(row.Groups["points"].Value),
                        pageIndex + 1));
            }
        }

        return entries;
    }

    private static List<VerificationEntryBuilder> ParseVerificationEntries(
        string text,
        ICollection<string> diagnostics,
        out int parsedStageResultCount,
        out int hitFactorTimeMismatchCount)
    {
        var entries = new List<VerificationEntryBuilder>();
        var entryByKey = new Dictionary<string, VerificationEntryBuilder>(StringComparer.Ordinal);
        VerificationEntryBuilder? currentEntry = null;
        parsedStageResultCount = 0;
        hitFactorTimeMismatchCount = 0;
        var pages = text.Split('\f');
        for (var pageIndex = 0; pageIndex < pages.Length; pageIndex++)
        {
            foreach (var line in pages[pageIndex].Split('\n'))
            {
                var header = VerificationEntry.Match(line);
                if (header.Success)
                {
                    var name = header.Groups["name"].Value.Trim();
                    var normalizedName = FreezerIdentityNormalizer.NormalizeName(name);
                    var divisionCode = header.Groups["division"].Value;
                    if (normalizedName.Length == 0)
                    {
                        diagnostics.Add("A verification entry has no readable competitor name and was not paired.");
                        currentEntry = null;
                        continue;
                    }

                    if (!VerificationDivisionNames.TryGetValue(divisionCode, out var division))
                    {
                        diagnostics.Add("A verification entry uses an unmapped division code and was not paired.");
                        currentEntry = null;
                        continue;
                    }

                    var competitorNumber = header.Groups["competitorNumber"].Value;
                    var key = $"{competitorNumber}|{division}|{normalizedName}";
                    if (!entryByKey.TryGetValue(key, out currentEntry))
                    {
                        var powerFactor = PowerFactor.Match(line);
                        currentEntry = new VerificationEntryBuilder(
                            competitorNumber,
                            name,
                            normalizedName,
                            NormalizeDivision(division),
                            powerFactor.Success ? powerFactor.Groups["factor"].Value.ToUpperInvariant() : null,
                            pageIndex + 1);
                        entryByKey.Add(key, currentEntry);
                        entries.Add(currentEntry);
                    }
                    continue;
                }

                var stageRow = VerificationStage.Match(line);
                if (!stageRow.Success)
                    continue;
                if (currentEntry is null)
                {
                    diagnostics.Add("A verification stage row appeared before a recognized competitor header.");
                    continue;
                }

                if (!TryParseStage(stageRow, pageIndex + 1, out var parsedStage, out var stageError))
                {
                    diagnostics.Add(stageError);
                    continue;
                }

                currentEntry.Stages.Add(parsedStage);
                parsedStageResultCount++;
                if (parsedStage.HitFactorMatchesReportedPointsAndTime == false)
                    hitFactorTimeMismatchCount++;
            }
        }

        return entries;
    }

    private static bool TryParseStage(
        Match stageRow,
        int pageNumber,
        out FreezerPdfStagePreview stage,
        out string diagnostic)
    {
        stage = default!;
        var tokens = stageRow.Groups["values"].Value.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length is not (10 or 11))
        {
            diagnostic = "A verification stage row has an unsupported number of columns and was not mapped.";
            return false;
        }

        var overtimeValue = tokens.Length == 11 ? tokens[9] : null;
        var timeToken = tokens.Length == 11 ? tokens[10] : tokens[9];
        if (!TryParseDecimal(tokens[0], out var hitFactor) ||
            !TryParseInt(tokens[1], out var points) ||
            !TryParseInt(tokens[2], out var alpha) ||
            !TryParseInt(tokens[3], out var charlie) ||
            !TryParseInt(tokens[4], out var delta) ||
            !TryParseInt(tokens[5], out var deduction) ||
            !TryParseInt(tokens[6], out var misses) ||
            !TryParseInt(tokens[7], out var noShoots) ||
            !TryParseInt(tokens[8], out var procedurals) ||
            !TryParseDecimal(timeToken, out var timeSeconds))
        {
            diagnostic = "A verification stage row contains a non-numeric required field and was not mapped.";
            return false;
        }

        var stageDiagnostics = new List<string>();
        bool? hitFactorMatches = null;
        if (timeSeconds <= 0)
        {
            stageDiagnostics.Add("Reported time is not positive; hit-factor reconciliation is unavailable.");
        }
        else
        {
            hitFactorMatches = Math.Abs(points / timeSeconds - hitFactor) <= 0.00005 + 1e-10;
            if (hitFactorMatches == false)
                stageDiagnostics.Add("Reported hit factor does not reconcile with reported points and time at four-decimal precision.");
        }

        stage = new FreezerPdfStagePreview(
            int.Parse(stageRow.Groups["stage"].Value, CultureInfo.InvariantCulture),
            hitFactor,
            points,
            alpha,
            charlie,
            delta,
            deduction,
            misses,
            noShoots,
            procedurals,
            overtimeValue,
            timeSeconds,
            pageNumber,
            hitFactorMatches,
            stageDiagnostics);
        diagnostic = string.Empty;
        return true;
    }

    private static bool TryParseDecimal(string value, out double result) =>
        double.TryParse(
            value.Replace(',', '.'),
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out result) && double.IsFinite(result);

    private static double ParseDecimal(string value) =>
        TryParseDecimal(value, out var result)
            ? result
            : throw new FormatException("A classification numeric field is invalid.");

    private static bool TryParseInt(string value, out int result) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    private static FreezerPdfPairPreviewResponse CreateResponse(
        PdfTextExtractionResult overall,
        PdfTextExtractionResult verification,
        int matched,
        int unmatchedClassification,
        int unmatchedVerification,
        int ambiguousGroups,
        int mappedDisqualifiedCount,
        int unmatchedDisqualifiedCount,
        IReadOnlyList<FreezerPdfDisqualificationPreview> disqualifiedEntries,
        IReadOnlyList<string> diagnostics,
        IReadOnlyList<FreezerPdfClassificationPreview> classificationEntries,
        IReadOnlyList<FreezerPdfVerificationPreview> verificationEntries,
        int parsedStageResultCount,
        int hitFactorTimeMismatchCount) =>
        new(
            overall.ReportFamily == "overall-classification" ? overall.PageCount : 0,
            overall.ReportFamily == "overall-classification" ? overall.CompetitorCount : 0,
            overall.ReportFamily == "overall-classification" ? overall.DisqualifiedCompetitorCount : 0,
            verification.ReportFamily == "score-verification" ? verification.PageCount : 0,
            verification.ReportFamily == "score-verification" ? verification.CompetitorCount : 0,
            verification.ReportFamily == "score-verification" ? verification.StageResultCount : 0,
            matched,
            unmatchedClassification,
            unmatchedVerification,
            ambiguousGroups,
            mappedDisqualifiedCount,
            unmatchedDisqualifiedCount,
            CanPublish: false,
            diagnostics.Distinct(StringComparer.Ordinal).ToArray(),
            classificationEntries,
            verificationEntries,
            parsedStageResultCount,
            hitFactorTimeMismatchCount,
            disqualifiedEntries);

    private sealed record ClassificationEntry(
        string DisplayName,
        string NormalizedName,
        string Division,
        int Rank,
        double Percent,
        double Points,
        int PageNumber);

    private sealed record VerificationEntryBuilder(
        string CompetitorNumber,
        string DisplayName,
        string NormalizedName,
        string Division,
        string? PowerFactor,
        int PageNumber)
    {
        public List<FreezerPdfStagePreview> Stages { get; } = [];
    }

    private static string NormalizeDivision(string value) => FreezerIdentityNormalizer.NormalizeName(value);

    private static string PairKey(string normalizedName, string division) =>
        $"{division}|{normalizedName}";

}
