using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using MorWalPiz.VideoImporter.Models;

namespace MorWalPiz.VideoImporter.Services;

public interface IImageGenerationService
{
    Task<IReadOnlyList<GeneratedImage>> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<GeneratedImage>> EditAsync(ImageEditRequest request, CancellationToken cancellationToken);
}

public interface IImageApiKeyProvider
{
    Task<string> GetApiKeyAsync(CancellationToken cancellationToken);
}

public sealed class ConfigurationImageApiKeyProvider(IConfiguration configuration, ImageGenerationOptions options)
    : IImageApiKeyProvider
{
    public async Task<string> GetApiKeyAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var keyVaultUrl = configuration["KeyVaultUrl"];
        if (!Uri.TryCreate(keyVaultUrl, UriKind.Absolute, out var vaultUri))
            throw new ImageProviderException("Key Vault non è configurato per la chiave API immagini.");
        try
        {
            var client = new SecretClient(vaultUri, new DefaultAzureCredential());
            var response = await client.GetSecretAsync(options.ApiKeySecretName, cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(response.Value.Value))
                throw new ImageProviderException($"La chiave API immagine '{options.ApiKeySecretName}' è vuota in Azure Key Vault.");
            return response.Value.Value;
        }
        catch (ImageProviderException) { throw; }
        catch (Exception exception)
        {
            throw new ImageProviderException($"Impossibile recuperare la chiave API immagini da Azure Key Vault: {exception.Message}", null, exception);
        }
    }
}

public sealed class ImageGenerationService(
    IHttpClientFactory httpClientFactory,
    IImageApiKeyProvider apiKeyProvider,
    ImageGenerationOptions options,
    JsonSerializerOptions? serializerOptions = null) : IImageGenerationService
{
    private readonly JsonSerializerOptions _serializerOptions = serializerOptions ?? new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<GeneratedImage>> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken)
    {
        ValidateEndpoint();
        var payload = new
        {
            model = options.Model,
            prompt = request.Prompt,
            size = request.Size,
            quality = request.Quality,
            background = request.Background,
            output_compression = string.IsNullOrWhiteSpace(request.OutputCompression) ? null : request.OutputCompression,
            output_format = request.OutputFormat,
            n = request.Count
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload, _serializerOptions), Encoding.UTF8, "application/json");
        using var response = await SendAsync(HttpMethod.Post, content, cancellationToken);
        return await ReadImagesAsync(response, request.OutputFormat, cancellationToken);
    }

    public async Task<IReadOnlyList<GeneratedImage>> EditAsync(ImageEditRequest request, CancellationToken cancellationToken)
    {
        ValidateEndpoint();
        if (!File.Exists(request.SourceImagePath))
            throw new ImageProviderException("L'immagine sorgente non esiste.");
        if (request.MaskPath is not null && !File.Exists(request.MaskPath))
            throw new ImageProviderException("La maschera selezionata non esiste.");

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(request.Prompt), "prompt");
        using var imageStream = File.OpenRead(request.SourceImagePath);
        using var imageContent = new StreamContent(imageStream);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(GetMediaType(request.SourceImagePath));
        form.Add(imageContent, "image", Path.GetFileName(request.SourceImagePath));

        if (request.MaskPath is not null)
        {
            using var maskStream = File.OpenRead(request.MaskPath);
            using var maskContent = new StreamContent(maskStream);
            maskContent.Headers.ContentType = new MediaTypeHeaderValue(GetMediaType(request.MaskPath));
            form.Add(maskContent, "mask", Path.GetFileName(request.MaskPath));
            using var responseWithMask = await SendAsync(HttpMethod.Post, form, cancellationToken);
            return await ReadImagesAsync(responseWithMask, options.OutputFormat, cancellationToken);
        }

        using var response = await SendAsync(HttpMethod.Post, form, cancellationToken);
        return await ReadImagesAsync(response, options.OutputFormat, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, HttpContent content, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("ImageGeneration");
        using var request = new HttpRequestMessage(method, options.Endpoint) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await apiKeyProvider.GetApiKeyAsync(cancellationToken));
        try
        {
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                response.Dispose();
                throw new ImageProviderException($"Il provider immagini ha restituito HTTP {statusCode}: {TrimError(body)}", statusCode);
            }
            return response;
        }
        catch (HttpRequestException exception)
        {
            throw new ImageProviderException("Impossibile contattare il provider immagini.", null, exception);
        }
    }

    private async Task<IReadOnlyList<GeneratedImage>> ReadImagesAsync(HttpResponseMessage response, string format, CancellationToken cancellationToken)
    {
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<ImageResponse>(responseStream, _serializerOptions, cancellationToken)
            ?? throw new ImageProviderException("La risposta del provider immagini è vuota o non valida.");
        var images = new List<GeneratedImage>();
        foreach (var item in result.Data)
        {
            if (!string.IsNullOrWhiteSpace(item.Base64Json))
            {
                try { images.Add(new GeneratedImage(Convert.FromBase64String(item.Base64Json), NormalizeExtension(format), GetMediaType(format))); }
                catch (FormatException exception) { throw new ImageProviderException("Il provider ha restituito dati immagine non validi.", null, exception); }
            }
            else if (!string.IsNullOrWhiteSpace(item.Url))
            {
                using var client = httpClientFactory.CreateClient("ImageGeneration");
                var bytes = await client.GetByteArrayAsync(item.Url, cancellationToken);
                images.Add(new GeneratedImage(bytes, NormalizeExtension(format), GetMediaType(format)));
            }
        }
        if (images.Count == 0)
            throw new ImageProviderException("La risposta del provider non contiene immagini.");
        return images;
    }

    private void ValidateEndpoint()
    {
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _))
            throw new ImageProviderException("L'endpoint del provider immagini non è configurato correttamente.");
    }

    private static string TrimError(string body) => string.IsNullOrWhiteSpace(body) ? "nessun dettaglio" : body.Length > 500 ? body[..500] : body;
    private static string NormalizeExtension(string format) => format.Trim().TrimStart('.').ToLowerInvariant() switch { "jpeg" => "jpg", "webp" => "webp", _ => "png" };
    private static string GetMediaType(string pathOrFormat) => Path.GetExtension(pathOrFormat).ToLowerInvariant() switch { ".jpg" or ".jpeg" => "image/jpeg", ".webp" => "image/webp", _ => "image/png" };
}