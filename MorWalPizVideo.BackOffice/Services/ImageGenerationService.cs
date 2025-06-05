using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;
using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.BackOffice.Services;

public class ImageGenerationService : IImageGenerationService
{
    private readonly IBlobService _blobService;
    private const string TextImagesFolderName = "generated-text-images";

    public ImageGenerationService(IBlobService blobService)
    {
        _blobService = blobService;
    }

    public async Task<string> GenerateCreatorImageAsync(string creatorName, string fontStyle = "Arial", int fontSize = 48, string textColor = "#FFFFFF", string outlineColor = "#000000", int outlineThickness = 2)
    {
        // Validazione input
        if (string.IsNullOrWhiteSpace(creatorName) ||
            string.IsNullOrWhiteSpace(fontStyle) ||
            fontSize <= 0)
        {
            throw new ArgumentException("CreatorName, FontStyle are required and FontSize must be greater than 0");
        }

        // Parse dei colori
        Color parsedTextColor;
        Color parsedOutlineColor;
        try
        {
            parsedTextColor = Color.ParseHex(textColor);
            parsedOutlineColor = Color.ParseHex(outlineColor);
        }
        catch
        {
            throw new ArgumentException("Invalid color format. Use hexadecimal format (e.g., #FFFFFF)");
        }

        // Genera un nome univoco per l'immagine basato sui parametri
        var uniqueImageName = GenerateUniqueImageName(creatorName, fontStyle, fontSize, textColor, outlineColor, outlineThickness);
        var blobPath = $"{TextImagesFolderName}/{uniqueImageName}";

        // Cerca l'immagine nel blob storage
        var existingImageStream = await _blobService.DownloadImageAsync(blobPath, false);

        if (existingImageStream != null)
        {
            // L'immagine esiste già, restituisci il nome
            existingImageStream.Dispose();
            return uniqueImageName;
        }

        // Path del font - non aggiunge .ttf se già presente
        var fontFileName = fontStyle.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) 
            ? fontStyle 
            : $"{fontStyle}.ttf";
        var fontPath = Path.Combine(Directory.GetCurrentDirectory(), "fonts", fontFileName);

        if (!File.Exists(fontPath))
        {
            throw new FileNotFoundException($"Font file not found: {fontStyle}.ttf");
        }

        // Carica il font
        var fontCollection = new FontCollection();
        var fontFamily = fontCollection.Add(fontPath);
        var font = fontFamily.CreateFont(fontSize, FontStyle.Regular);

        const int margin = 15;

        // Misura il testo per calcolare le dimensioni dell'immagine
        var textSize = TextMeasurer.MeasureSize(creatorName, new TextOptions(font));

        // Calcola le dimensioni dell'immagine (aggiungi spazio per drop shadow e outline)
        var outlineOffset = outlineThickness;
        var shadowOffset = 3; // Offset per la drop shadow
        var maxWidth = (int)textSize.Width;
        var totalHeight = (int)textSize.Height;

        var imageWidth = maxWidth + (margin * 2) + outlineOffset * 2 + shadowOffset;
        var imageHeight = totalHeight + (margin * 2) + outlineOffset * 2 + shadowOffset;

        // Crea l'immagine con sfondo trasparente
        using var image = new Image<Rgba32>(imageWidth, imageHeight);
        
        // Coordinate per il testo (compensando per outline e shadow)
        var textStartX = margin + outlineOffset;
        var textY = margin + outlineOffset;

        // Disegna drop shadow
        var shadowColor = Color.FromRgba(0, 0, 0, 100); // Nero semi-trasparente
        image.Mutate(x => x.DrawText(creatorName, font, shadowColor, new PointF(textStartX + shadowOffset, textY + shadowOffset)));

        // Disegna la traccia (outline) solo se il colore è diverso dal testo
        if (outlineThickness > 0 && !parsedTextColor.Equals(parsedOutlineColor))
        {
            // Disegna la traccia (outline) - metodo simulato
            for (int dx = -outlineThickness; dx <= outlineThickness; dx++)
            {
                for (int dy = -outlineThickness; dy <= outlineThickness; dy++)
                {
                    if (dx != 0 || dy != 0) // Non disegnare al centro
                    {
                        image.Mutate(x => x.DrawText(creatorName, font, parsedOutlineColor, 
                            new PointF(textStartX + dx, textY + dy)));
                    }
                }
            }
        }

        // Disegna il testo principale
        image.Mutate(x => x.DrawText(creatorName, font, parsedTextColor, new PointF(textStartX, textY)));

        // Converte l'immagine in byte array 
        using var memoryStream = new MemoryStream();
        await image.SaveAsPngAsync(memoryStream);
        var imageBytes = memoryStream.ToArray();

        // Salva nel blob storage
        using var uploadStream = new MemoryStream(imageBytes);
        await _blobService.UploadImagesAsync(blobPath, uploadStream, false);

        return uniqueImageName;
    }

    public async Task<Stream?> GetExistingImageAsync(string imageName)
    {
        var blobPath = $"{TextImagesFolderName}/{imageName}";
        return await _blobService.DownloadImageAsync(blobPath, false);
    }

    private string GenerateUniqueImageName(string creatorName, string fontStyle, int fontSize, string textColor, string outlineColor, int outlineThickness)
    {
        // Crea una stringa con tutti i parametri per generare un hash univoco
        var inputString = $"{creatorName}_{fontStyle}_{fontSize}_{textColor}_{outlineColor}_{outlineThickness}";
        
        // Genera hash SHA256
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        var hashString = Convert.ToHexString(hashBytes).ToLower();
        
        // Restituisci il nome file con estensione
        return $"{hashString}.png";
    }
}
