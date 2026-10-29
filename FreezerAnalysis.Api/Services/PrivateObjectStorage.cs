using System.Security.Cryptography;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure;
using Microsoft.Extensions.Options;
using MorWalPizVideo.Domain.FreezerAnalysis;

namespace FreezerAnalysis.Api.Services;

public sealed class PrivateBlobOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "freezer-analysis-private";
    public bool UseManagedIdentity { get; set; } = true;
}

public sealed class AzurePrivateObjectStorage(BlobContainerClient container) : IFreezerPrivateObjectStorage
{
    public async Task<PrivateObjectReference> PutAsync(
        string category,
        string sha256,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken)
    {
        ValidateHash(sha256);
        var key = BuildKey(category, sha256);
        await using var stream = new MemoryStream(content.ToArray(), writable: false);
        var client = container.GetBlobClient(key);
        try
        {
            await client.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["sha256"] = sha256,
                    ["length"] = content.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                },
                Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
            }, cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            var existing = await client.DownloadContentAsync(cancellationToken);
            var existingBytes = existing.Value.Content.ToArray();
            VerifyContent(new(key, sha256, content.Length, contentType), existingBytes);
            if (!string.Equals(existing.Value.Details.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A content-addressed private object already exists with a different content type.");
        }
        return new(key, sha256, content.Length, contentType);
    }

    public async Task<byte[]> GetAsync(PrivateObjectReference reference, CancellationToken cancellationToken)
    {
        ValidateReference(reference);
        var response = await container.GetBlobClient(reference.Key).DownloadContentAsync(cancellationToken);
        var bytes = response.Value.Content.ToArray();
        VerifyContent(reference, bytes);
        return bytes;
    }

    private static string BuildKey(string category, string sha256)
    {
        if (category is not ("source" or "input" or "artifact"))
            throw new ArgumentOutOfRangeException(nameof(category));
        ValidateHash(sha256);
        return $"{category}/{sha256[..2]}/{sha256}";
    }

    private static void ValidateHash(string sha256)
    {
        if (sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))
            throw new ArgumentException("A SHA-256 hash must be 64 hexadecimal characters.", nameof(sha256));
    }

    private static void ValidateReference(PrivateObjectReference reference)
    {
        if (reference.Length < 0 ||
            reference.Key.Split('/') is not [var category, var prefix, var hash] ||
            category is not ("source" or "input" or "artifact") ||
            !string.Equals(prefix, reference.Sha256[..Math.Min(2, reference.Sha256.Length)], StringComparison.Ordinal) ||
            !string.Equals(hash, reference.Sha256, StringComparison.Ordinal) ||
            !string.Equals(reference.Key, BuildKey(category, reference.Sha256), StringComparison.Ordinal))
            throw new ArgumentException("The private object reference is invalid.", nameof(reference));
    }

    private static void VerifyContent(PrivateObjectReference reference, byte[] bytes)
    {
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if ((reference.Length > 0 && bytes.LongLength != reference.Length) ||
            !string.Equals(hash, reference.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The private object failed its stored length or SHA-256 verification.");
    }
}

public sealed class FilePrivateObjectStorage(string rootPath) : IFreezerPrivateObjectStorage
{
    public async Task<PrivateObjectReference> PutAsync(
        string category,
        string sha256,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var path = ResolvePath(category, sha256);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path))
        {
            var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
            try
            {
                await using (var stream = new FileStream(
                                 temporaryPath,
                                 FileMode.CreateNew,
                                 FileAccess.Write,
                                 FileShare.None,
                                 bufferSize: 81920,
                                 FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await stream.WriteAsync(content, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }
                File.Move(temporaryPath, path);
            }
            catch (IOException) when (File.Exists(path))
            {
                var existing = await File.ReadAllBytesAsync(path, cancellationToken);
                VerifyContent(new(ToKey(category, sha256), sha256, content.Length, contentType), existing);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
        return new(ToKey(category, sha256), sha256, content.Length, contentType);
    }

    public async Task<byte[]> GetAsync(PrivateObjectReference reference, CancellationToken cancellationToken)
    {
        var path = ResolveReferencePath(reference);
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        VerifyContent(reference, bytes);
        return bytes;
    }

    private string ResolvePath(string category, string sha256)
    {
        ValidateCategoryAndHash(category, sha256);
        return Path.Combine(rootPath, category, sha256[..2], sha256);
    }

    private static string ToKey(string category, string sha256) => $"{category}/{sha256[..2]}/{sha256}";

    private string ResolveReferencePath(PrivateObjectReference reference)
    {
        if (reference.Length < 0 ||
            reference.Key.Split('/') is not [var category, _, var hash] ||
            !string.Equals(hash, reference.Sha256, StringComparison.Ordinal))
            throw new ArgumentException("The private object reference is invalid.", nameof(reference));
        ValidateCategoryAndHash(category, reference.Sha256);
        if (!string.Equals(reference.Key, ToKey(category, reference.Sha256), StringComparison.Ordinal))
            throw new ArgumentException("The private object key is not canonical.", nameof(reference));
        return ResolvePath(category, reference.Sha256);
    }

    private static void ValidateCategoryAndHash(string category, string sha256)
    {
        if (category is not ("source" or "input" or "artifact") ||
            sha256.Length != 64 || !sha256.All(Uri.IsHexDigit) ||
            !string.Equals(sha256, sha256.ToLowerInvariant(), StringComparison.Ordinal))
            throw new ArgumentException("The private object category or SHA-256 hash is invalid.");
    }

    private static void VerifyContent(PrivateObjectReference reference, byte[] bytes)
    {
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if ((reference.Length > 0 && bytes.LongLength != reference.Length) ||
            !string.Equals(hash, reference.Sha256, StringComparison.Ordinal))
            throw new InvalidDataException("The private object failed its stored length or SHA-256 verification.");
    }
}

public static class PrivateObjectStorageRegistration
{
    public static IServiceCollection AddFreezerPrivateObjectStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment() || environment.IsEnvironment("Test"))
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MorWalPizVideo",
                "FreezerAnalysis",
                "private");
            services.AddSingleton<IFreezerPrivateObjectStorage>(new FilePrivateObjectStorage(root));
            return services;
        }

        var options = configuration.GetSection("FreezerAnalysis:PrivateBlob").Get<PrivateBlobOptions>() ?? new();
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(options.ContainerName))
            throw new InvalidOperationException("FreezerAnalysis private Blob endpoint and container must be configured over HTTPS.");

        var client = options.UseManagedIdentity
            ? new BlobServiceClient(endpoint, new DefaultAzureCredential())
            : throw new InvalidOperationException("Shared-key Blob access is not supported by FreezerAnalysis.");
        services.AddSingleton(client.GetBlobContainerClient(options.ContainerName));
        services.AddSingleton<IFreezerPrivateObjectStorage, AzurePrivateObjectStorage>();
        return services;
    }
}
