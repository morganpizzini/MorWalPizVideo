using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MorWalPizVideo.Models.FreezerAnalysis;

namespace FreezerAnalysis.Api.Services;

public sealed record ParsedFreezerImport(
    string MatchKey,
    string MatchName,
    DateOnly LocalMatchDate,
    IReadOnlyList<FreezerMatchStage> Stages,
    IReadOnlyList<FreezerCompetitorResult> Results,
    IReadOnlyList<string> AvailableBeforeStartCompetitorIds,
    IReadOnlyList<string> ExcludedStageIds,
    FreezerImportCompleteness Completeness,
    IReadOnlyList<string> Diagnostics,
    string SourceHash);

public sealed record ParsedRosterEntry(
    string CompetitorId,
    string DisplayName,
    string Division,
    string? Class,
    string? PowerFactor,
    string? Category,
    string? Squad,
    string? LocalDay);

public sealed record RosterPreview(
    string MatchKey,
    int CompetitorCount,
    int UnresolvedCount,
    IReadOnlyList<ParsedRosterEntry> Entries,
    IReadOnlyList<string> Diagnostics);

public sealed class FreezerCsvParser
{
    public const string Version = "archive-csv-v1";

    private static readonly string[] ScoreHeaders =
    [
        "gara", "data", "comp_id", "nome", "div", "fattore", "stage", "colpi", "hf", "punti", "tempo"
    ];

    private static readonly string[] RosterHeaders =
    [
        "gara", "comp_id", "nome", "div", "squad"
    ];

