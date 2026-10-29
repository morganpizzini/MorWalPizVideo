using FreezerAnalysis.Api.Services;

namespace FreezerAnalysis.Api.Tests;

public sealed class PdfTextExtractorTests
{
    [Theory]
    [InlineData("Overall\f", 1)]
    [InlineData("Overall\fResults", 2)]
    [InlineData("Overall\fResults\f\r\n", 2)]
    [InlineData("", 1)]
    public void Classifier_counts_pdf_pages_without_counting_a_trailing_page_break(
        string layoutText,
        int expectedPageCount)
    {
        var result = new FreezerPdfReportDetector().Classify(layoutText);

        Assert.Equal(expectedPageCount, result.PageCount);
    }

    [Theory]
    [InlineData("Score Verification\fStage 1", "score-verification")]
    [InlineData("Overall Match Results\fCompetitor", "overall-classification")]
    public void Classifier_uses_extracted_report_content(
        string layoutText,
        string expectedFamily)
    {
        var result = new FreezerPdfReportDetector().Classify(layoutText);

        Assert.Equal(expectedFamily, result.ReportFamily);
        Assert.Contains("no verified score mapping", result.Diagnostics[0], StringComparison.OrdinalIgnoreCase);
    }
}
