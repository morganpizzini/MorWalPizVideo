using System.Security.Claims;
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

    [HttpPost("firearms")]
    public async Task<IActionResult> CreateFirearm(CreateWorkshopFirearmRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            return Ok(await service.CreateFirearmAsync(AccountId(), new(request.Manufacturer, request.Model, request.Caliber,
                request.Category, request.Nickname, request.PurchaseDate, request.Notes), ct));
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
                    request.PurchaseDate, request.Notes, request.Revision, request.IdempotencyKey), ct));
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
                request.Model, request.Description, request.DefaultServiceInterval, request.TrackingMode, request.Notes), ct));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
    }

    [HttpGet("components")]
    public async Task<IActionResult> Components(CancellationToken ct) =>
        Ok(await service.GetComponentsAsync(AccountId(), ct));

    [HttpPost("components/installations")]
    public async Task<IActionResult> Install(InstallWorkshopComponentRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            return Ok(await service.InstallComponentAsync(AccountId(), new(request.ComponentId, request.FirearmId,
                request.InstalledAt, request.InstallationNotes, request.IdempotencyKey), ct));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("components/installations/{installationId}/remove")]
    public async Task<IActionResult> Remove(string installationId, RemoveWorkshopComponentRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            return Ok(await service.RemoveComponentAsync(AccountId(), new(installationId, request.RemovedAt,
                request.RemovalNotes, request.Revision, request.IdempotencyKey), ct));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("maintenance")]
    public async Task<IActionResult> Maintenance(RecordWorkshopMaintenanceRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            return Ok(await service.RecordMaintenanceAsync(AccountId(), new(request.FirearmId, request.ComponentId,
                request.EventType, request.Date, request.RoundCountAtEvent, request.Description, request.Notes,
                request.IdempotencyKey), ct));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("components")]
    public async Task<IActionResult> CreateComponent(CreateWorkshopComponentRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            return Ok(await service.CreateComponentAsync(AccountId(), new(request.DefinitionId, request.IdentificationNote,
                request.Manufacturer, request.Model, request.ServiceInterval, request.TrackingMode, request.Notes), ct));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
    }

    [HttpGet("recipes")]
    public async Task<IActionResult> Recipes(CancellationToken ct) =>
        Ok(await service.GetRecipesAsync(AccountId(), ct));

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
                request.Notes, request.ConsumptionRecords.Select(x => new ShootingConsumption(x.BatchId, x.Quantity, x.AllocationId)).ToArray(),
                request.UntrackedAmmunition, request.UsageSegments.Select(x => new ShootingUsageSegment(x.ComponentId, x.InstallationId, x.Quantity)).ToArray(),
                request.IdempotencyKey), ct);
            return Ok(ToContract(result));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("ammunition/allocations")]
    public async Task<IActionResult> Allocate(AllocateWorkshopAmmoRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            var result = await service.AllocateAsync(AccountId(),
                new(request.BatchId, request.FirearmId, request.Quantity, request.Notes, request.IdempotencyKey), ct);
            return Ok(ToContract(result));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("ammunition/allocations/{allocationId}/release")]
    public async Task<IActionResult> Release(string allocationId, ReleaseWorkshopAllocationRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            var result = await service.ReleaseAllocationAsync(AccountId(),
                new(allocationId, request.Quantity, request.Revision, request.IdempotencyKey), ct);
            return Ok(ToContract(result));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("ammunition/allocations/reallocate")]
    public async Task<IActionResult> Reallocate(ReallocateWorkshopAmmoRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            var result = await service.ReallocateAsync(AccountId(),
                new(request.SourceAllocationId, request.DestinationFirearmId, request.Quantity, request.IdempotencyKey), ct);
            return Ok(ToContract(result));
        }
        catch (WorkshopValidationException ex) { return ValidationProblem(ex.Message); }
        catch (WorkshopConflictException ex) { return Conflict(new ProblemDetails { Title = ex.Message }); }
    }

    [HttpPost("sessions/shooting/correct")]
    public async Task<IActionResult> CorrectShooting(CorrectWorkshopOperationRequest request, CancellationToken ct)
    {
        if (!HasCsrf()) return Forbid();
        try
        {
            var replacement = request.ReplacementShooting;
            var command = replacement is null ? null : ToShootingCommand(replacement);
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
            dashboard.AvailableRounds, dashboard.AllocatedRounds, dashboard.RecentActivityCount,
            dashboard.ProjectionState));
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] int page = 1, [FromQuery] int pageSize = 100,
        [FromQuery] WorkshopOperationType? type = null, CancellationToken ct = default) =>
        Ok(await service.GetActivitiesAsync(AccountId(), page, pageSize, type, ct));

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

    private static WorkshopShootingCommand ToShootingCommand(RecordWorkshopShootingRequest request) =>
        new(request.FirearmId, request.Date, request.TotalRounds, request.Notes,
            request.ConsumptionRecords.Select(x => new ShootingConsumption(x.BatchId, x.Quantity, x.AllocationId)).ToArray(),
            request.UntrackedAmmunition,
            request.UsageSegments.Select(x => new ShootingUsageSegment(x.ComponentId, x.InstallationId, x.Quantity)).ToArray(),
            request.IdempotencyKey);
}
