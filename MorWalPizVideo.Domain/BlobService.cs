using Microsoft.Extensions.Options;
using MorWalPizVideo.Models.Configuration;
using Azure.Storage.Blobs;

namespace MorWalPizVideo.Domain
{
    public interface IBlobService
    {
        public Task<List<string>> GetImagesInFolderAsync(string folderName);
        public Task UploadImagesAsync(string filePath, MemoryStream stream, bool loadInMatchFolder = false);
    }
    public class BlobServiceMock : IBlobService
    {
        public Task<List<string>> GetImagesInFolderAsync(string folderName)
            =>
            Task.FromResult(new List<string> { "https://placehold.co/1920x1080", "https://placehold.co/1920x1080", "https://placehold.co/1920x1080" });
        
        public Task UploadImagesAsync(string filePath, MemoryStream stream, bool loadInMatchFolder = false) => Task.CompletedTask;
    }
    public class BlobService : IBlobService
    {
        private readonly BlobStorageOptions _options;
        public BlobService(IOptions<BlobStorageOptions> options)
        {
            _options = options.Value;
        }

        public async Task<List<string>> GetImagesInFolderAsync(string folderName)
        {
            var _blobContainerClient = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
            var images = new List<string>();
            await foreach (var blobItem in _blobContainerClient.GetBlobsAsync(prefix: folderName))
            {
                if (blobItem.Properties.ContentType?.StartsWith("image/") == true)
                {
                    var blobClient = _blobContainerClient.GetBlobClient(blobItem.Name);
                    images.Add(blobClient.Uri.ToString());
                }
            }
            return images;
        }
        public Task UploadImagesAsync(string filePath, MemoryStream stream, bool loadInMatchFolder= false)
        {
            var _blobContainerClient = new BlobContainerClient(_options.ConnectionString, loadInMatchFolder ? _options.ContainerName : _options.UploadContainerName);

            var blobClient = _blobContainerClient.GetBlobClient(filePath);

            return blobClient.UploadAsync(stream, overwrite: true);
        }

    }
}
