using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace FreezerAnalysis.Api.Services;

public sealed record PdfTextExtractionResult(
    string LayoutText,
    int PageCount,
    string ReportFamily,
    int CompetitorCount,
    int StageResultCount,
    int DisqualifiedCompetitorCount,
    IReadOnlyList<PdfDisqualifiedCompetitor> DisqualifiedCompetitors,
    IReadOnlyList<string> Diagnostics);

public sealed record PdfDisqualifiedCompetitor(string Division, string DisplayName, int PageNumber);

public sealed class PdfTextExtractor(IOptions<FreezerAnalysisHostOptions> options)
{
    private const int MaximumExtractedCharacters = 40 * 1024 * 1024;
    private static readonly TimeSpan ExtractionTimeout = TimeSpan.FromSeconds(30);

    public async Task<string> ExtractLayoutTextAsync(
        ReadOnlyMemory<byte> pdfBytes,
        CancellationToken cancellationToken)
    {
        if (pdfBytes.Length == 0 || pdfBytes.Length > options.Value.MaximumUploadBytes)
            throw new InvalidDataException("PDF size is empty or exceeds the configured upload limit.");
        if (pdfBytes.Length < 5 || !pdfBytes.Span[..5].SequenceEqual("%PDF-"u8))
            throw new InvalidDataException("The selected file is not a PDF document.");

        var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"freezer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        var inputPath = Path.Combine(temporaryDirectory, "input.pdf");
        try
        {
            await File.WriteAllBytesAsync(inputPath, pdfBytes.ToArray(), cancellationToken);
            var startInfo = new ProcessStartInfo
            {
                FileName = options.Value.PdfTextExtractorPath,
                WorkingDirectory = temporaryDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = new UTF8Encoding(false, false),
                StandardErrorEncoding = new UTF8Encoding(false, false)
            };
            startInfo.ArgumentList.Add("-layout");
            startInfo.ArgumentList.Add("-enc");
            startInfo.ArgumentList.Add("UTF-8");
            startInfo.ArgumentList.Add("input.pdf");
            startInfo.ArgumentList.Add("-");

            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
                throw new InvalidOperationException("The local PDF text extractor did not start.");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ExtractionTimeout);
            var outputTask = ReadLimitedAsync(process.StandardOutput, MaximumExtractedCharacters, timeout.Token);
            var errorTask = ReadLimitedAsync(process.StandardError, 4096, timeout.Token);
            try
            {
                var output = await outputTask;
                var error = await errorTask;
                await process.WaitForExitAsync(timeout.Token);
                if (process.ExitCode != 0)
                    throw new InvalidDataException(string.IsNullOrWhiteSpace(error)
                        ? "The PDF text extractor rejected the document."
                        : "The PDF text extractor rejected the document with a diagnostic.");
                return output;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                KillIfRunning(process);
                throw new TimeoutException("PDF text extraction exceeded the 30-second limit.");
            }
            catch
            {
                KillIfRunning(process);
                throw;
            }
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new InvalidOperationException(
                "The configured local PDF text extractor could not be started. Install Poppler and verify FreezerAnalysis:PdfTextExtractorPath.",
                exception);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private static async Task<string> ReadLimitedAsync(
        StreamReader reader,
        int maximumCharacters,
        CancellationToken cancellationToken)
    {
        var buffer = new char[8192];
        var result = new StringBuilder(Math.Min(maximumCharacters, buffer.Length));
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0)
                return result.ToString();
            if (result.Length + read > maximumCharacters)
                throw new InvalidDataException("PDF extracted text exceeds the configured processing limit.");
            result.Append(buffer, 0, read);
        }
    }

    private static void KillIfRunning(Process process)
    {
        if (!process.HasExited)
            process.Kill(entireProcessTree: true);
    }
}

