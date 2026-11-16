using System.Security.Claims;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.Contracts.Workshop;
using MorWalPizVideo.Domain.Workshop;
using MorWalPizVideo.Models.Models.Workshop;
using MorWalPizVideo.MvcHelpers.Authentication;

namespace MorWalPizVideo.ServerAPI.Controllers.Workshop;

[ApiController]
[EnableCors("public-account")]
[Authorize(AuthenticationSchemes = PublicAccountSecurity.Scheme)]
[Route("api/v1/workshop")]
public sealed class WorkshopController(WorkshopService service) : ControllerBase
{
    [HttpGet("firearms")]
    public async Task<IActionResult> Firearms(CancellationToken ct) =>
        Ok(await service.GetFirearmsAsync(AccountId(), ct));

    [HttpGet("firearms/{firearmId}/detail")]
    public async Task<IActionResult> FirearmDetail(string firearmId, [FromQuery] int maintenancePage = 1,
        [FromQuery] int shootingPage = 1, [FromQuery] int pageSize = 15, CancellationToken ct = default)
    {
        var detail = await service.GetFirearmDetailAsync(AccountId(), firearmId, maintenancePage, shootingPage, pageSize, ct);
        if (detail is null) return NotFound();
        var shooting = detail.Shooting;
        return Ok(new WorkshopFirearmDetailContract(
            detail.Firearm,
            detail.Components.Select(x => new WorkshopComponentConditionContract(x.Component, x.Definition, x.Installation)).ToArray(),
            new(detail.Maintenance, maintenancePage, Math.Clamp(pageSize, 1, 100), detail.MaintenanceHasMore),
            new(shooting.Select(x => new WorkshopShootingSessionContract(x.Session, x.Superseded)).ToArray(), shootingPage,
                Math.Clamp(pageSize, 1, 100), detail.ShootingHasMore),
            detail.RoundHistory.Select(x => new WorkshopRoundHistoryPointContract(x.EffectiveAt, x.Delta, x.CumulativeRounds, x.OperationId)).ToArray(),
            detail.ReplacementCandidates, detail.ProjectionState,
            detail.MalfunctionCount));
    }

    [HttpPost("firearms")]
    public async Task<IActionResult> CreateFirearm(CreateWorkshopFirearmRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            return Ok(await service.CreateFirearmAsync(AccountId(), new(request.Manufacturer, request.Model, request.Caliber,
                request.Category, request.Nickname, request.PurchaseDate, request.Notes, request.InitialRounds,
                request.IdempotencyKey), ct));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
    }

    [HttpPut("firearms/{firearmId}")]
    public async Task<IActionResult> UpdateFirearm(string firearmId, UpdateWorkshopFirearmRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            return Ok(await service.UpdateFirearmAsync(AccountId(), firearmId,
                new(request.Manufacturer, request.Model, request.Caliber, request.Category, request.Nickname,
                    request.PurchaseDate, request.Notes, request.Revision, request.IdempotencyKey ?? string.Empty), ct));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("firearms/{firearmId}/archive")]
    public async Task<IActionResult> ArchiveFirearm(string firearmId, ArchiveWorkshopFirearmRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            return Ok(await service.ArchiveFirearmAsync(AccountId(), firearmId,
                new(request.Revision, request.IdempotencyKey), ct));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpGet("components/definitions")]
    public async Task<IActionResult> Definitions(CancellationToken ct) =>
        Ok(await service.GetComponentDefinitionsAsync(AccountId(), ct));

    [HttpPost("components/definitions")]
    public async Task<IActionResult> CreateDefinition(CreateWorkshopComponentDefinitionRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            return Ok(await service.CreateComponentDefinitionAsync(AccountId(), new(request.Name, request.Manufacturer,
                request.Model, request.Description, request.DefaultServiceInterval, request.TrackingMode, request.Notes,
                request.CompatibilityTags), ct));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
    }

