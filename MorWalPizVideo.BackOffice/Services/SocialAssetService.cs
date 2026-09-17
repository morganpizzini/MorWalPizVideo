using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

public sealed record SocialAssetUploadResult(SocialAssetContract? Contract, string? ConflictReason = null)
{
    public bool IsConflict => ConflictReason is not null;
}

public sealed class SocialAssetService(
    ISocialAssetRepository repository,
    ISocialAssetBlobService blobService,
    IOptions<BlobStorageOptions> options,
    TimeProvider timeProvider)
{
    private static readonly IReadOnlyDictionary<string, string> SupportedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp",
        [".mp4"] = "video/mp4", [".mov"] = "video/quicktime"
    };

    public async Task<SocialAssetUploadResult> UploadAsync(
        string channelId,
        string idempotencyKey,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        idempotencyKey = idempotencyKey.Trim();
        if (file.Length <= 0)
            throw new SocialAssetValidationException("File cannot be empty.");
        if (file.Length > settings.SocialAssetMaxBytes)
            throw new SocialAssetValidationException("File exceeds the configured social asset size limit.");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 200)
            throw new SocialAssetValidationException("Idempotency-Key is required and must be at most 200 characters.");

        var extension = Path.GetExtension(file.FileName);
        if (!SupportedTypes.TryGetValue(extension, out var expectedContentType))
            throw new SocialAssetValidationException("Only supported image and video extensions are accepted.");

        var tempPath = Path.Combine(Path.GetTempPath(), $"social-asset-{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var source = file.OpenReadStream())
            await using (var target = File.Create(tempPath))
                await source.CopyToAsync(target, cancellationToken);

            var size = new FileInfo(tempPath).Length;
            if (size <= 0 || size > settings.SocialAssetMaxBytes)
                throw new SocialAssetValidationException("File size is outside the configured limits.");

            var contentType = await DetectContentTypeAsync(tempPath, cancellationToken);
            if (contentType is null || !string.Equals(contentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
                throw new SocialAssetValidationException("The file content does not match its declared extension.");

            await using var hashStream = File.OpenRead(tempPath);
            var checksum = Convert.ToHexString(await SHA256.HashDataAsync(hashStream, cancellationToken)).ToLowerInvariant();
            var existing = await repository.GetByIdempotencyKeyAsync(channelId, idempotencyKey);
            if (existing is not null)
            {
                if (!string.Equals(existing.Sha256, checksum, StringComparison.OrdinalIgnoreCase) || existing.Size != size)
                    return new(null, "The idempotency key was already used for different content.");
                return new(ToContract(existing, await blobService.CreateReadSasAsync(existing.StorageKey, existing.ExpiresAt, cancellationToken)));
            }

            var now = timeProvider.GetUtcNow();
            var expiresAt = now.AddMinutes(Math.Clamp(settings.SocialAssetSasTtlMinutes, 1, settings.SocialAssetMaxSasTtlMinutes));
            var asset = new SocialAsset
            {
                Id = Guid.NewGuid().ToString("N"),
                AssetId = Guid.NewGuid().ToString("N"),
                ChannelId = channelId,
                StorageKey = $"social/{channelId}/{Guid.NewGuid():N}",
                ContentType = contentType,
                Size = size,
                Sha256 = checksum,
                MediaKind = contentType.StartsWith("video/", StringComparison.Ordinal) ? "video" : "image",
                CreatedAt = now,
                ExpiresAt = expiresAt,
                Status = SocialAssetStatus.Available,
                IdempotencyKey = idempotencyKey
            };

            await using (var content = File.OpenRead(tempPath))
                await blobService.UploadAsync(asset.StorageKey, content, asset.ContentType, asset.Size, asset.Sha256, cancellationToken);
            await repository.AddItemAsync(asset);
            return new(ToContract(asset, await blobService.CreateReadSasAsync(asset.StorageKey, asset.ExpiresAt, cancellationToken)));
        }
        finally
        {
            try { File.Delete(tempPath); } catch { }
        }
    }

    private static SocialAssetContract ToContract(SocialAsset asset, Uri readUrl) =>
        new(asset.AssetId, asset.MediaKind, asset.ContentType, asset.Size, asset.Sha256, readUrl.ToString(), asset.ExpiresAt);

    private static async Task<string?> DetectContentTypeAsync(string path, CancellationToken cancellationToken)
    {
        var header = new byte[16];
        await using var stream = File.OpenRead(path);
        var read = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return "image/jpeg";
        if (read >= 8 && header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (read >= 12 && header[..4].SequenceEqual("RIFF"u8.ToArray()) && header[8..12].SequenceEqual("WEBP"u8.ToArray())) return "image/webp";
        if (read >= 12 && header[4..8].SequenceEqual("ftyp"u8.ToArray()))
            return header[8..12].SequenceEqual("qt  "u8.ToArray()) ? "video/quicktime" : "video/mp4";
        return null;
    }
}

public sealed class SocialAssetValidationException(string message) : Exception(message);
