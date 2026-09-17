using System.Security.Cryptography;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Azure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Models.Configuration;

namespace MorWalPizVideo.Domain;

public sealed record SocialAssetBlobUpload(
    string StorageKey,
    string ContentType,
    long Size,
    string Sha256);

public interface ISocialAssetBlobService
{
    Task UploadAsync(string storageKey, Stream content, string contentType, long size, string sha256, CancellationToken cancellationToken = default);
    Task<Uri> CreateReadSasAsync(string storageKey, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed class SocialAssetBlobService(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options,
    ILogger<SocialAssetBlobService> logger) : ISocialAssetBlobService
{
    private readonly BlobStorageOptions settings = options.Value;

    public async Task UploadAsync(string storageKey, Stream content, string contentType, long size, string sha256, CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(settings.SocialAssetContainerName);
        var blob = container.GetBlobClient(storageKey);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            Metadata = new Dictionary<string, string>
            {
                ["sha256"] = sha256,
                ["sizebytes"] = size.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }
        }, cancellationToken);

        logger.LogInformation("Uploaded social asset {AssetId} with size {Size} and checksum {Checksum}",
            storageKey[..Math.Min(storageKey.Length, 32)], size, sha256);
    }

    public async Task<Uri> CreateReadSasAsync(string storageKey, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var blob = blobServiceClient.GetBlobContainerClient(settings.SocialAssetContainerName).GetBlobClient(storageKey);
        if (settings.PreferManagedIdentity)
        {
            if (string.IsNullOrWhiteSpace(settings.StorageAccountName))
            {
                throw new InvalidOperationException("BlobStorage:StorageAccountName is required to create a User Delegation SAS when managed identity is preferred.");
            }

            var startsOn = DateTimeOffset.UtcNow.AddMinutes(-5);
            var delegationKey = await blobServiceClient.GetUserDelegationKeyAsync(startsOn, expiresAt, cancellationToken);
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = settings.SocialAssetContainerName,
                BlobName = storageKey,
                Resource = "b",
                StartsOn = startsOn,
                ExpiresOn = expiresAt
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var uriBuilder = new BlobUriBuilder(blob.Uri)
            {
                Sas = sasBuilder.ToSasQueryParameters(delegationKey.Value, settings.StorageAccountName)
            };
            return uriBuilder.ToUri();
        }

        if (string.IsNullOrWhiteSpace(settings.StorageAccountName) || string.IsNullOrWhiteSpace(settings.StorageAccountKey))
        {
            throw new InvalidOperationException("BlobStorage:StorageAccountName and BlobStorage:StorageAccountKey are required to sign social asset SAS URLs when managed identity is disabled.");
        }

        var credential = new StorageSharedKeyCredential(settings.StorageAccountName, settings.StorageAccountKey);
        var signingBlob = new BlobClient(blob.Uri, credential);
        return signingBlob.GenerateSasUri(BlobSasPermissions.Read, expiresAt);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
        await blobServiceClient.GetBlobContainerClient(settings.SocialAssetContainerName)
            .DeleteBlobIfExistsAsync(storageKey, DeleteSnapshotsOption.IncludeSnapshots, conditions: null, cancellationToken);
}

public sealed class SocialAssetBlobServiceMock(IOptions<BlobStorageOptions> options) : ISocialAssetBlobService
{
    private sealed record StoredAsset(byte[] Content, string ContentType, long Size, string Sha256);
    private readonly Dictionary<string, StoredAsset> assets = new(StringComparer.Ordinal);
    private readonly object sync = new();
    private readonly BlobStorageOptions settings = options.Value;

    public Task UploadAsync(string storageKey, Stream content, string contentType, long size, string sha256, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var memory = new MemoryStream();
        content.CopyTo(memory);
        lock (sync)
            assets[storageKey] = new StoredAsset(memory.ToArray(), contentType, size, sha256);
        return Task.CompletedTask;
    }

    public Task<Uri> CreateReadSasAsync(string storageKey, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<Uri>(
            new($"https://mock-social-assets.example/{Uri.EscapeDataString(storageKey)}?sv=mock&sp=r&se={Uri.EscapeDataString(expiresAt.ToString("O"))}"));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        lock (sync)
            assets.Remove(storageKey);
        return Task.CompletedTask;
    }
}