    [HttpPut("components/definitions/{definitionId}")]
    public async Task<IActionResult> UpdateDefinition(string definitionId, UpdateWorkshopComponentDefinitionRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try { return Ok(await service.UpdateComponentDefinitionAsync(AccountId(), definitionId,
            new(request.Name, request.Description, request.Notes, request.Revision, request.IdempotencyKey), ct)); }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("components/definitions/{definitionId}/archive")]
    public async Task<IActionResult> ArchiveDefinition(string definitionId, ArchiveWorkshopComponentDefinitionRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try { return Ok(await service.ArchiveComponentDefinitionAsync(AccountId(), definitionId,
            new(request.Revision, request.IdempotencyKey), ct)); }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpDelete("components/definitions/{definitionId}")]
    public async Task<IActionResult> DeleteDefinition(string definitionId, DeleteWorkshopComponentDefinitionRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try { await service.DeleteComponentDefinitionAsync(AccountId(), definitionId, request.Revision, ct); return NoContent(); }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpGet("components")]
    public async Task<IActionResult> Components(CancellationToken ct) =>
        Ok(await service.GetComponentsAsync(AccountId(), ct));

    [HttpGet("components/installations")]
    public async Task<IActionResult> Installations(CancellationToken ct) =>
        Ok(await service.GetComponentInstallationsAsync(AccountId(), ct));

    [HttpPost("components/installations")]
    public async Task<IActionResult> Install(InstallWorkshopComponentRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try { return Ok(await service.InstallComponentAsync(AccountId(), new(request.ComponentId, request.FirearmId,
            request.InstalledAt, request.InstallationNotes, request.IdempotencyKey), ct)); }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("components/installations/{installationId}/remove")]
    public async Task<IActionResult> Remove(string installationId, RemoveWorkshopComponentRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try { return Ok(await service.RemoveComponentAsync(AccountId(), new(installationId, request.RemovedAt,
            request.RemovalNotes, request.Revision, request.IdempotencyKey), ct)); }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("maintenance")]
    public async Task<IActionResult> Maintenance(RecordWorkshopMaintenanceRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try { return Ok(await service.RecordMaintenanceAsync(AccountId(), new(request.FirearmId, request.ComponentId,
            request.EventType, request.Date, request.RoundCountAtEvent, request.Description, request.Notes,
            request.IdempotencyKey), ct)); }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("components")]
    public async Task<IActionResult> CreateComponent(CreateWorkshopComponentRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try { return Ok(await service.CreateComponentAsync(AccountId(), new(request.DefinitionId, request.IdentificationNote,
            request.Manufacturer, request.Model, request.ServiceInterval, request.TrackingMode, request.Notes,
            request.CompatibilityTags), ct)); }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
    }

    [HttpPost("components/install")]
    public async Task<IActionResult> CreateAndInstall(CreateAndInstallWorkshopComponentRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            var result = await service.CreateAndInstallComponentAsync(AccountId(), new(request.DefinitionId, request.FirearmId,
                request.IdentificationNote, request.Manufacturer, request.Model, request.ServiceInterval, request.TrackingMode,
                request.Notes, request.InstalledAt, request.InstallationNotes, request.IdempotencyKey, request.CompatibilityTags), ct);
            return Ok(new CreateAndInstallWorkshopComponentResponse(result.Component, result.Installation));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPut("components/{componentId}")]
    public async Task<IActionResult> UpdateComponent(string componentId, UpdateWorkshopComponentRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try { return Ok(await service.UpdateComponentAsync(AccountId(), componentId, new(request.IdentificationNote,
            request.Manufacturer, request.Model, request.ServiceInterval, request.TrackingMode ?? WorkshopTrackingMode.Counter,
            request.Notes, request.CompatibilityTags, request.Revision, request.IdempotencyKey), ct)); }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("components/{componentId}/archive")]
    public async Task<IActionResult> ArchiveComponent(string componentId, ArchiveWorkshopComponentRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try { return Ok(await service.ArchiveComponentAsync(AccountId(), componentId,
            new(request.Revision, request.IdempotencyKey), ct)); }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpDelete("components/{componentId}")]
    public async Task<IActionResult> DeleteComponent(string componentId, DeleteWorkshopComponentRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try { await service.DeleteComponentAsync(AccountId(), componentId, request.Revision, ct); return NoContent(); }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpGet("recipes")]
    public async Task<IActionResult> Recipes(CancellationToken ct) =>
        Ok(await service.GetRecipesAsync(AccountId(), ct));

    [HttpGet("recipes/{recipeId}")]
    public async Task<IActionResult> Recipe(string recipeId, CancellationToken ct) =>
        await service.GetActiveRecipeAsync(AccountId(), recipeId, ct) is { } recipe
            ? Ok(recipe)
            : NotFound();

