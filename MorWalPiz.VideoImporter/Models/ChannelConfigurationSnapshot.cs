namespace MorWalPiz.VideoImporter.Models;

public sealed record ChannelConfigurationSnapshot
{
    public const int CurrentSchemaVersion = 1;
    public const int MaxLanguageCount = 50;
    public const int MaxScheduleCount = 100;
    public const int MaxPayloadBytes = 64 * 1024;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string ApplicationName { get; init; } = string.Empty;
    public string DefaultHashtags { get; init; } = string.Empty;
    public IReadOnlyList<ChannelLanguageSnapshot> Languages { get; init; } = [];
    public IReadOnlyList<ChannelPublishScheduleSnapshot> PublishSchedules { get; init; } = [];
}

public sealed record ChannelLanguageSnapshot(
    string Code,
    string Name,
    bool IsDefault,
    bool IsSelected,
    string? DisclaimerText);

public sealed record ChannelPublishScheduleSnapshot(
    string Name,
    int DaysOfWeek,
    TimeSpan PublishTime,
    bool IsActive);