    public ParsedFreezerImport ParseScores(
        ReadOnlyMemory<byte> bytes,
        string matchName,
        DateOnly requestedMatchDate,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sourceHash = Convert.ToHexString(SHA256.HashData(bytes.Span)).ToLowerInvariant();
        var document = ParseDocument(bytes);
        RequireHeaders(document.Headers, ScoreHeaders);
        var rows = new List<ScoreRow>();
        var diagnostics = new List<string>();
        var matchKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matchDates = new HashSet<DateOnly>();

        foreach (var values in document.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (values.All(string.IsNullOrWhiteSpace))
                continue;
            var row = new CsvRow(document.Headers, values);
            var matchKey = Required(row, "gara");
            matchKeys.Add(matchKey);
            matchDates.Add(ParseDate(Required(row, "data")));
            var rounds = ParseInt(Required(row, "colpi"), "colpi");
            var stageNumber = ParseStageNumber(Required(row, "stage"));
            if (rounds is < 1 or > 100)
                throw new FormatException("The colpi field must be between 1 and 100.");

            var name = Required(row, "nome");
            var competitorId = Required(row, "comp_id");
            var division = Required(row, "div");
            var hitFactor = ParseOptionalDouble(row.Get("hf"));
            var time = ParseOptionalDouble(row.Get("tempo"));
            var classificationPoints = ParseOptionalDouble(row.Get("punti"));
            var status = ParseStatus(row.Get("dq"), row.Get("azzerato"), hitFactor, time);
            var alpha = ParseOptionalInt(row.Get("A"));
            var charlie = ParseOptionalInt(row.Get("C"));
            var delta = ParseOptionalInt(row.Get("D"));
            var miss = ParseOptionalInt(row.Get("M"));
            var noShoot = ParseOptionalInt(row.Get("NS"));
            var procedural = ParseOptionalInt(row.Get("PE"));
            var hitCounts = new[] { alpha, charlie, delta, miss, noShoot, procedural };
            int? netPoints = null;
            if (hitCounts.Any(x => x.HasValue))
            {
                if (alpha.HasValue && charlie.HasValue && delta.HasValue && miss.HasValue)
                {
                    var scoredRounds = alpha.Value + charlie.Value + delta.Value + miss.Value;
                    if (scoredRounds != rounds)
                        diagnostics.Add($"Competitor {competitorId}, stage {stageNumber}: A/C/D/M count ({scoredRounds}) differs from colpi ({rounds}).");
                    var major = string.Equals(row.Get("fattore"), "Major", StringComparison.OrdinalIgnoreCase);
                    var minor = string.Equals(row.Get("fattore"), "Minor", StringComparison.OrdinalIgnoreCase);
                    if (major || minor)
                    {
                        var grossPoints = alpha.Value * 5 + charlie.Value * (major ? 4 : 3) + delta.Value * (major ? 2 : 1);
                        netPoints = Math.Max(0, grossPoints - 10 * (miss.Value + (noShoot ?? 0) + (procedural ?? 0)));
                    }
                    else
                    {
                        diagnostics.Add($"Competitor {competitorId}, stage {stageNumber}: net points were not calculated because fattore is not Major or Minor.");
                    }
                }
                else
                {
                    diagnostics.Add($"Competitor {competitorId}, stage {stageNumber}: net points were not calculated because A/C/D/M counts are incomplete.");
                }
            }

            rows.Add(new ScoreRow(
                matchKey,
                ParseDate(Required(row, "data")),
                competitorId,
                name,
                FreezerIdentityNormalizer.NormalizeName(name),
                division,
                row.Get("cls"),
                row.Get("fattore"),
                row.Get("cat"),
                row.Get("squad"),
                stageNumber,
                rounds,
                hitFactor,
                time,
                netPoints,
                classificationPoints,
                alpha,
                charlie,
                delta,
                miss,
                noShoot,
                procedural,
                status,
                ParseBoolean(row.Get("prima")),
                ParseBoolean(row.Get("escluso")),
                ParseOptionalDouble(row.Get("totale"))));
        }

        if (rows.Count == 0)
            throw new FormatException("The score CSV contains no result rows.");
        if (matchKeys.Count != 1)
            throw new FormatException("One import may contain rows from exactly one match.");
        if (matchDates.Count != 1 || matchDates.Single() != requestedMatchDate)
            throw new FormatException("The match date in the CSV must match the selected local match date.");

        var stageRows = rows.GroupBy(x => x.StageNumber).OrderBy(x => x.Key).ToArray();
        var stages = stageRows.Select(group =>
        {
            var rounds = group.Select(x => x.MinimumRounds).Distinct().ToArray();
            if (rounds.Length != 1)
                throw new FormatException($"Stage {group.Key} has conflicting minimum-round counts.");
            return new FreezerMatchStage
            {
                StageId = group.Key.ToString(CultureInfo.InvariantCulture),
                StageNumber = group.Key,
                Name = $"Stage {group.Key}",
                MinimumRounds = rounds[0],
                MaximumPoints = rounds[0] * 5
            };
        }).ToArray();

        var results = new List<FreezerCompetitorResult>();
        foreach (var competitor in rows.GroupBy(x => x.CompetitorId, StringComparer.Ordinal).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var identityRows = competitor.ToArray();
            if (identityRows.Select(x => x.NormalizedName).Distinct(StringComparer.Ordinal).Count() > 1)
                throw new FormatException($"Competitor id {competitor.Key} has multiple names in the same file.");
            if (identityRows.Select(x => x.Division).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                throw new FormatException($"Competitor id {competitor.Key} has multiple divisions in the same file.");

            var duplicateStage = identityRows.GroupBy(x => x.StageNumber).FirstOrDefault(x => x.Count() > 1);
            if (duplicateStage is not null)
                throw new FormatException($"Competitor id {competitor.Key} has duplicate results for stage {duplicateStage.Key}.");

            var first = identityRows[0];
            var officialTotal = identityRows.Where(x => x.ClassificationPoints.HasValue)
                .Sum(x => x.ClassificationPoints!.Value);
            var allClassificationPoints = identityRows.All(x => x.ClassificationPoints.HasValue);
            var declaredTotal = identityRows.Select(x => x.OfficialTotal)
                .FirstOrDefault(x => x.HasValue);
            if (declaredTotal.HasValue && allClassificationPoints &&
                Math.Abs(officialTotal - declaredTotal.Value) > 0.02)
                throw new FormatException($"Competitor id {competitor.Key} classification total does not reconcile within 0.02 points.");
            if (!declaredTotal.HasValue)
                diagnostics.Add($"Competitor {competitor.Key}: no independent official total was provided; classification sum is provisional.");

            var stageResults = identityRows.OrderBy(x => x.StageNumber).Select(row => new FreezerStageResult
            {
                StageId = row.StageNumber.ToString(CultureInfo.InvariantCulture),
                MinimumRounds = row.MinimumRounds,
                HitFactor = row.HitFactor,
                TimeSeconds = row.TimeSeconds,
                NetPoints = row.NetPoints,
                ClassificationPoints = row.ClassificationPoints,
                Alpha = row.Alpha,
                Charlie = row.Charlie,
                Delta = row.Delta,
                Miss = row.Miss,
                NoShoot = row.NoShoot,
                Procedural = row.Procedural,
                Status = row.Status
            }).ToArray();
            results.Add(new FreezerCompetitorResult
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                CompetitorId = competitor.Key,
                ContentHash = HashCanonicalResult(competitor.Key, first, stageResults),
                DisplayName = first.Name,
                NormalizedName = first.NormalizedName,
                Division = first.Division,
                Class = first.Class,
                PowerFactor = first.PowerFactor,
                Category = first.Category,
                Squad = first.Squad,
                OfficialTotal = declaredTotal,
                IsDisqualified = stageResults.Any(x => x.Status == FreezerResultStatus.Disqualified),
                IsZeroed = stageResults.Any(x => x.Status == FreezerResultStatus.Zeroed),
                Stages = stageResults
            });
        }

        var completeness = results.All(x => x.OfficialTotal.HasValue &&
                                           x.Stages.Count == stages.Length &&
                                           x.Stages.All(stage => stage.ClassificationPoints.HasValue))
            ? FreezerImportCompleteness.Final
            : FreezerImportCompleteness.Provisional;
        if (completeness == FreezerImportCompleteness.Provisional)
            diagnostics.Add("Import is provisional: one or more competitors lack a full official classification total or stage coverage.");

        return new ParsedFreezerImport(
            matchKeys.Single(),
            matchName.Trim(),
            requestedMatchDate,
            stages,
            results,
            rows.Where(x => x.AvailableBeforeStart).Select(x => x.CompetitorId).Distinct(StringComparer.Ordinal).ToArray(),
            rows.Where(x => x.Excluded).Select(x => x.StageNumber.ToString(CultureInfo.InvariantCulture)).Distinct(StringComparer.Ordinal).ToArray(),
            completeness,
            diagnostics.Distinct(StringComparer.Ordinal).ToArray(),
            sourceHash);
    }

