using Microsoft.Extensions.Options;
using MorWalPizVideo.Models.Configuration;
using Azure.Storage.Blobs;
using MongoDB.Driver.Core.Configuration;

namespace MorWalPizVideo.Domain
{
    public class BlobService
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
