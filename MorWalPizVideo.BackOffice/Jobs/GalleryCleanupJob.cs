using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Jobs;

public sealed class GalleryCleanupJob(IGalleryRepository repository, IBlobService blobService, ILogger<GalleryCleanupJob> logger)
{
    public const string JobId = "gallery-expiration-cleanup";

    public async Task ExecuteAsync()
    {
        var expired = (await repository.GetItemsAsync(item => item.Expiration <= DateTime.UtcNow)).ToArray();
        foreach (var gallery in expired)
        {
            foreach (var image in gallery.BlobReference)
            {
                await blobService.DeleteImageAsync(image.StorageKey);
                if (!string.IsNullOrWhiteSpace(image.PreviewStorageKey))
                    await blobService.DeleteImageAsync(image.PreviewStorageKey);
            }
            await repository.DeleteItemAsync(gallery.Id);
        }
        logger.LogInformation("Gallery cleanup deleted {Count} expired galleries", expired.Length);
    }
}