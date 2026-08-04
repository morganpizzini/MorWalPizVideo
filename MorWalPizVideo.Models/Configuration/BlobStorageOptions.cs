namespace MorWalPizVideo.Models.Configuration
{
    public class BlobStorageOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string UploadContainerName { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public bool PreferManagedIdentity { get; set; }
        public string SponsorContainerName { get; set; } = string.Empty;
        public string PageContainerName { get; set; } = string.Empty;
        public string RecoveryContainerName { get; set; } = string.Empty;
    }
}
