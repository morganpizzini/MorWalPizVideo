using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MorWalPizVideo.Models.Configuration;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Security.Cryptography;

namespace MorWalPizVideo.Domain
{
    public enum BlobDownloadStatus
    {
        Success,
        NotFound,
        Forbidden,
        Unavailable,
        ChecksumMismatch
    }

    public sealed record BlobDownloadResult(
        BlobDownloadStatus Status,
        Stream? Content = null,
        string? ContentType = null,
        string? ETag = null,
        IReadOnlyDictionary<string, string>? Metadata = null)
    {
        public bool IsSuccess => Status == BlobDownloadStatus.Success;
    }

    public interface IBlobService
    {
        public Task<List<string>> GetImagesInFolderAsync(string folderName, CancellationToken cancellationToken = default);
        public Task UploadImagesAsync(string filePath, MemoryStream stream, bool loadInMatchFolder = false, CancellationToken cancellationToken = default);
        public Task UploadImageAsync(string filePath, MemoryStream stream, string containerName, CancellationToken cancellationToken = default);
        public Task<Stream?> DownloadImageAsync(string filePath, bool loadInMatchFolder = false, CancellationToken cancellationToken = default);
        public Task<BlobDownloadResult> DownloadWithMetadataAsync(string filePath, bool loadInMatchFolder = false, CancellationToken cancellationToken = default);
    }
    public class BlobServiceMock : IBlobService
    {
        public Task<List<string>> GetImagesInFolderAsync(string folderName, CancellationToken cancellationToken = default)
            =>
            Task.FromResult(new List<string> { "https://placehold.co/1920x1080", "https://placehold.co/1920x1080", "https://placehold.co/1920x1080" });

        public Task UploadImagesAsync(string filePath, MemoryStream stream, bool loadInMatchFolder = false, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UploadImageAsync(string filePath, MemoryStream stream, string containerName, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Stream?> DownloadImageAsync(string filePath, bool loadInMatchFolder = false, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);

        public Task<BlobDownloadResult> DownloadWithMetadataAsync(string filePath, bool loadInMatchFolder = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BlobDownloadResult(BlobDownloadStatus.NotFound));
    }
    public class BlobService : IBlobService
    {
        private readonly BlobStorageOptions _options;
        private readonly BlobServiceClient _serviceClient;
        private readonly ILogger<BlobService> _logger;

        public BlobService(
            IOptions<BlobStorageOptions> options,
            BlobServiceClient serviceClient,
            ILogger<BlobService> logger)
        {
            _options = options.Value;
            _serviceClient = serviceClient;
            _logger = logger;
        }

        public async Task<List<string>> GetImagesInFolderAsync(string folderName, CancellationToken cancellationToken = default)
        {
            var blobContainerClient = _serviceClient.GetBlobContainerClient(_options.ContainerName);
            var images = new List<string>();
            await foreach (var blobItem in blobContainerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix: folderName, cancellationToken: cancellationToken))
            {
                if (blobItem.Properties.ContentType?.StartsWith("image/") == true)
                {
                    var blobClient = blobContainerClient.GetBlobClient(blobItem.Name);
                    images.Add(blobClient.Uri.ToString());
                }
            }
            return images;
        }
        public Task UploadImagesAsync(string filePath, MemoryStream stream, bool loadInMatchFolder = false, CancellationToken cancellationToken = default)
        {
            var containerName = loadInMatchFolder ? _options.ContainerName : _options.UploadContainerName;
            return UploadAsync(filePath, stream, containerName, cancellationToken);
        }

        public Task UploadImageAsync(string filePath, MemoryStream stream, string containerName, CancellationToken cancellationToken = default)
        {
            return UploadAsync(filePath, stream, containerName, cancellationToken);
        }

        public async Task<Stream?> DownloadImageAsync(string filePath, bool loadInMatchFolder = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var containerName = loadInMatchFolder ? _options.ContainerName : _options.UploadContainerName;
                var blobClient = _serviceClient.GetBlobContainerClient(containerName).GetBlobClient(filePath);
                var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
                return response.Value.Content;
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                _logger.LogInformation(
                    "Blob not found in container {ContainerName} at path {BlobPath}",
                    loadInMatchFolder ? _options.ContainerName : _options.UploadContainerName,
                    filePath);
                return null;
            }
        }

        public async Task<BlobDownloadResult> DownloadWithMetadataAsync(
            string filePath,
            bool loadInMatchFolder = false,
            CancellationToken cancellationToken = default)
        {
            var containerName = loadInMatchFolder ? _options.ContainerName : _options.UploadContainerName;
            try
            {
                var blobClient = _serviceClient.GetBlobContainerClient(containerName).GetBlobClient(filePath);
                var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
                await using var source = response.Value.Content;
                var content = new MemoryStream();
                await source.CopyToAsync(content, cancellationToken);
                content.Position = 0;

                var metadata = new Dictionary<string, string>(response.Value.Details.Metadata, StringComparer.OrdinalIgnoreCase);
                if (metadata.TryGetValue("sha256", out var expectedChecksum) &&
                    !VerifyChecksum(content, expectedChecksum))
                {
                    content.Dispose();
                    _logger.LogError(
                        "Blob checksum verification failed for container {ContainerName} at path {BlobPath}",
                        containerName,
                        filePath);
                    return new BlobDownloadResult(BlobDownloadStatus.ChecksumMismatch, Metadata: metadata);
                }

                return new BlobDownloadResult(
                    BlobDownloadStatus.Success,
                    content,
                    response.Value.Details.ContentType,
                    response.Value.Details.ETag.ToString(),
                    metadata);
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                return new BlobDownloadResult(BlobDownloadStatus.NotFound);
            }
            catch (RequestFailedException exception) when (exception.Status is 401 or 403)
            {
                _logger.LogWarning(
                    "Blob access was denied for container {ContainerName} at path {BlobPath}",
                    containerName,
                    filePath);
                return new BlobDownloadResult(BlobDownloadStatus.Forbidden);
            }
            catch (RequestFailedException exception)
            {
                _logger.LogError(
                    exception,
                    "Blob service request failed for container {ContainerName} at path {BlobPath}",
                    containerName,
                    filePath);
                return new BlobDownloadResult(BlobDownloadStatus.Unavailable);
            }
        }

