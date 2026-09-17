namespace MorWalPiz.Contracts.Contracts;

public sealed record SocialAssetContract(
    string AssetId,
    string MediaKind,
    string ContentType,
    long Size,
    string Checksum,
    string ReadUrl,
    DateTimeOffset ExpiresAt);
