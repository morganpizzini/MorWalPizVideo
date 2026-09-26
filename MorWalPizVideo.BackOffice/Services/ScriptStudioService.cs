using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Configuration;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using BackOfficeAzureConfig = MorWalPizVideo.BackOffice.Configuration.AzureConfig;

namespace MorWalPizVideo.BackOffice.Services;

public interface IScriptStudioService
{
    Task<ScriptStudioChannelData> GetAsync(string channelId, CancellationToken cancellationToken);
    Task<ScriptStudioChannelData> SaveAsync(string channelId, ScriptStudioDocumentDto document, CancellationToken cancellationToken);
    Task<ScriptStudioGenerationResponse> GenerateAsync(string userId, string channelId, ScriptStudioGenerationRequest request, string correlationId, CancellationToken cancellationToken);
    Task<ScriptStudioGlobalPrompt> GetGlobalPromptAsync(CancellationToken cancellationToken);
    Task<ScriptStudioGlobalPrompt> SaveGlobalPromptAsync(string prompt, CancellationToken cancellationToken);
    Task SetQuotaAsync(string userId, int monthlyQuota, CancellationToken cancellationToken);
}

public sealed class ScriptStudioService(
    IScriptStudioRepository repository,
    IScriptStudioGlobalPromptRepository globalPromptRepository,
    IScriptStudioAuditRepository auditRepository,
    IUserRepository userRepository,
    Kernel kernel,
    IOptions<ScriptStudioOptions> options,
    ILogger<ScriptStudioService> logger,
    IOptions<BackOfficeAzureConfig> azureConfig) : IScriptStudioService
{
    private readonly ScriptStudioOptions options = options.Value;
    private readonly BackOfficeAzureConfig azureConfig = azureConfig.Value;

    public async Task<ScriptStudioChannelData> GetAsync(string channelId, CancellationToken cancellationToken)
        => await repository.GetByChannelIdAsync(channelId, cancellationToken) ?? new ScriptStudioChannelData { Id = Guid.NewGuid().ToString("N"), ChannelId = channelId };

    public async Task<ScriptStudioChannelData> SaveAsync(string channelId, ScriptStudioDocumentDto document, CancellationToken cancellationToken)
    {
        ValidateFormat(document.Format);
        return await repository.UpsertAsync(new ScriptStudioChannelData
        {
            Id = string.IsNullOrWhiteSpace(document.ChannelId) ? Guid.NewGuid().ToString("N") : document.ChannelId,
            ChannelId = channelId,
            Script = document.Script ?? string.Empty,
            SavedPrompt = document.SavedPrompt ?? string.Empty,
            Examples = document.Examples ?? string.Empty,
            Style = document.Style ?? string.Empty,
            GeneralContext = document.GeneralContext ?? string.Empty,
            SavedResult = document.SavedResult ?? string.Empty,
            Format = document.Format.Trim().ToLowerInvariant()
        }, cancellationToken);
    }

    public async Task<ScriptStudioGenerationResponse> GenerateAsync(string userId, string channelId, ScriptStudioGenerationRequest request, string correlationId, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ScriptStudioOperation>(request.Operation, true, out var operation)) throw new ArgumentException("Unsupported operation.");
        ValidateFormat(request.Format);
        var period = DateTime.UtcNow.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
        var quota = await userRepository.ConsumeScriptStudioQuotaAsync(userId, period, cancellationToken);
        if (!quota.Consumed)
        {
            await WriteAuditAsync(userId, channelId, operation, false, true, quota, correlationId, null, Stopwatch.GetTimestamp(), cancellationToken);
            throw new InvalidOperationException("Monthly Script Studio quota exceeded.");
        }

        var started = Stopwatch.GetTimestamp();
        var global = await globalPromptRepository.GetCurrentAsync(cancellationToken);
        var prompt = ComposePrompt(global?.Prompt, request, operation);
        try
        {
#pragma warning disable SKEXP0010
            var result = await kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
#pragma warning restore SKEXP0010
            await WriteAuditAsync(userId, channelId, operation, true, false, quota, correlationId, global?.Version, started, cancellationToken);
            return new ScriptStudioGenerationResponse { Result = result.ToString(), Format = request.Format.Trim().ToLowerInvariant(), QuotaLimit = quota.Limit, QuotaUsed = quota.UsedAfter };
        }
        catch
        {
            await WriteAuditAsync(userId, channelId, operation, false, false, quota, correlationId, global?.Version, started, cancellationToken);
            throw;
        }
    }

    public async Task<ScriptStudioGlobalPrompt> GetGlobalPromptAsync(CancellationToken cancellationToken)
        => await globalPromptRepository.GetCurrentAsync(cancellationToken) ?? new ScriptStudioGlobalPrompt { Id = Guid.NewGuid().ToString("N") };

    public async Task<ScriptStudioGlobalPrompt> SaveGlobalPromptAsync(string prompt, CancellationToken cancellationToken)
    {
        if (prompt is null) throw new ArgumentNullException(nameof(prompt));
        if (prompt.Length > options.GlobalPromptMaxLength)
            throw new ArgumentException($"Global prompt cannot exceed {options.GlobalPromptMaxLength} characters.", nameof(prompt));

        var current = await GetGlobalPromptAsync(cancellationToken);
        return await globalPromptRepository.SaveAsync(current with { Prompt = prompt.Trim(), Version = current.Version + 1 }, cancellationToken);
    }

    public async Task SetQuotaAsync(string userId, int monthlyQuota, CancellationToken cancellationToken)
    {
        if (monthlyQuota < 0) throw new ArgumentOutOfRangeException(nameof(monthlyQuota));
        var user = await userRepository.GetItemAsync(userId) ?? throw new KeyNotFoundException("User not found.");
        await userRepository.UpdateItemAsync(user with { ScriptStudioMonthlyQuota = monthlyQuota });
    }

    internal static string ComposePrompt(string? globalPrompt, ScriptStudioGenerationRequest request, ScriptStudioOperation operation)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(globalPrompt)) builder.AppendLine(globalPrompt.Trim());
        builder.AppendLine($"Operation: {operation}");
        builder.AppendLine($"Format: {request.Format.Trim().ToLowerInvariant()}");
        builder.AppendLine($"Style:\n{request.Style}");
        builder.AppendLine($"Context:\n{request.GeneralContext}");
        builder.AppendLine($"Examples:\n{request.Examples}");
        builder.AppendLine($"User prompt:\n{request.Prompt}");
        builder.AppendLine($"Script:\n{request.Script}");
        return builder.ToString();
    }

    private async Task WriteAuditAsync(string userId, string channelId, ScriptStudioOperation operation, bool success, bool deniedQuota, ScriptStudioQuotaConsumption quota, string correlationId, int? promptVersion, long started, CancellationToken cancellationToken)
        {
        try
        {
            await auditRepository.AddItemAsync(new ScriptStudioAuditEvent
            {
                Id = Guid.NewGuid().ToString("N"), UserId = userId, ChannelId = channelId, Operation = operation,
                Success = success, DeniedQuota = deniedQuota, QuotaBefore = quota.UsedBefore, QuotaAfter = quota.UsedAfter,
                DurationMilliseconds = (long)(Stopwatch.GetElapsedTime(started).TotalMilliseconds), CorrelationId = correlationId,
                PromptVersion = promptVersion, Deployment = azureConfig.OpenAi.DeploymentName,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(60), CreationDateTime = DateTime.UtcNow
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Script Studio audit write failed for user {UserId}, channel {ChannelId}, operation {Operation}", userId, channelId, operation);
        }
    }

    private static void ValidateFormat(string format)
    {
        if (!string.Equals(format?.Trim(), "plain", StringComparison.OrdinalIgnoreCase) && !string.Equals(format?.Trim(), "markdown", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Format must be plain or markdown.");
    }
}