    [HttpPost("recipes")]
    public async Task<IActionResult> CreateRecipe(WorkshopRecipeRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try { return Ok(await service.CreateRecipeAsync(AccountId(), Snapshot(request), ct)); }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
    }

    [HttpPut("recipes/{recipeId}")]
    public async Task<IActionResult> UpdateRecipe(string recipeId, UpdateWorkshopRecipeRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            return Ok(await service.UpdateRecipeAsync(AccountId(), recipeId, request.Version, Snapshot(request),
                request.IdempotencyKey, ct));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("recipes/{recipeId}/archive")]
    public async Task<IActionResult> ArchiveRecipe(string recipeId, ArchiveWorkshopRecipeRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try { return Ok(await service.ArchiveRecipeAsync(AccountId(), recipeId, request.Version, request.IdempotencyKey, ct)); }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpGet("ammunition/batches")]
    public async Task<IActionResult> Batches(CancellationToken ct) =>
        Ok(await service.GetBatchesAsync(AccountId(), ct));

    [HttpPost("ammunition/production")]
    public async Task<IActionResult> Produce(RecordWorkshopProductionRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            var result = await service.RecordProductionAsync(AccountId(),
                new(request.RecipeId, request.ProductionDate, request.QuantityProduced, request.Notes, request.IdempotencyKey), ct);
            return Ok(ToContract(result));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("sessions/shooting")]
    public async Task<IActionResult> Shoot(RecordWorkshopShootingRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            var result = await service.RecordShootingAsync(AccountId(), new(request.FirearmId, request.Date, request.TotalRounds,
                request.Notes, request.AmmunitionRecords.Select(x => new ShootingRecipeConsumption(x.RecipeId, x.Quantity)).ToArray(),
                request.UntrackedAmmunition, request.UsageSegments.Select(x => new ShootingUsageSegment(x.ComponentId, x.InstallationId, x.Quantity)).ToArray(),
                request.IdempotencyKey), ct);
            return Ok(ToContract(result));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpGet("sessions/shooting")]
    public async Task<IActionResult> ShootingSessions(CancellationToken ct) =>
        Ok((await service.GetShootingSessionsAsync(AccountId(), ct))
            .Select(x => new WorkshopShootingSessionReadContract(x.Session, x.Superseded)));

    [HttpPost("sessions/shooting/correct")]
    public async Task<IActionResult> CorrectShooting(CorrectWorkshopOperationRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            var replacement = request.ReplacementShooting;
            var command = replacement is null ? null : ToShootingCommand(replacement, true);
            var result = await service.CorrectShootingAsync(AccountId(),
                new(request.OriginalOperationId, command, request.IdempotencyKey), ct);
            return Ok(ToContract(result));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var dashboard = await service.GetDashboardAsync(AccountId(), ct);
        return Ok(new WorkshopDashboardContract(dashboard.FirearmCount, dashboard.ComponentCount,
            dashboard.TotalFirearmRounds, dashboard.ComponentsAtOrAboveInterval, dashboard.TotalProducedRounds,
            dashboard.AvailableRounds, dashboard.RecentActivityCount,
            dashboard.ProjectionState));
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var end = to ?? DateTime.UtcNow;
        var start = from ?? end.AddDays(-30);
        try
        {
            var analytics = await service.GetAnalyticsAsync(AccountId(), start, end, ct);
            return Ok(new WorkshopAnalyticsContract(analytics.From, analytics.To, analytics.ShootingRounds,
                analytics.ShootingSessions, analytics.TrackedRounds, analytics.UntrackedRounds,
                analytics.ProducedRounds, analytics.AvailableRounds, analytics.MaintenanceEvents,
                analytics.MalfunctionEvents, analytics.CorrectionCount, analytics.ProjectionState,
                analytics.Buckets.Select(x => new WorkshopAnalyticsBucketContract(x.Date, x.ShootingRounds,
                    x.ProducedRounds, x.ShootingSessions)).ToArray()));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] int page = 1, [FromQuery] int pageSize = 100,
        [FromQuery] WorkshopOperationType? type = null, CancellationToken ct = default) =>
        Ok(await service.GetActivitiesAsync(AccountId(), page, pageSize, type, ct));

