using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
namespace MorWalPizVideo.BackOffice.Controllers;

public class ImageUploadController : ApplicationController
{
    private readonly IBlobService blobServiceClient;
    private readonly IMongoDatabase database;

    public ImageUploadController(IMongoDatabase _database, IBlobService _blobServiceClient) 
    {
        database = _database;
        blobServiceClient = _blobServiceClient;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(IFormFile image, string folderName,bool loadInMatchFolder)
    {
        if (image == null || image.Length == 0)
        {
            return BadRequest("File non valido.");
        }

        var matchCollection = database.GetCollection<Match>(DbCollections.Matches);
        var existingMatch = matchCollection.Find(x => x.Url == folderName).FirstOrDefault();

        if(existingMatch == null)
        {
            return BadRequest("Match non trovato.");
        }

        // Creazione di uno stream per l'immagine ridimensionata
        using var inputStream = image.OpenReadStream();
        using var outputStream = new MemoryStream();

        // Ridimensionamento dell'immagine a 1080p
        using (var img = await Image.LoadAsync(inputStream))
        {
            // Controlla l'orientamento e ridimensiona rispettando il rapporto d'aspetto
            if (img.Width > img.Height)
            {
                // Landscape
                img.Mutate(x => x.Resize(1920, 1080));
            }
            else
            {
                // Portrait
                img.Mutate(x => x.Resize(1080, 1920));
            }

            await img.SaveAsJpegAsync(outputStream);
        }

        // Rewind dello stream per l'upload
        outputStream.Seek(0, SeekOrigin.Begin);

        // Generazione di un nome file univoco
        var fileName = $"{Path.GetFileNameWithoutExtension(image.FileName)}_{Guid.NewGuid()}.jpg";
        var filePath = string.IsNullOrEmpty(folderName)
                            ? fileName
                            : $"{folderName.TrimEnd('/')}/{fileName}";

        await blobServiceClient.UploadImagesAsync(filePath, outputStream, loadInMatchFolder);
        
        return NoContent();
    }
}
