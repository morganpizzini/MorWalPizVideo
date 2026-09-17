using System.ComponentModel.DataAnnotations;

namespace MorWalPiz.VideoImporter.Models;

public enum SocialProviderKind
{
    FacebookPage,
    FacebookPersonalProfile,
    InstagramBusiness,
    Threads
}

public enum SocialMediaType
{
    Image,
    Video
}

public sealed class SocialMediaDescriptor
{
    public string FilePath { get; set; } = string.Empty;
    public SocialMediaType MediaType { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? RemoteUrl { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
}

public sealed class SocialPostDraft
{
    public int TenantId { get; set; }
    public string ChannelId { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public List<SocialMediaDescriptor> Media { get; set; } = [];
    public List<SocialProviderKind> Providers { get; set; } = [];
    public string AspectRatio { get; set; } = "square";
    public DateTimeOffset? ScheduledAt { get; set; }
}

public sealed class HashtagHistory
{
    public int Id { get; set; }
    [Required]
    public string Value { get; set; } = string.Empty;
    [Required]
    public string ChannelId { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public DateTime LastUsedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SocialChannelConfiguration
{
    public int Id { get; set; }
    [Required]
    public string ChannelId { get; set; } = string.Empty;
    [Required]
    public SocialProviderKind Provider { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public int TenantId { get; set; }
}

public sealed record SocialProviderCapability(
    SocialProviderKind Provider,
    bool IsConfigured,
    bool SupportsLocalMedia,
    bool SupportsScheduling,
    IReadOnlyCollection<string> SupportedAspectRatios,
    string StatusMessage);

public sealed record SocialPublishResult(bool Succeeded, string Message);