using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.DTOs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;
using MorWalPizVideo.Domain;
using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.BackOffice.Controllers;

public class UtilityController : ApplicationController
{
    private readonly IBlobService _blobService;
    private const string TextImagesFolderName = "generated-text-images";

    public UtilityController(IBlobService blobService)
    {
        _blobService = blobService;
    }

    [HttpPost("generate-text-image")]
    public async Task<IActionResult> GenerateTextImage([FromBody] GenerateTextImageRequest request)
    {
        try
        {
            // Validazione input
            if (string.IsNullOrWhiteSpace(request.Nome) ||
                string.IsNullOrWhiteSpace(request.Cognome) ||
                string.IsNullOrWhiteSpace(request.FontStyle) ||
                request.FontSize <= 0)
            {
                return BadRequest("Tutti i parametri sono obbligatori e FontSize deve essere maggiore di 0");
            }

            // Parse dei colori
            Color textColor;
            Color outlineColor;
            try
            {
                textColor = Color.ParseHex(request.TextColor);
                outlineColor = Color.ParseHex(request.OutlineColor);
            }
            catch
            {
                return BadRequest("Formato colore non valido. Utilizzare formato esadecimale (es: #FFFFFF)");
            }

            // Genera un nome univoco per l'immagine basato sui parametri
            var uniqueImageName = GenerateUniqueImageName(request);
            var blobPath = $"{TextImagesFolderName}/{uniqueImageName}";

            // Cerca l'immagine nel blob storage
            var existingImageStream = await _blobService.DownloadImageAsync(blobPath, false);

            if (existingImageStream != null)
            {
                // L'immagine esiste già, restituiscila come stream
                return File(existingImageStream, "image/png", $"{request.Nome}_{request.Cognome}.png");
            }

            // Path del font - non aggiunge .ttf se già presente
            var fontFileName = request.FontStyle.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) 
                ? request.FontStyle 
                : $"{request.FontStyle}.ttf";
            var fontPath = Path.Combine(Directory.GetCurrentDirectory(), "fonts", fontFileName);

            if (!System.IO.File.Exists(fontPath))
            {
                return BadRequest($"Font file non trovato: {request.FontStyle}.ttf");
            }

            // Carica il font
            var fontCollection = new FontCollection();
            var fontFamily = fontCollection.Add(fontPath);
            var font = fontFamily.CreateFont(request.FontSize, FontStyle.Regular);

            const int margin = 15;

            // Misura il testo per calcolare le dimensioni dell'immagine
            var nomeSize = TextMeasurer.MeasureSize(request.Nome, new TextOptions(font));
            var cognomeSize = TextMeasurer.MeasureSize(request.Cognome, new TextOptions(font));

            // Calcola le dimensioni dell'immagine (aggiungi spazio per drop shadow e outline)
            var outlineOffset = request.OutlineThickness;
            var shadowOffset = 3; // Offset per la drop shadow
            var maxWidth = (int)Math.Max(nomeSize.Width, cognomeSize.Width);
            var totalHeight = (int)(nomeSize.Height + cognomeSize.Height);

            var imageWidth = maxWidth + (margin * 2) + outlineOffset * 2 + shadowOffset;
            var imageHeight = totalHeight + (margin * 2) + outlineOffset * 2 + shadowOffset;

            // Crea l'immagine con sfondo trasparente
            using var image = new Image<Rgba32>(imageWidth, imageHeight);
            
            // Sfondo trasparente (non serve riempire, Rgba32 è già trasparente di default)

            // Coordinate per il testo (compensando per outline e shadow)
            var textStartX = margin + outlineOffset;
            var nomeY = margin + outlineOffset;
            var cognomeY = margin + outlineOffset + nomeSize.Height;

            // Disegna drop shadow per il nome
            var shadowColor = Color.FromRgba(0, 0, 0, 100); // Nero semi-trasparente
            image.Mutate(x => x.DrawText(request.Nome, font, shadowColor, new PointF(textStartX + shadowOffset, nomeY + shadowOffset)));

            // Disegna drop shadow per il cognome
            image.Mutate(x => x.DrawText(request.Cognome, font, shadowColor, new PointF(textStartX + shadowOffset, cognomeY + shadowOffset)));

            // Disegna la traccia (outline) solo se il colore è diverso dal testo
            if (request.OutlineThickness > 0 && !textColor.Equals(outlineColor))
            {
                // Disegna la traccia (outline) per il nome - metodo simulato
                for (int dx = -request.OutlineThickness; dx <= request.OutlineThickness; dx++)
                {
                    for (int dy = -request.OutlineThickness; dy <= request.OutlineThickness; dy++)
                    {
                        if (dx != 0 || dy != 0) // Non disegnare al centro
                        {
                            image.Mutate(x => x.DrawText(request.Nome, font, outlineColor, 
                                new PointF(textStartX + dx, nomeY + dy)));
                        }
                    }
                }

                // Disegna la traccia (outline) per il cognome - metodo simulato
                for (int dx = -request.OutlineThickness; dx <= request.OutlineThickness; dx++)
                {
                    for (int dy = -request.OutlineThickness; dy <= request.OutlineThickness; dy++)
                    {
                        if (dx != 0 || dy != 0) // Non disegnare al centro
                        {
                            image.Mutate(x => x.DrawText(request.Cognome, font, outlineColor, 
                                new PointF(textStartX + dx, cognomeY + dy)));
                        }
                    }
                }
            }

            // Disegna il testo principale (nome)
            image.Mutate(x => x.DrawText(request.Nome, font, textColor, new PointF(textStartX, nomeY)));

            // Disegna il testo principale (cognome)
            image.Mutate(x => x.DrawText(request.Cognome, font, textColor, new PointF(textStartX, cognomeY)));

            // Converte l'immagine in byte array 
            using var memoryStream = new MemoryStream();
            await image.SaveAsPngAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            // Salva nel blob storage (crea una copia del stream per l'upload)
            using var uploadStream = new MemoryStream(imageBytes);
            await _blobService.UploadImagesAsync(blobPath, uploadStream, false);

            // Restituisce l'immagine generata come stream
            return File(imageBytes, "image/png", $"{request.Nome}_{request.Cognome}.png");
        }
        catch (Exception ex)
        {
            return BadRequest($"Errore durante la generazione dell'immagine: {ex.Message}");
        }
    }

    private string GenerateUniqueImageName(GenerateTextImageRequest request)
    {
        // Crea una stringa con tutti i parametri per generare un hash univoco
        var inputString = $"{request.Nome}_{request.Cognome}_{request.FontStyle}_{request.FontSize}_{request.TextColor}_{request.OutlineColor}_{request.OutlineThickness}";
        
        // Genera hash SHA256
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        var hashString = Convert.ToHexString(hashBytes).ToLower();
        
        // Restituisci il nome file con estensione
        return $"{hashString}.png";
    }

    [HttpGet("fonts")]
    public IActionResult GetFonts()
    {
        try
        {
            var fontsPath = Path.Combine(Directory.GetCurrentDirectory(), "fonts");

            if (!Directory.Exists(fontsPath))
            {
                return BadRequest("Cartella fonts non trovata");
            }

            var response = new FontListResponse();

            // Ottieni tutte le sottocartelle nella cartella fonts
            var directories = Directory.GetDirectories(fontsPath);

            foreach (var directory in directories)
            {
                var categoryName = Path.GetFileName(directory);
                var fontFiles = Directory.GetFiles(directory, "*.ttf")
                    .Select(file => Path.GetFileName(file))
                    .ToList();

                if (fontFiles.Any())
                {
                    response.Categories.Add(new FontCategoryResponse
                    {
                        CategoryName = categoryName,
                        FontFiles = fontFiles
                    });
                }
            }

            // Controlla anche se ci sono file .ttf direttamente nella cartella fonts (senza categoria)
            var rootFontFiles = Directory.GetFiles(fontsPath, "*.ttf")
                .Select(file => Path.GetFileName(file))
                .ToList();

            if (rootFontFiles.Any())
            {
                response.Categories.Add(new FontCategoryResponse
                {
                    CategoryName = "General",
                    FontFiles = rootFontFiles
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Errore durante il recupero dei font: {ex.Message}");
        }
    }
}