public sealed class FreezerPdfReportDetector
{
    private static readonly Regex OverallResultRow = new(
        @"^\s*\d+\s+\d{1,3}[,.]\d{2}\s+\d{1,4}[,.]\d{4}\s+",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex VerificationCompetitorHeader = new(
        @"\bDIV\s*:",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex VerificationStageRow = new(
        @"^\s*Stage\s+\d+\s+",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex RankedOverallResultRow = new(
        @"^\s*\d+\s+\d{1,3}[,.]\d{2}\s+\d{1,4}[,.]\d{4}\s+",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    public PdfTextExtractionResult Classify(string layoutText)
    {
        var normalized = layoutText.ToUpperInvariant();
        var separators = layoutText.Count(x => x == '\f');
        var hasTrailingSeparator = layoutText.TrimEnd(' ', '\t', '\r', '\n').EndsWith('\f');
        var pages = Math.Max(1, separators + 1 - (hasTrailingSeparator ? 1 : 0));
        var family = normalized.Contains("VERIFY TIRATORI", StringComparison.Ordinal) ||
                     normalized.Contains("SCORE VERIFICATION", StringComparison.Ordinal) ||
                     normalized.Contains("STAGE RESULTS", StringComparison.Ordinal)
            ? "score-verification"
            : normalized.Contains("RESULTS OVERALL", StringComparison.Ordinal) ||
              normalized.Contains("OVERALL", StringComparison.Ordinal)
                ? "overall-classification"
                : normalized.Contains("COMPETITOR LISTING", StringComparison.Ordinal)
                    ? "competitor-roster"
                    : "unknown";

        var competitorCount = family switch
        {
            "score-verification" => VerificationCompetitorHeader.Matches(layoutText).Count,
            "overall-classification" when normalized.Contains("COMPETITOR", StringComparison.Ordinal) =>
                OverallResultRow.Matches(layoutText).Count,
            _ => 0
        };
        var stageResultCount = family == "score-verification"
            ? VerificationStageRow.Matches(layoutText).Count
            : 0;
        var disqualifiedCompetitors = family == "overall-classification"
            ? ParseDisqualifiedCompetitors(layoutText)
            : [];

        var diagnostics = family == "unknown"
            ? new[] { "PDF report family was not recognized from extracted content. No scores were inferred; use the supported CSV import or correct the layout mapping." }
            : new[] { $"PDF report family '{family}' was recognized; structural counts are available, but no verified score mapping is available. No scores were inferred or published." };

        return new(
            layoutText,
            pages,
            family,
            competitorCount,
            stageResultCount,
            disqualifiedCompetitors.Count,
            disqualifiedCompetitors,
            diagnostics);
    }

    private static IReadOnlyList<PdfDisqualifiedCompetitor> ParseDisqualifiedCompetitors(string layoutText)
    {
        var entries = new List<PdfDisqualifiedCompetitor>();
        var pages = layoutText.Split('\f');
        for (var pageIndex = 0; pageIndex < pages.Length; pageIndex++)
        {
            var page = pages[pageIndex];
            if (!page.Contains("Disqualified", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = page.Split('\n');
            var headerIndex = Array.FindIndex(lines, line =>
                line.Contains("Division", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("Name", StringComparison.OrdinalIgnoreCase));
            if (headerIndex < 0)
                continue;

            var header = lines[headerIndex];
            var divisionStart = header.IndexOf("Division", StringComparison.OrdinalIgnoreCase);
            var nameStart = header.IndexOf("Name", divisionStart, StringComparison.OrdinalIgnoreCase);
            if (divisionStart < 0 || nameStart <= divisionStart)
                continue;

            for (var index = headerIndex + 1; index < lines.Length; index++)
            {
                var line = lines[index];
                if (!Regex.IsMatch(line, @"^\s*\d+\s+", RegexOptions.CultureInvariant) ||
                    line.Length <= nameStart)
                    continue;

                var division = line.Substring(
                    divisionStart,
                    Math.Min(nameStart, line.Length) - divisionStart).Trim();
                var name = line[nameStart..].Trim();
                if (division.Length > 0 && name.Length > 0)
                    entries.Add(new PdfDisqualifiedCompetitor(division, name, pageIndex + 1));
            }
        }

        return entries;
    }
}
