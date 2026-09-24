using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace MorWalPiz.VideoImporter.Models;

public sealed class ImageGenerationOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ApiKeySecretName { get; set; } = "ImageGenerationApiKey";
    public string OutputDirectory { get; set; } = string.Empty;
    public string OutputFormat { get; set; } = "png";
    public string Quality { get; set; } = "auto";
    public string Background { get; set; } = "auto";
    public string OutputCompression { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
    public Dictionary<string, string> SupportedSizes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static ImageGenerationOptions FromConfiguration(IConfiguration configuration)
    {
        var options = new ImageGenerationOptions
        {
            Endpoint = configuration["ImageGeneration:Endpoint"] ?? string.Empty,
            Model = configuration["ImageGeneration:Model"] ?? string.Empty,
            ApiKeySecretName = configuration["ImageGeneration:ApiKeySecretName"] ?? "ImageGenerationApiKey",
            OutputDirectory = configuration["ImageGeneration:OutputDirectory"] ?? string.Empty,
            OutputFormat = configuration["ImageGeneration:OutputFormat"] ?? "png",
            Quality = configuration["ImageGeneration:Quality"] ?? "auto",
            Background = configuration["ImageGeneration:Background"] ?? "auto",
            OutputCompression = configuration["ImageGeneration:OutputCompression"] ?? string.Empty,
            Count = configuration.GetValue("ImageGeneration:Count", 1)
        };

        foreach (var child in configuration.GetSection("ImageGeneration:SupportedSizes").GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(child.Key) && !string.IsNullOrWhiteSpace(child.Value))
                options.SupportedSizes[child.Key] = child.Value;
        }

        if (options.SupportedSizes.Count == 0)
            options.SupportedSizes["1024x1024"] = "1024x1024";

        return options;
    }
}

public sealed record ImageGenerationRequest(
    string Prompt,
    string Size,
    string Quality,
    string Background,
    string OutputCompression,
    string OutputFormat,
    int Count);

public sealed record ImageEditRequest(string Prompt, string SourceImagePath, string? MaskPath);

public sealed record GeneratedImage(byte[] Content, string Extension, string MediaType);

public sealed class ImageProviderException(string message, int? statusCode = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public int? StatusCode { get; } = statusCode;
}

internal sealed class ImageResponse
{
    [JsonPropertyName("data")]
    public List<ImageResponseItem> Data { get; set; } = [];
}

internal sealed class ImageResponseItem
{
    [JsonPropertyName("b64_json")]
    public string? Base64Json { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}