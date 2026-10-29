using FreezerAnalysis.Api.Services;

namespace FreezerAnalysis.Api.Tests;

public sealed class FreezerPdfReportDetectorTests
{
    [Fact]
    public void Verification_report_counts_competitor_sections_and_stage_rows_across_pages()
    {
        const string layout = """
            Score Verification Report
            DIV: Production CLASS: A FACTOR: Major CATEGORY: Senior
            STG FACTOR PTS A C D Ded. MI NS PE OT Time
            Stage 1 4,25 100 20 1 0 0 0 0 0 0 23,53
            Stage 2 5,00 120 24 0 0 0 0 0 0 0 24,00
            DIV: Production CLASS: B FACTOR: Minor CATEGORY: Senior
            STG FACTOR PTS A C D Ded. MI NS PE OT Time
            Stage 1 3,50 80 16 1 0 0 0 0 0 0 22,86
            """;

        var result = new FreezerPdfReportDetector().Classify(layout);

        Assert.Equal("score-verification", result.ReportFamily);
        Assert.Equal(2, result.CompetitorCount);
        Assert.Equal(3, result.StageResultCount);
        Assert.Contains(result.Diagnostics, x => x.Contains("No scores were inferred or published.", StringComparison.Ordinal));
    }

    [Fact]
    public void Overall_report_counts_rows_only_when_the_competitor_table_is_present()
    {
        const string layout = """
            Overall Match Results
            % Points Competitor Cat Reg Cls Tag Ics
            1 100,00 400,0000 Competitor A
            2 95,00 380,0000 Competitor B
            """;
        const string titleOnly = "Overall Match Results";

        var detector = new FreezerPdfReportDetector();

        var result = detector.Classify(layout);
        var incomplete = detector.Classify(titleOnly);

        Assert.Equal("overall-classification", result.ReportFamily);
        Assert.Equal(2, result.CompetitorCount);
        Assert.Equal(0, incomplete.CompetitorCount);
    }

    [Fact]
    public void Overall_report_counts_disqualification_appendix_rows_separately()
    {
        const string layout = """
            Overall Match Results
            Disqualified Competitors
            #   Division                 Name
            1   Open                     Competitor A
            2   Production               Competitor B
            """;

        var result = new FreezerPdfReportDetector().Classify(layout);

        Assert.Equal("overall-classification", result.ReportFamily);
        Assert.Equal(2, result.DisqualifiedCompetitorCount);
        Assert.Equal(
            [
                new PdfDisqualifiedCompetitor("Open", "Competitor A", 1),
                new PdfDisqualifiedCompetitor("Production", "Competitor B", 1)
            ],
            result.DisqualifiedCompetitors);
        Assert.Equal(0, result.CompetitorCount);
    }
}
