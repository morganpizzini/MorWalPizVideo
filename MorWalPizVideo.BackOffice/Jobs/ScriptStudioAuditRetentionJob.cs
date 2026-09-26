using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Jobs;

public sealed class ScriptStudioAuditRetentionJob(
    IScriptStudioAuditRepository auditRepository,
    ILogger<ScriptStudioAuditRetentionJob> logger)
{
    public const string JobId = "script-studio-audit-retention-job";

    public async Task ExecuteAsync()
    {
        await auditRepository.DeleteExpiredAsync(DateTime.UtcNow);
        logger.LogInformation("Script Studio audit retention completed");
    }
}