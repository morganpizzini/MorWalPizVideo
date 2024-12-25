
namespace MorWalPizVideo.Models.Configuration
{
    public class BlobStorageOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string UploadContainerName { get; set; } = string.Empty;
    }
}
