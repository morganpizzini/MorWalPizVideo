using System.Text.Json;

namespace MorWalPiz.Contracts.Contracts;

public sealed record VideoImporterChannelSettingsRequest(
    int SchemaVersion,
    JsonElement Payload);

public sealed record VideoImporterChannelSettingsResponse(
    string ChannelId,
    int SchemaVersion,
    JsonElement Payload,
    DateTime UpdatedAtUtc);
