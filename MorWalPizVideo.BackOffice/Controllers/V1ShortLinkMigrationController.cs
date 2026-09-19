using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/v1/maintenance/short-links")]
public sealed class V1ShortLinkMigrationController(
    IShortLinkMigrationService migrationService,
    IMongoIndexOperationsService? indexOperationsService = null) : ControllerBase
{
    [HttpPost("migrate")]
    [AllowUser(AuthorizationPermissionKeys.BackofficeManageAll)]
    [ProducesResponseType(typeof(V1ShortLinkMigrationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<V1ShortLinkMigrationResponse>> Migrate(
        [FromQuery] bool dryRun = true,
        [FromQuery] bool resume = true,
        [FromQuery] int batchSize = 100,
        [FromQuery] bool inventoryOnly = false,
        [FromQuery] bool rollbackOnFailure = false,
        CancellationToken cancellationToken = default)
        => ToResponse(await migrationService.RunAsync(
            new ShortLinkMigrationOptions(dryRun, resume, batchSize, inventoryOnly, rollbackOnFailure),
            cancellationToken));

    [HttpGet("validate")]
    [AllowUser(AuthorizationPermissionKeys.BackofficeManageAll)]
    public async Task<ActionResult<ShortLinkMigrationValidationResponse>> Validate(CancellationToken cancellationToken = default)
    {
        var migration = await migrationService.RunAsync(
            new ShortLinkMigrationOptions(DryRun: true, Resume: false, InventoryOnly: true), cancellationToken);
        if (indexOperationsService is null)
        {
            return Ok(new V1ShortLinkMigrationValidationResponse(
                ToResponse(migration),
                "unavailable",
                []));
        }

        var indexes = await indexOperationsService.AuditAsync(["shortlinks.code.unique"], cancellationToken);
        return Ok(new V1ShortLinkMigrationValidationResponse(
            ToResponse(migration),
            indexes.SingleOrDefault()?.Exists == true ? "verified" : "missing",
            indexes.Select(index => new V1ShortLinkIndexValidationResponse(
                index.Key,
                index.Exists,
                index.SpecJson)).ToArray()));
    }

    private static V1ShortLinkMigrationResponse ToResponse(ShortLinkMigrationReport report)
        => new(
            report.DryRun,
            report.Completed,
            report.LastProcessedMatchId,
            report.InspectedMatches,
            report.CanonicalLinksCreated,
            report.CanonicalLinksReplaced,
            report.EmbeddedLinksRemoved,
            report.Conflicts.ToArray(),
            report.MissingReferences.ToArray(),
            report.DuplicateCodes.ToArray(),
            report.MalformedCodes.ToArray(),
            report.Failures.ToArray(),
            report.ArchivalExceptions.ToArray(),
            report.ResumeMarker,
            report.RollbackStatus);
}

public sealed record ShortLinkMigrationValidationResponse(
    ShortLinkMigrationReport Migration,
    string IndexStatus,
    IReadOnlyList<ShortLinkIndexValidationResponse> Indexes);

public sealed record ShortLinkIndexValidationResponse(
    string Key,
    bool Exists,
    string Specification);