        private async Task UploadAsync(
            string filePath,
            MemoryStream stream,
            string containerName,
            CancellationToken cancellationToken)
        {
            var blobClient = _serviceClient.GetBlobContainerClient(containerName).GetBlobClient(filePath);
            var checksum = Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
            stream.Position = 0;

            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = GetContentType(filePath) },
                Metadata = new Dictionary<string, string>
                {
                    ["sha256"] = checksum,
                    ["sizebytes"] = stream.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["uploadedatutc"] = DateTimeOffset.UtcNow.ToString("O")
                }
            }, cancellationToken);

            _logger.LogInformation(
                "Uploaded blob to container {ContainerName} at path {BlobPath} with SHA-256 {Checksum}",
                containerName,
                filePath,
                checksum);
        }

        public static string GetContentType(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".json" => "application/json",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };

        public static bool VerifyChecksum(Stream content, string expectedChecksum)
        {
            var originalPosition = content.CanSeek ? content.Position : 0;
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
            if (content.CanSeek)
            {
                content.Position = originalPosition;
            }

            return string.Equals(checksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
        }

    }

    public static class BlobStorageClientFactory
    {
        public static BlobServiceClient Create(BlobStorageOptions options, TokenCredential credential)
        {
            if (options.PreferManagedIdentity || string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint))
                {
                    throw new InvalidOperationException(
                        "BlobStorage:Endpoint is required when managed identity is preferred or no connection string fallback is configured.");
                }

                return new BlobServiceClient(endpoint, credential);
            }

            return new BlobServiceClient(options.ConnectionString);
        }

        public static bool IsConfigured(BlobStorageOptions options) =>
            !string.IsNullOrWhiteSpace(options.Endpoint) ||
            !string.IsNullOrWhiteSpace(options.ConnectionString);
    }

    public sealed class BlobStorageOptionsValidator : IValidateOptions<BlobStorageOptions>
    {
        private readonly bool _requirePrivateContainers;

        public BlobStorageOptionsValidator(bool requirePrivateContainers = false)
        {
            _requirePrivateContainers = requirePrivateContainers;
        }

        public ValidateOptionsResult Validate(string? name, BlobStorageOptions options)
        {
            var failures = new List<string>();
            if (string.IsNullOrWhiteSpace(options.Endpoint) && string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                failures.Add("BlobStorage requires Endpoint or ConnectionString.");
            }

            if ((options.PreferManagedIdentity || string.IsNullOrWhiteSpace(options.ConnectionString)) &&
                (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps))
            {
                failures.Add("BlobStorage:Endpoint must be an absolute HTTPS URI when managed identity is used.");
            }

            if (string.IsNullOrWhiteSpace(options.ContainerName))
            {
                failures.Add("BlobStorage:ContainerName is required.");
            }

            if (string.IsNullOrWhiteSpace(options.SponsorContainerName))
            {
                failures.Add("BlobStorage:SponsorContainerName is required.");
            }

            if (string.IsNullOrWhiteSpace(options.PageContainerName))
            {
                failures.Add("BlobStorage:PageContainerName is required.");
            }

            if (_requirePrivateContainers && string.IsNullOrWhiteSpace(options.UploadContainerName))
            {
                failures.Add("BlobStorage:UploadContainerName is required by BackOffice.");
            }

            if (_requirePrivateContainers && string.IsNullOrWhiteSpace(options.RecoveryContainerName))
            {
                failures.Add("BlobStorage:RecoveryContainerName is required by BackOffice operations.");
            }

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }
    }

    public sealed class BlobStorageHealthCheck : IHealthCheck
    {
        private readonly BlobServiceClient _serviceClient;
        private readonly BlobStorageOptions _options;

        public BlobStorageHealthCheck(
            BlobServiceClient serviceClient,
            IOptions<BlobStorageOptions> options)
        {
            _serviceClient = serviceClient;
            _options = options.Value;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _serviceClient
                    .GetBlobContainerClient(_options.ContainerName)
                    .GetPropertiesAsync(cancellationToken: cancellationToken);
                return HealthCheckResult.Healthy(
                    "Blob service is reachable.",
                    new Dictionary<string, object>
                    {
                        ["endpoint"] = _serviceClient.Uri.GetLeftPart(UriPartial.Authority),
                        ["authenticationMode"] = _options.PreferManagedIdentity || string.IsNullOrWhiteSpace(_options.ConnectionString)
                            ? "managed-identity"
                            : "connection-string"
                    });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return HealthCheckResult.Unhealthy(
                    "Blob service is unavailable.",
                    data: new Dictionary<string, object>
                    {
                        ["endpoint"] = _serviceClient.Uri.GetLeftPart(UriPartial.Authority),
                        ["failureType"] = exception.GetType().Name
                    });
            }
        }
    }
}
