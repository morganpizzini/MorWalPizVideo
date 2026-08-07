using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Constraints;
using RuntimeHealthCheckService = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService;

namespace MorWalPizVideo.BackOffice.Controllers;

[AllowUser("group:" + AuthorizationGroupCodes.Admin, "group:" + AuthorizationGroupCodes.Contributor)]
public sealed class DiagnosticsController : ApplicationControllerBase
{
    private readonly RuntimeHealthCheckService _healthChecks;
    private readonly DiagnosticsProblemStore _problemStore;

    public DiagnosticsController(RuntimeHealthCheckService healthChecks, DiagnosticsProblemStore problemStore)
    {
        _healthChecks = healthChecks;
        _problemStore = problemStore;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var report = await _healthChecks.CheckHealthAsync(cancellationToken);
        return Ok(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(
                item => item.Key,
                item => new
                {
                    status = item.Value.Status.ToString(),
                    description = DiagnosticsRedactor.Redact(item.Value.Description),
                    durationMilliseconds = item.Value.Duration.TotalMilliseconds
                }),
            recentProblems = _problemStore.GetRecent(25)
        });
    }
}