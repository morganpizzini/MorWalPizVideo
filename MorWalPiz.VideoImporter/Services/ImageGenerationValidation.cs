using System.Text.RegularExpressions;
using System.IO;
using MorWalPiz.VideoImporter.Models;

namespace MorWalPiz.VideoImporter.Services;

public static partial class ImageGenerationValidation
{
    public static string ResolveProviderSize(ImageGenerationOptions options, string requestedSize)
    {
        var normalized = requestedSize.Trim().ToLowerInvariant();
        if (options.SupportedSizes.TryGetValue(normalized, out var providerSize))
            return providerSize;
        throw new ImageProviderException($"Il formato '{requestedSize}' non è supportato dalla configurazione del provider. Nessun ritaglio o ridimensionamento automatico verrà eseguito.");
    }

    public static void ValidatePrompt(string name, string prompt)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ImageProviderException("Il nome è obbligatorio.");
        if (string.IsNullOrWhiteSpace(prompt)) throw new ImageProviderException("Il prompt è obbligatorio.");
    }

    public static void ValidateReferenceImages(IReadOnlyCollection<string> images)
    {
        if (images.Count > 1)
            throw new ImageProviderException("L'editing supporta una sola immagine sorgente più una maschera. Le immagini di riferimento aggiuntive non sono supportate.");
    }

    public static string ValidateOutputDirectory(ImageGenerationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            throw new ImageProviderException("La directory di output non è configurata.");
        return Path.GetFullPath(options.OutputDirectory);
    }
}