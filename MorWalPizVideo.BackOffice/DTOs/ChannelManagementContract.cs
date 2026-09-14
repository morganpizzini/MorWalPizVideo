using MorWalPiz.Contracts.Contracts;

namespace MorWalPizVideo.BackOffice.DTOs;

public sealed class ChannelManagementContract : ChannelContract
{
    public SocialPublishingManagementContract SocialPublishing { get; set; } = new();
}

public sealed class SocialPublishingManagementContract
{
    public SocialPublishingProviderManagementContract Telegram { get; set; } = new();
    public SocialPublishingProviderManagementContract Discord { get; set; } = new();
    public SocialPublishingProviderManagementContract Facebook { get; set; } = new();
}

public sealed class SocialPublishingProviderManagementContract
{
    public string DestinationId { get; set; } = string.Empty;
    public bool CredentialConfigured { get; set; }
}