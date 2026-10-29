using FreezerAnalysis.Api.Services;

namespace FreezerAnalysis.Api.Tests;

public sealed class FreezerPdfPairParserTests
{
    private readonly FreezerPdfPairParser parser = new(new FreezerPdfReportDetector());

    [Fact]
    public void Pair_preview_matches_unique_normalized_name_and_division_without_enabling_publication()
    {
        var classification = OverallReport(
            ("1", "100,00", "400,0000", "Dóe, Jane"));
        var verification = VerificationReport(
            "123 Doe, Jane DIV: O CLASS: A FACTOR: Major CATEGORY: Senior",
            "Stage 1 4,25 100 20 1 0 0 0 0 0 23,53");

        var preview = parser.Preview(classification, verification);

        Assert.Equal(1, preview.ClassificationCompetitorCount);
        Assert.Equal(1, preview.VerificationCompetitorCount);
        Assert.Equal(1, preview.VerificationStageResultCount);
        Assert.Equal(1, preview.MatchedCompetitorCount);
        Assert.Equal(0, preview.AmbiguousNameDivisionGroupCount);
        Assert.False(preview.CanPublish);
        Assert.Contains(preview.Diagnostics, x => x.Contains("publication remain disabled", StringComparison.Ordinal));
        var classificationEntry = Assert.Single(preview.ClassificationEntries);
        Assert.Equal("OPEN", classificationEntry.Division);
        Assert.Equal(400, classificationEntry.Points);
        var verificationEntry = Assert.Single(preview.VerificationEntries);
        Assert.Equal("MAJOR", verificationEntry.PowerFactor);
        var stage = Assert.Single(verificationEntry.Stages);
        Assert.Equal(100, stage.ReportedPoints);
        Assert.Equal(20, stage.Alpha);
        Assert.Equal(1, stage.Charlie);
        Assert.False(stage.HitFactorMatchesReportedPointsAndTime);
        Assert.Equal(1, preview.HitFactorTimeMismatchCount);
    }

    [Fact]
    public void Pair_preview_joins_disqualification_appendix_only_when_name_and_division_are_unique()
    {
        var classification = OverallReport(
            ("1", "100,00", "400,0000", "Doe, Jane")) +
            "\nDisqualified Competitors\n#   Division                 Name\n1   Open                     Doe, Jane";
        var verification = VerificationReport(
            "123 Doe, Jane DIV: O CLASS: A FACTOR: Major CATEGORY: Senior",
            "Stage 1 4,25 100 20 1 0 0 0 0 0 23,53");

        var preview = parser.Preview(classification, verification);

        Assert.Equal(1, preview.DisqualifiedCompetitorCount);
        Assert.Equal(1, preview.MappedDisqualifiedCompetitorCount);
        Assert.Equal(0, preview.UnmatchedDisqualifiedCompetitorCount);
        var disqualifiedEntry = Assert.Single(preview.DisqualifiedEntries!);
        Assert.Equal("Doe, Jane", disqualifiedEntry.DisplayName);
        Assert.Equal("matched", disqualifiedEntry.PairingStatus);
        Assert.True(Assert.Single(preview.ClassificationEntries).IsDisqualified);
        Assert.True(Assert.Single(preview.VerificationEntries).IsDisqualified);
        Assert.Contains(preview.Diagnostics, diagnostic =>
            diagnostic.Contains("uniquely joined", StringComparison.Ordinal));
        Assert.False(preview.CanPublish);
    }

