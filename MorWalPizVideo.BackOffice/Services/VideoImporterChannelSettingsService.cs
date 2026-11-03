using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

public sealed class VideoImporterChannelSettingsService(
    IVideoImporterChannelSettingsRepository repository,
    ILogger<VideoImporterChannelSettingsService> logger)
{
    public const int CurrentSchemaVersion = 1;
    public const int MaxPayloadBytes = 64 * 1024;

    public async Task<VideoImporterChannelSettingsResponse?> GetAsync(
        string channelId,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var saved = await repository.GetByChannelIdAsync(channelId, cancellationToken);
        if (saved is null)
        {
            logger.LogInformation(
                "Video importer channel settings read {Outcome} for channel {ChannelId} in {ElapsedMilliseconds} ms",
                "missing",
                channelId,
                stopwatch.ElapsedMilliseconds);
            return null;
        }

        using var document = JsonDocument.Parse(saved.PayloadJson);
        logger.LogInformation(
            "Video importer channel settings read {Outcome} for channel {ChannelId}, schema {SchemaVersion}, {PayloadBytes} bytes in {ElapsedMilliseconds} ms",
            "found",
            channelId,
            saved.SchemaVersion,
            Encoding.UTF8.GetByteCount(saved.PayloadJson),
            stopwatch.ElapsedMilliseconds);
        return new VideoImporterChannelSettingsResponse(
            saved.ChannelId,
            saved.SchemaVersion,
            document.RootElement.Clone(),
            saved.UpdatedAtUtc);
    }

    public async Task<VideoImporterChannelSettingsResponse> SaveAsync(
        string channelId,
        VideoImporterChannelSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var payloadJson = ValidateAndSerialize(request);
        var saved = await repository.UpsertByChannelIdAsync(new VideoImporterChannelSettings
        {
            ChannelId = channelId,
            SchemaVersion = request.SchemaVersion,
            PayloadJson = payloadJson,
            UpdatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        logger.LogInformation(
            "Video importer channel settings saved for channel {ChannelId}, schema {SchemaVersion}, {PayloadBytes} bytes in {ElapsedMilliseconds} ms",
            channelId,
            saved.SchemaVersion,
            Encoding.UTF8.GetByteCount(saved.PayloadJson),
            stopwatch.ElapsedMilliseconds);
        using var document = JsonDocument.Parse(saved.PayloadJson);
        return new VideoImporterChannelSettingsResponse(
            saved.ChannelId,
            saved.SchemaVersion,
            document.RootElement.Clone(),
            saved.UpdatedAtUtc);
    }

    private static string ValidateAndSerialize(VideoImporterChannelSettingsRequest request)
    {
        if (request.SchemaVersion != CurrentSchemaVersion)
            throw new VideoImporterChannelSettingsValidationException(
                "unsupported_schema_version",
                "The settings schema version is not supported.");

        if (request.Payload.ValueKind != JsonValueKind.Object)
            throw new VideoImporterChannelSettingsValidationException(
                "settings_payload_object_required",
                "The settings payload must be a JSON object.");

        var payloadJson = request.Payload.GetRawText();
        if (Encoding.UTF8.GetByteCount(payloadJson) > MaxPayloadBytes)
            throw new VideoImporterChannelSettingsValidationException(
                "settings_payload_too_large",
                "The settings payload exceeds the maximum supported size.",
                StatusCodes.Status413PayloadTooLarge);

        return payloadJson;
    }
}

public sealed class VideoImporterChannelSettingsValidationException(
    string code,
    string message,
    int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}
