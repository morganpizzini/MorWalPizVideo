using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MorWalPiz.Contracts.Contracts.FreezerAnalysis;
using FreezerAnalysis.Api.Services;

namespace FreezerAnalysis.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class FreezerAnalysisController(
    FreezerAnalysisCoordinator coordinator,
    FreezerCsvParser csvParser,
    PdfTextExtractor pdfTextExtractor,
    FreezerPdfReportDetector pdfReportDetector,
    FreezerPdfPairParser pdfPairParser,
    IOptions<FreezerAnalysisHostOptions> hostOptions) : ControllerBase
{
    [HttpGet("supported-formats")]
    [AllowAnonymous]
    public IActionResult GetSupportedFormats() => Ok(new
    {
        scoreImports = new[]
        {
            new { format = "archive-csv", extension = ".csv", status = "supported", preview = true, publication = true }
        },
        rosterImports = new[]
        {
            new { format = "archive-roster-csv", extension = ".csv", status = "supported", preview = true, publication = false }
        },
        pdf = new
        {
            status = "paired-score-preview-no-publication",
            extraction = "local-poppler-layout",
            pairPreview = true,
            structuralCounts = true,
            numericScorePreview = true,
            scorePublication = false
        }
    });

    [HttpGet("matches")]
    public async Task<ActionResult<IReadOnlyList<FreezerMatchResponse>>> GetMatches(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await coordinator.ListMatchesAsync(AccountId(), limit, cancellationToken));

    [HttpGet("matches/{matchId}")]
    public async Task<ActionResult<FreezerMatchResponse>> GetMatch(
        string matchId,
        CancellationToken cancellationToken)
    {
        var match = await coordinator.GetMatchSummaryAsync(AccountId(), matchId, cancellationToken);
        return match is null ? NotFound() : Ok(match);
    }

    [HttpGet("profile")]
    public async Task<ActionResult<FreezerProfileResponse>> GetProfile(CancellationToken cancellationToken) =>
        Ok(await coordinator.GetProfileAsync(AccountId(), cancellationToken));

    [HttpPut("profile")]
    public async Task<ActionResult<FreezerProfileResponse>> SaveProfile(
        FreezerProfileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await coordinator.SaveProfileAsync(AccountId(), request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception.Message);
        }
    }

    [HttpPost("identities")]
    public async Task<ActionResult<FreezerIdentityResponse>> CreateIdentity(
        FreezerIdentityCreateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var identity = await coordinator.CreateIdentityAsync(AccountId(), request, cancellationToken);
            return CreatedAtAction(nameof(GetIdentity), new { identityId = identity.Id }, identity);
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception.Message);
        }
    }

    [HttpGet("identities/{identityId}")]
    public async Task<ActionResult<FreezerIdentityResponse>> GetIdentity(
        string identityId,
        CancellationToken cancellationToken)
    {
        var identity = await coordinator.GetIdentityAsync(AccountId(), identityId, cancellationToken);
        return identity is null ? NotFound() : Ok(identity);
    }

    [HttpGet("identities")]
    public async Task<ActionResult<IReadOnlyList<FreezerIdentityResponse>>> GetIdentities(
        CancellationToken cancellationToken) =>
        Ok(await coordinator.ListIdentitiesAsync(AccountId(), cancellationToken));

    [HttpGet("matches/{matchId}/identities/review")]
    public async Task<ActionResult<IReadOnlyList<FreezerIdentityReviewEntryResponse>>> GetIdentityReview(
        string matchId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await coordinator.GetIdentityReviewAsync(AccountId(), matchId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("matches/{matchId}/identities/{resultId}")]
    public async Task<ActionResult<FreezerIdentityResolutionResponse>> ResolveIdentity(
        string matchId,
        string resultId,
        FreezerIdentityResolutionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var resolution = await coordinator.ResolveIdentityAsync(
                AccountId(),
                matchId,
                resultId,
                request,
                cancellationToken);
            return resolution is null
                ? Conflict(new ProblemDetails { Title = "The identity resolution changed; reload the review." })
                : Ok(resolution);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception.Message);
        }
    }

    [HttpDelete("matches/{matchId}/identities/{resultId}")]
    public async Task<IActionResult> RemoveIdentityResolution(
        string matchId,
        string resultId,
        [FromQuery] long expectedRevision,
        CancellationToken cancellationToken)
    {
        if (expectedRevision < 0)
            return ValidationProblem("The expected identity-resolution revision cannot be negative.");
        try
        {
            var removed = await coordinator.RemoveIdentityResolutionAsync(
                AccountId(),
                matchId,
                resultId,
                expectedRevision,
                cancellationToken);
            return removed
                ? NoContent()
                : Conflict(new ProblemDetails { Title = "The identity resolution changed; reload the review." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("imports/scores/preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<FreezerImportPreviewResponse>> PreviewScores(
        [FromForm] ScoreImportForm form,
        CancellationToken cancellationToken)
    {
        var source = await ReadUploadAsync(form.File, cancellationToken);
        return Ok(await coordinator.PreviewScoresAsync(source, form.MatchName, form.MatchDate, cancellationToken));
    }

    [HttpPost("imports/scores/publish")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<FreezerImportPreviewResponse>> PublishScores(
        [FromForm] ScoreImportForm form,
        [FromForm] int expectedRevision,
        CancellationToken cancellationToken)
    {
        if (expectedRevision < 0)
            return ValidationProblem("The expected match revision cannot be negative.");
        var source = await ReadUploadAsync(form.File, cancellationToken);
        var result = await coordinator.PublishScoresAsync(
            AccountId(),
            source,
            form.MatchName,
            form.MatchDate,
            expectedRevision,
            cancellationToken);
        return result.CanPublish ? Ok(result) : Conflict(result);
    }

    [HttpPost("imports/roster/preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> PreviewRoster(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var source = await ReadUploadAsync(file, cancellationToken);
        return Ok(csvParser.ParseRoster(source, cancellationToken));
    }

    [HttpPost("imports/pdf/preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> PreviewPdf(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var source = await ReadUploadAsync(file, cancellationToken);
        var text = await pdfTextExtractor.ExtractLayoutTextAsync(source, cancellationToken);
        var result = pdfReportDetector.Classify(text);
        return Ok(new
        {
            result.ReportFamily,
            result.PageCount,
            result.CompetitorCount,
            result.StageResultCount,
            result.Diagnostics,
            CanPublish = false
        });
    }

    [HttpPost("imports/pdfs/preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(40 * 1024 * 1024)]
    public async Task<ActionResult<FreezerPdfPairPreviewResponse>> PreviewPdfPair(
        [FromForm] PdfPairImportForm form,
        CancellationToken cancellationToken)
    {
        var classificationSource = await ReadUploadAsync(form.ClassificationFile, cancellationToken);
        var verificationSource = await ReadUploadAsync(form.VerificationFile, cancellationToken);
        var classificationText = await pdfTextExtractor.ExtractLayoutTextAsync(classificationSource, cancellationToken);
        var verificationText = await pdfTextExtractor.ExtractLayoutTextAsync(verificationSource, cancellationToken);
        return Ok(pdfPairParser.Preview(classificationText, verificationText));
    }

    [HttpGet("matches/{matchId}/draft")]
    public async Task<ActionResult<FreezerDraftResponse>> GetDraft(
        string matchId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await coordinator.GetDraftAsync(AccountId(), matchId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("matches/{matchId}/draft")]
    public async Task<ActionResult<FreezerDraftResponse>> SaveDraft(
        string matchId,
        FreezerDraftRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var saved = await coordinator.SaveDraftAsync(AccountId(), matchId, request, cancellationToken);
            return saved is null ? Conflict(new ProblemDetails { Title = "The draft is frozen or has changed." }) : Ok(saved);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception.Message);
        }
    }

    [HttpGet("matches/{matchId}/estimate")]
    public async Task<ActionResult<FreezerEstimateResponse>> GetEstimate(
        string matchId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await coordinator.EstimateAsync(AccountId(), matchId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("matches/{matchId}/freeze")]
    public async Task<ActionResult<FreezerSnapshotResponse>> Freeze(
        string matchId,
        FreezerFreezeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await coordinator.FreezeAsync(
                AccountId(),
                matchId,
                request.ExpectedRevision,
                cancellationToken);
            return snapshot is null
                ? Conflict(new ProblemDetails { Title = "The draft has changed or is already frozen." })
                : Ok(snapshot);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("snapshots/{snapshotId}")]
    public async Task<ActionResult<FreezerSnapshotResponse>> GetSnapshot(
        string snapshotId,
        CancellationToken cancellationToken)
    {
        var snapshot = await coordinator.GetSnapshotAsync(AccountId(), snapshotId, cancellationToken);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    [HttpGet("snapshots")]
    public async Task<ActionResult<IReadOnlyList<FreezerSnapshotSelectionResponse>>> ListSnapshots(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default) =>
        Ok(await coordinator.ListSnapshotsAsync(AccountId(), limit, cancellationToken));

    [HttpGet("snapshots/{snapshotId}/replay")]
    public async Task<ActionResult<FreezerSnapshotReplayResponse>> ReplaySnapshot(
        string snapshotId,
        CancellationToken cancellationToken)
    {
        var replay = await coordinator.ReplayAsync(AccountId(), snapshotId, cancellationToken);
        return replay is null ? NotFound() : Ok(replay);
    }

    [HttpPost("snapshots/{snapshotId}/verification")]
    public async Task<ActionResult<FreezerVerificationResponse>> VerifySnapshot(
        string snapshotId,
        FreezerVerificationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await coordinator.VerifyAsync(
                AccountId(),
                snapshotId,
                request,
                cancellationToken);
            return report is null ? NotFound() : Ok(report);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception.Message);
        }
    }

    [HttpPost("parameter-backtests")]
    public async Task<ActionResult<FreezerParameterBacktestResponse>> BacktestParameters(
        FreezerParameterBacktestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await coordinator.BacktestParametersAsync(AccountId(), request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception.Message);
        }
    }

    [HttpGet("parameter-backtests/{backtestId}")]
    public async Task<ActionResult<FreezerParameterBacktestResponse>> GetParameterBacktest(
        string backtestId,
        CancellationToken cancellationToken)
    {
        var report = await coordinator.GetParameterBacktestAsync(AccountId(), backtestId, cancellationToken);
        return report is null ? NotFound() : Ok(report);
    }

    [HttpGet("parameters/adoption")]
    public async Task<ActionResult<FreezerParameterAdoptionResponse>> GetParameterAdoption(
        CancellationToken cancellationToken)
    {
        var adoption = await coordinator.GetLatestParameterAdoptionAsync(AccountId(), cancellationToken);
        return adoption is null ? NotFound() : Ok(adoption);
    }

    [HttpPost("parameters/adoption")]
    public async Task<ActionResult<FreezerParameterAdoptionResponse>> AdoptParameters(
        FreezerParameterAdoptionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await coordinator.AdoptParametersAsync(AccountId(), request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Title = "Parameter adoption conflict", Detail = exception.Message });
        }
    }

    [HttpPost("snapshots/{snapshotId}/field-events")]
    public async Task<ActionResult<FreezerFieldEventResponse>> AppendFieldEvent(
        string snapshotId,
        FreezerFieldEventRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var saved = await coordinator.AppendFieldEventAsync(
                AccountId(),
                snapshotId,
                request,
                cancellationToken);
            return saved is null
                ? Conflict(new ProblemDetails { Title = "The operation id already belongs to another snapshot." })
                : Ok(saved);
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception.Message);
        }
    }

    [HttpGet("snapshots/{snapshotId}/field-events")]
    public async Task<ActionResult<IReadOnlyList<FreezerFieldEventResponse>>> GetFieldEvents(
        string snapshotId,
        CancellationToken cancellationToken)
    {
        var events = await coordinator.GetFieldEventsAsync(AccountId(), snapshotId, cancellationToken);
        return events is null ? NotFound() : Ok(events);
    }

    [HttpPost("snapshots/{snapshotId}/artifacts")]
    public async Task<IActionResult> CreateArtifacts(
        string snapshotId,
        CancellationToken cancellationToken)
    {
        var artifact = await coordinator.CreateOrGetArtifactAsync(
            AccountId(),
            snapshotId,
            cancellationToken);
        if (artifact is null)
            return NotFound();
        Response.Headers.ETag = $"\"{artifact.Value.Manifest.Sha256}\"";
        return File(
            artifact.Value.Archive,
            artifact.Value.Manifest.ContentType,
            $"freezer-card-{snapshotId}.zip");
    }

    [HttpGet("artifacts/{artifactId}")]
    public async Task<IActionResult> GetArtifact(
        string artifactId,
        CancellationToken cancellationToken)
    {
        var artifact = await coordinator.GetArtifactAsync(AccountId(), artifactId, cancellationToken);
        if (artifact is null)
            return NotFound();
        Response.Headers.ETag = $"\"{artifact.Value.Manifest.Sha256}\"";
        return File(
            artifact.Value.Archive,
            artifact.Value.Manifest.ContentType,
            $"freezer-card-{artifact.Value.Manifest.SnapshotId}.zip");
    }

    private string AccountId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The authenticated account id claim is missing.");

    private async Task<ReadOnlyMemory<byte>> ReadUploadAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0 || file.Length > hostOptions.Value.MaximumUploadBytes)
            throw new InvalidDataException("The selected file is empty or exceeds the configured upload limit.");
        await using var content = new MemoryStream((int)file.Length);
        await using var upload = file.OpenReadStream();
        await upload.CopyToAsync(content, cancellationToken);
        if (content.Length != file.Length || content.Length > hostOptions.Value.MaximumUploadBytes)
            throw new InvalidDataException("The uploaded file length does not match the request metadata or exceeds the configured limit.");
        return content.ToArray();
    }
}

public sealed class ScoreImportForm
{
    [FromForm(Name = "file")]
    public IFormFile? File { get; init; }

    [FromForm(Name = "matchName")]
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(200)]
    public string MatchName { get; init; } = string.Empty;

    [FromForm(Name = "matchDate")]
    [System.ComponentModel.DataAnnotations.Required]
    public DateOnly MatchDate { get; init; }
}

public sealed class PdfPairImportForm
{
    [FromForm(Name = "classificationFile")]
    [System.ComponentModel.DataAnnotations.Required]
    public IFormFile? ClassificationFile { get; init; }

    [FromForm(Name = "verificationFile")]
    [System.ComponentModel.DataAnnotations.Required]
    public IFormFile? VerificationFile { get; init; }
}
