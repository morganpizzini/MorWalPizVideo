using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.BackOffice.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Controllers;

public class UtilityController : ApplicationControllerBase
{
    private readonly IImageGenerationService _imageGenerationService;

    public UtilityController(IImageGenerationService imageGenerationService)
    {
        _imageGenerationService = imageGenerationService;
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

            // Combine Nome and Cognome for the creator name
            var creatorName = $"{request.Nome} {request.Cognome}";

            // Generate the image using the service
            var imageName = await _imageGenerationService.GenerateCreatorImageAsync(
                creatorName,
                request.FontStyle,
                request.FontSize,
                request.TextColor,
                request.OutlineColor,
                request.OutlineThickness);

            // Get the image stream
            var imageStream = await _imageGenerationService.GetExistingImageAsync(imageName);
            
            if (imageStream == null)
            {
                return BadRequest("Errore durante la generazione dell'immagine");
            }

            // Return the image
            return File(imageStream, "image/png", $"{request.Nome}_{request.Cognome}.png");
        }
        catch (Exception ex)
        {
            return BadRequest($"Errore durante la generazione dell'immagine: {ex.Message}");
        }
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