    [Fact]
    public void Pair_preview_keeps_disqualification_status_unverified_when_names_collide()
    {
        var classification = OverallReport(
            ("1", "100,00", "400,0000", "Doe, Jane"),
            ("2", "95,00", "380,0000", "Doe Jane")) +
            "\nDisqualified Competitors\n#   Division                 Name\n1   Open                     Doe, Jane";
        var verification = VerificationReport(
            "123 Doe, Jane DIV: O CLASS: A FACTOR: Major CATEGORY: Senior",
            "Stage 1 4,25 100 20 1 0 0 0 0 0 23,53");

        var preview = parser.Preview(classification, verification);

        Assert.Equal(0, preview.MappedDisqualifiedCompetitorCount);
        Assert.Equal(1, preview.UnmatchedDisqualifiedCompetitorCount);
        Assert.Equal("ambiguous", Assert.Single(preview.DisqualifiedEntries!).PairingStatus);
        Assert.All(preview.ClassificationEntries, entry => Assert.Null(entry.IsDisqualified));
        Assert.Null(Assert.Single(preview.VerificationEntries).IsDisqualified);
        Assert.Contains(preview.Diagnostics, diagnostic =>
            diagnostic.Contains("could not be joined uniquely", StringComparison.Ordinal));
    }

    [Fact]
    public void Pair_preview_keeps_disqualification_appendix_visible_when_counterpart_report_is_missing()
    {
        var classification = OverallReport(("1", "100,00", "400,0000", "Doe, Jane")) +
            "\nDisqualified Competitors\n#   Division                 Name\n1   Open                     Doe, Jane";

        var preview = parser.Preview(classification, "Unsupported report");

        Assert.False(preview.CanPublish);
        Assert.Equal(0, preview.MappedDisqualifiedCompetitorCount);
        Assert.Equal(1, preview.UnmatchedDisqualifiedCompetitorCount);
        var disqualifiedEntry = Assert.Single(preview.DisqualifiedEntries!);
        Assert.Equal("Doe, Jane", disqualifiedEntry.DisplayName);
        Assert.Equal("unmatched", disqualifiedEntry.PairingStatus);
    }

    [Fact]
    public void Pair_preview_preserves_optional_overtime_without_using_it_for_scoring()
    {
        var preview = parser.Preview(
            OverallReport(("1", "100,00", "400,0000", "Doe, Jane")),
            VerificationReport(
                "123 Doe, Jane DIV: O CLASS: A FACTOR: Major CATEGORY: Senior",
                "Stage 1 4,25 100 20 1 0 0 0 0 0 2 23,53"));

        var stage = Assert.Single(Assert.Single(preview.VerificationEntries).Stages);

        Assert.Equal("2", stage.OvertimeValue);
        Assert.Equal(23.53, stage.TimeSeconds);
        Assert.False(preview.CanPublish);
    }

    [Fact]
    public void Pair_preview_does_not_merge_normalized_name_collisions()
    {
        var classification = OverallReport(
            ("1", "100,00", "400,0000", "Doe, Jane"),
            ("2", "95,00", "380,0000", "Doe Jane"));
        var verification = VerificationReport(
            "123 Doe, Jane DIV: O CLASS: A FACTOR: Major CATEGORY: Senior",
            "Stage 1 4,25 100 20 1 0 0 0 0 0 23,53");

        var preview = parser.Preview(classification, verification);

        Assert.Equal(0, preview.MatchedCompetitorCount);
        Assert.Equal(1, preview.AmbiguousNameDivisionGroupCount);
        Assert.Contains(preview.Diagnostics, x => x.Contains("collisions require explicit review", StringComparison.Ordinal));
    }

    private static string OverallReport(params (string Rank, string Percent, string Points, string Name)[] rows)
    {
        const string header = "          %      Points     Competitor                  Cat   Reg   Cls   Tag   Ics";
        var resultRows = rows.Select(row =>
        {
            var prefix = $"{row.Rank,3} {row.Percent,8} {row.Points,10}";
            return prefix.PadRight(31) + row.Name.PadRight(29) + "SEN     IT    A";
        });
        return "Open -- Overall Match Results\n" + header + "\n" + string.Join('\n', resultRows);
    }

    private static string VerificationReport(string participantHeader, string stageRow) =>
        $"Score Verification Report\n{participantHeader}\nSTG FACTOR PTS A C D Ded. MI NS PE OT Time\n{stageRow}";
}
