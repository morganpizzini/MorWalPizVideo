using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
namespace MorWalPizVideo.BackOffice.Controllers;

[RequireChannelScope]
public class ImageUploadController : ApplicationControllerBase
{
    private readonly IBlobService blobServiceClient;
    private readonly IYouTubeContentRepository contentRepository;

    public ImageUploadController(IYouTubeContentRepository contentRepository, IBlobService blobServiceClient)
    {
        this.contentRepository = contentRepository;
        this.blobServiceClient = blobServiceClient;
    }

    [HttpPost("upload")]
    [AllowUser(AuthorizationPermissionKeys.ImagesCreate, AuthorizationPermissionKeys.ImagesManage)]
    public async Task<IActionResult> UploadImage(IFormFile image, string folderName, bool loadInMatchFolder)
    {
        if (image == null || image.Length == 0)
        {
            return BadRequest("File non valido.");
        }

        var existingMatch = (await contentRepository.GetItemsAsync(match => match.Url == folderName)).FirstOrDefault();

        if (existingMatch == null)
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

    [HttpPost("upload-multiple")]
    [AllowUser(AuthorizationPermissionKeys.ImagesCreate, AuthorizationPermissionKeys.ImagesManage)]
    public async Task<IActionResult> UploadImages(IFormFileCollection images, string folderName, bool loadInMatchFolder)
    {
        if (images == null || images.Count == 0)
        {
            return BadRequest("Nessun file fornito.");
        }

        foreach (var image in images)
        {
            if (image.Length == 0) continue;

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

            var fileName = $"{Path.GetFileNameWithoutExtension(image.FileName)}_{Guid.NewGuid()}.jpg";
            var filePath = string.IsNullOrEmpty(folderName)
                                ? fileName
                                : $"{folderName.TrimEnd('/')}/{fileName}";

            // Creazione del container se non esiste
            await blobServiceClient.UploadImagesAsync(filePath, outputStream, loadInMatchFolder);

        }

        return NoContent();
    }
}
