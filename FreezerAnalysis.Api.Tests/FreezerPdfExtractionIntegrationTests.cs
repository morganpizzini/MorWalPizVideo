using System.Text;
using FreezerAnalysis.Api.Services;
using Microsoft.Extensions.Options;

namespace FreezerAnalysis.Api.Tests;

public sealed class FreezerPdfExtractionIntegrationTests
{
    [LocalPopplerFact]
    public async Task Synthetic_report_pdfs_are_extracted_and_parsed_as_a_pair()
    {
        var extractor = new PdfTextExtractor(Options.Create(new FreezerAnalysisHostOptions
        {
            PdfTextExtractorPath = Environment.GetEnvironmentVariable("FREEZER_ANALYSIS_PDFTEXTEXTRACTOR_PATH")!,
            MaximumUploadBytes = 20 * 1024 * 1024
        }));
        var classificationPdf = CreateTextPdf(
        [
            "Open -- Overall Match Results",
            "          %      Points     Competitor                  Cat   Reg   Cls   Tag   Ics",
            ClassificationRow("1", "100,00", "400,0000", "Doe, Jane")
        ]);
        var verificationPdf = CreateTextPdf(
        [
            "Score Verification Report",
            "123 Doe, Jane DIV: O CLASS: A FACTOR: Major CATEGORY: Senior",
            "STG FACTOR PTS A C D Ded. MI NS PE OT Time",
            "Stage 1 4,25 100 20 1 0 0 0 0 0 23,5294"
        ]);

        var classificationText = await extractor.ExtractLayoutTextAsync(classificationPdf, CancellationToken.None);
        var verificationText = await extractor.ExtractLayoutTextAsync(verificationPdf, CancellationToken.None);
        var preview = new FreezerPdfPairParser(new FreezerPdfReportDetector())
            .Preview(classificationText, verificationText);

        Assert.Equal(1, preview.ClassificationPageCount);
        Assert.Equal(1, preview.VerificationPageCount);
        Assert.Equal(1, preview.MatchedCompetitorCount);
        Assert.Equal("matched", Assert.Single(preview.ClassificationEntries).PairingStatus);
        Assert.Equal("matched", Assert.Single(preview.VerificationEntries).PairingStatus);
        Assert.Equal(1, preview.ParsedVerificationStageResultCount);
        Assert.True(Assert.Single(Assert.Single(preview.VerificationEntries).Stages)
            .HitFactorMatchesReportedPointsAndTime);
        Assert.False(preview.CanPublish);
    }

    private static byte[] CreateTextPdf(IReadOnlyList<string> lines)
    {
        var content = new StringBuilder("BT\n/F1 8 Tf\n24 760 Td\n");
        foreach (var line in lines)
            content.Append('(').Append(EscapePdfString(line)).Append(") Tj\n0 -12 Td\n");
        content.Append("ET\n");
        var stream = content.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream"
        };
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.Append(index + 1).Append(" 0 obj\n")
                .Append(objects[index]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.Append("xref\n0 ").Append(objects.Length + 1).Append("\n")
            .Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            pdf.Append(offset.ToString("D10", System.Globalization.CultureInfo.InvariantCulture))
                .Append(" 00000 n \n");
        pdf.Append("trailer\n<< /Size ").Append(objects.Length + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n").Append(xrefOffset)
            .Append("\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string ClassificationRow(string rank, string percent, string points, string name)
    {
        var prefix = $"{rank,3} {percent,8} {points,10}";
        return prefix.PadRight(31) + name.PadRight(29) + "SEN     IT    A";
    }

    private static string EscapePdfString(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
}