    public RosterPreview ParseRoster(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var document = ParseDocument(bytes);
        RequireHeaders(document.Headers, RosterHeaders);
        var entries = new List<ParsedRosterEntry>();
        var matchKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var values in document.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (values.All(string.IsNullOrWhiteSpace))
                continue;
            var row = new CsvRow(document.Headers, values);
            var id = Required(row, "comp_id");
            if (!ids.Add(id))
                throw new FormatException($"The roster contains duplicate competitor id {id}.");
            matchKeys.Add(Required(row, "gara"));
            entries.Add(new ParsedRosterEntry(
                id,
                Required(row, "nome"),
                Required(row, "div"),
                row.Get("cls"),
                row.Get("fattore"),
                row.Get("cat"),
                row.Get("squad"),
                row.Get("giorno")));
        }
        if (matchKeys.Count != 1 || entries.Count == 0)
            throw new FormatException("The roster must contain entries for exactly one match.");
        return new RosterPreview(
            matchKeys.Single(),
            entries.Count,
            entries.Count,
            entries,
            ["Roster identities remain unresolved until the user explicitly confirms a match." ]);
    }

    private static CsvDocument ParseDocument(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length is 0 or > 20 * 1024 * 1024)
            throw new FormatException("CSV size must be between 1 byte and 20 MiB.");
        var encoding = new UTF8Encoding(false, true);
        var text = encoding.GetString(bytes.Span).TrimStart('\uFEFF');
        var delimiter = GuessDelimiter(text);
        var records = ParseRecords(text, delimiter);
        if (records.Count < 2)
            throw new FormatException("CSV must contain a header row and at least one data row.");
        var headers = records[0].Select(NormalizeHeader).ToArray();
        if (headers.Length != headers.Distinct(StringComparer.Ordinal).Count())
            throw new FormatException("CSV contains duplicate column names.");
        var rows = records.Skip(1).ToArray();
        if (rows.Any(row => row.Count != headers.Length))
            throw new FormatException("CSV contains a row with a different number of columns than its header.");
        return new CsvDocument(headers, rows.ToList());
    }

    private static List<List<string>> ParseRecords(string text, char delimiter)
    {
        var records = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            if (quoted)
            {
                if (current == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (current == '"')
                    quoted = false;
                else
                    field.Append(current);
                continue;
            }

            if (current == '"' && field.Length == 0)
            {
                quoted = true;
                continue;
            }
            if (current == delimiter)
            {
                row.Add(field.ToString().Trim());
                field.Clear();
                continue;
            }
            if (current is '\r' or '\n')
            {
                row.Add(field.ToString().Trim());
                field.Clear();
                if (row.Any(x => x.Length != 0))
                    records.Add(row);
                row = [];
                if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                    index++;
                continue;
            }
            field.Append(current);
        }

        if (quoted)
            throw new FormatException("CSV contains an unterminated quoted field.");
        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString().Trim());
            if (row.Any(x => x.Length != 0))
                records.Add(row);
        }
        return records;
    }

    private static char GuessDelimiter(string text)
    {
        var firstLine = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            ?? throw new FormatException("CSV is empty.");
        var commaCount = firstLine.Count(x => x == ',');
        var semicolonCount = firstLine.Count(x => x == ';');
        return semicolonCount > commaCount ? ';' : ',';
    }

    private static void RequireHeaders(IReadOnlyCollection<string> headers, IEnumerable<string> required)
    {
        var missing = required.Where(x => !headers.Contains(NormalizeHeader(x), StringComparer.Ordinal)).ToArray();
        if (missing.Length > 0)
            throw new FormatException($"Unsupported CSV schema. Missing required columns: {string.Join(", ", missing)}.");
    }

    private static string Required(CsvRow row, string header) =>
        string.IsNullOrWhiteSpace(row.Get(header))
            ? throw new FormatException($"Required CSV field '{header}' is empty.")
            : row.Get(header)!.Trim();

    private static int ParseInt(string value, string column) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new FormatException($"CSV field '{column}' must be an integer.");

    private static int ParseStageNumber(string value)
    {
        var match = Regex.Match(value, @"\d+");
        return match.Success && int.TryParse(match.Value, CultureInfo.InvariantCulture, out var stage) && stage > 0
            ? stage
            : throw new FormatException("The stage field must contain a positive stage number.");
    }

    private static DateOnly ParseDate(string value)
    {
        var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "yyyy/MM/dd" };
        return DateOnly.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new FormatException("Match date must use yyyy-MM-dd or dd/MM/yyyy.");
    }

    private static double? ParseOptionalDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim().Replace(',', '.');
        if (!double.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result) ||
            !double.IsFinite(result))
            throw new FormatException("A numeric CSV field is invalid or non-finite.");
        return result;
    }

    private static int? ParseOptionalInt(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) && result >= 0
                ? result
                : throw new FormatException("A hit count must be a non-negative integer.");

    private static bool ParseBoolean(string? value) => value?.Trim().ToLowerInvariant() is
        "true" or "1" or "yes" or "y" or "si" or "sì";

    private static FreezerResultStatus ParseStatus(string? dq, string? zeroed, double? hf, double? time)
    {
        if (ParseBoolean(dq) || string.Equals(dq?.Trim(), "dq", StringComparison.OrdinalIgnoreCase))
            return FreezerResultStatus.Disqualified;
        if (ParseBoolean(zeroed))
            return FreezerResultStatus.Zeroed;
        if (!hf.HasValue || !time.HasValue || time <= 0 || hf < 0)
            return FreezerResultStatus.Missing;
        return FreezerResultStatus.Scored;
    }

    private static string NormalizeHeader(string value) =>
        new string(value.Trim().TrimStart('\uFEFF').ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());

    private static string HashCanonicalResult(string competitorId, ScoreRow first, IReadOnlyList<FreezerStageResult> stages)
    {
        var canonical = string.Join('|',
            competitorId,
            first.NormalizedName,
            first.Division.ToUpperInvariant(),
            first.Class?.ToUpperInvariant() ?? string.Empty,
            first.PowerFactor?.ToUpperInvariant() ?? string.Empty,
            first.Category?.ToUpperInvariant() ?? string.Empty,
            first.Squad ?? string.Empty,
            first.OfficialTotal?.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty,
            first.AvailableBeforeStart,
            first.Excluded,
            string.Join(';', stages.OrderBy(x => x.StageId, StringComparer.Ordinal).Select(x =>
                $"{x.StageId}:{x.MinimumRounds}:{x.HitFactor?.ToString("R", CultureInfo.InvariantCulture)}:" +
                $"{x.TimeSeconds?.ToString("R", CultureInfo.InvariantCulture)}:{x.NetPoints}:" +
                $"{x.ClassificationPoints?.ToString("R", CultureInfo.InvariantCulture)}:{x.Alpha}:{x.Charlie}:" +
                $"{x.Delta}:{x.Miss}:{x.NoShoot}:{x.Procedural}:{(int)x.Status}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private sealed record CsvDocument(string[] Headers, List<List<string>> Rows);

    private sealed class CsvRow(string[] headers, IReadOnlyList<string> values)
    {
        private readonly Dictionary<string, string> fields = headers
            .Select((header, index) => new KeyValuePair<string, string>(header, values[index]))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);

        public string? Get(string name) =>
            fields.TryGetValue(NormalizeHeader(name), out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : null;
    }

    private sealed record ScoreRow(
        string MatchKey,
        DateOnly MatchDate,
        string CompetitorId,
        string Name,
        string NormalizedName,
        string Division,
        string? Class,
        string? PowerFactor,
        string? Category,
        string? Squad,
        int StageNumber,
        int MinimumRounds,
        double? HitFactor,
        double? TimeSeconds,
        int? NetPoints,
        double? ClassificationPoints,
        int? Alpha,
        int? Charlie,
        int? Delta,
        int? Miss,
        int? NoShoot,
        int? Procedural,
        FreezerResultStatus Status,
        bool AvailableBeforeStart,
        bool Excluded,
        double? OfficialTotal)
    {
    }
}
