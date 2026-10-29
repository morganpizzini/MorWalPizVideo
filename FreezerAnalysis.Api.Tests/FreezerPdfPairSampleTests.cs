using FreezerAnalysis.Api.Services;
using Microsoft.Extensions.Options;

namespace FreezerAnalysis.Api.Tests;

public sealed class FreezerPdfPairSampleTests
{
    [LocalPopplerFact]
    public async Task Supplied_pair_has_complete_score_preview_coverage_without_publication()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sampleDirectory = Path.Combine(
            repositoryRoot,
            "docs",
            "feature-handoffs",
            "freezer-analysis-samples");
        var options = Options.Create(new FreezerAnalysisHostOptions
        {
            PdfTextExtractorPath = Environment.GetEnvironmentVariable("FREEZER_ANALYSIS_PDFTEXTEXTRACTOR_PATH")!,
            MaximumUploadBytes = 20 * 1024 * 1024
        });
        var extractor = new PdfTextExtractor(options);
        var overallPath = Path.Combine(sampleDirectory, "classifica.pdf");
        var verificationPath = Path.Combine(sampleDirectory, "verify.pdf");
        var extractionTasks = new[]
        {
            extractor.ExtractLayoutTextAsync(await File.ReadAllBytesAsync(overallPath), CancellationToken.None),
            extractor.ExtractLayoutTextAsync(await File.ReadAllBytesAsync(verificationPath), CancellationToken.None)
        };
        var extractedDocuments = await Task.WhenAll(extractionTasks);
        var preview = new FreezerPdfPairParser(new FreezerPdfReportDetector())
            .Preview(extractedDocuments[0], extractedDocuments[1]);

        Assert.Equal(9, preview.ClassificationPageCount);
        Assert.Equal(142, preview.ClassificationEntries.Count);
        Assert.Equal(48, preview.VerificationPageCount);
        Assert.Equal(142, preview.VerificationEntries.Count);
        Assert.Equal(1_704, preview.ParsedVerificationStageResultCount);
        Assert.Equal(7, preview.DisqualifiedCompetitorCount);
        Assert.Equal(0, preview.MappedDisqualifiedCompetitorCount);
        Assert.Equal(7, preview.UnmatchedDisqualifiedCompetitorCount);
        Assert.Equal(7, preview.DisqualifiedEntries?.Count);
        Assert.All(preview.DisqualifiedEntries!, entry => Assert.Equal("unmatched", entry.PairingStatus));
        Assert.All(preview.ClassificationEntries, entry => Assert.Null(entry.IsDisqualified));
        Assert.All(preview.VerificationEntries, entry => Assert.Null(entry.IsDisqualified));
        Assert.Contains(preview.Diagnostics, diagnostic =>
            diagnostic.Contains("could not be joined uniquely", StringComparison.Ordinal));
        Assert.Equal(11, preview.HitFactorTimeMismatchCount);
        Assert.False(preview.CanPublish);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(
                    directory.FullName,
                    "docs",
                    "feature-handoffs",
                    "freezer-analysis-samples")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("The repository PDF sample directory was not found.");
    }
}

public sealed class LocalPopplerFactAttribute : FactAttribute
{
    public LocalPopplerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("FREEZER_ANALYSIS_PDFTEXTEXTRACTOR_PATH")))
        {
            Skip = "Set FREEZER_ANALYSIS_PDFTEXTEXTRACTOR_PATH to run the local sample-PDF check.";
        }
    }
}