    [HttpGet("report")]
    public async Task<IActionResult> Report([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string? format = null, CancellationToken ct = default)
    {
        var end = to ?? DateTime.UtcNow;
        var start = from ?? end.AddDays(-30);
        try
        {
            var report = await service.GetReportAsync(AccountId(), start, end, ct);
            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
                return File(Encoding.UTF8.GetBytes(ToCsv(report)), "text/csv",
                    $"workshop-report-{start:yyyyMMdd}-{end:yyyyMMdd}.csv");

            var analytics = report.Analytics;
            return Ok(new WorkshopReportContract(
                new(analytics.From, analytics.To, analytics.ShootingRounds, analytics.ShootingSessions,
                    analytics.TrackedRounds, analytics.UntrackedRounds, analytics.ProducedRounds,
                    analytics.AvailableRounds, analytics.MaintenanceEvents, analytics.MalfunctionEvents,
                    analytics.CorrectionCount, analytics.ProjectionState,
                    analytics.Buckets.Select(x => new WorkshopAnalyticsBucketContract(x.Date, x.ShootingRounds,
                        x.ProducedRounds, x.ShootingSessions)).ToArray()),
                report.Activities.Select(x => new WorkshopReportActivityContract(
                    x.Id, x.OperationId, x.Type, x.EffectiveAt, x.RecordedAt, x.Summary, x.EntityLabel)).ToArray()));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
    }

    [HttpGet("reconciliation")]
    public async Task<IActionResult> Reconciliation(CancellationToken ct)
    {
        var result = await service.ReconcileAsync(AccountId(), false, ct);
        return Ok(new WorkshopReconciliationContract(result.State.ToString(), result.Mismatches.Count, result.CompletedAt, result.Mismatches));
    }

    [HttpPost("reconciliation/rebuild")]
    public async Task<IActionResult> Rebuild(CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        var result = await service.ReconcileAsync(AccountId(), true, ct);
        return Ok(new WorkshopReconciliationContract(result.State.ToString(), result.Mismatches.Count, result.CompletedAt, result.Mismatches));
    }

    private string AccountId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Authenticated account claim is missing.");
    private bool HasCsrf() => PublicAccountSecurity.HasValidCsrf(Request);
    private static RecipeSnapshot Snapshot(WorkshopRecipeRequest request) =>
        new(request.Name, request.Caliber, request.Oal, request.BulletWeight, request.BulletForm, request.Drawing,
            request.Primer, request.Powder, request.PowderWeight, request.Notes);
    private static WorkshopOperationContract ToContract(WorkshopOperationResult result) =>
        new(result.Operation.Id, result.Operation.OperationType, result.Operation.EffectiveAt,
            result.Operation.RecordedAt, result.Operation.ContributionDigest);

    private static string ToCsv(WorkshopReportData report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("section,date,type,label,summary,shootingRounds,producedRounds,shootingSessions");
        foreach (var bucket in report.Analytics.Buckets)
            builder.AppendLine(string.Join(',', "analytics", Csv(bucket.Date.ToString("O", CultureInfo.InvariantCulture)),
                string.Empty, string.Empty, string.Empty, bucket.ShootingRounds, bucket.ProducedRounds, bucket.ShootingSessions));
        foreach (var activity in report.Activities)
            builder.AppendLine(string.Join(',', "activity", Csv(activity.EffectiveAt.ToString("O", CultureInfo.InvariantCulture)),
                Csv(activity.Type.ToString()), Csv(activity.EntityLabel), Csv(activity.Summary), string.Empty, string.Empty, string.Empty));
        return builder.ToString();
    }

    private static string Csv(string? value) =>
        $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

    private static WorkshopShootingCommand ToShootingCommand(RecordWorkshopShootingRequest request, bool deriveTotal = false) =>
        new(request.FirearmId, request.Date,
            deriveTotal ? request.AmmunitionRecords.Sum(x => x.Quantity) + request.UntrackedAmmunition : request.TotalRounds,
            request.Notes,
            request.AmmunitionRecords.Select(x => new ShootingRecipeConsumption(x.RecipeId, x.Quantity)).ToArray(),
            request.UntrackedAmmunition,
            request.UsageSegments.Select(x => new ShootingUsageSegment(x.ComponentId, x.InstallationId, x.Quantity)).ToArray(),
            request.IdempotencyKey);
}
