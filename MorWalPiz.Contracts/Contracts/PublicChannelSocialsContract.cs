namespace MorWalPiz.Contracts.Contracts;

public sealed record PublicChannelSocialLinkContract(string Provider, string Url);

public sealed record PublicChannelSocialsContract(IReadOnlyList<PublicChannelSocialLinkContract> Links);
