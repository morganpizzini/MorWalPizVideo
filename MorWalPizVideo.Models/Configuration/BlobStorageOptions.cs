
namespace MorWalPizVideo.Models.Configuration
{
    public class BlobStorageOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string UploadContainerName { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string SponsorContainerName { get; set; } = string.Empty;
        public string PageContainerName { get; set; } = string.Empty;
    }
}
