namespace Sivar.Os.Shared.Configuration
{

    /// <summary>
    /// Configuration settings for Azure Blob Storage file service
    /// </summary>
    public class AzureBlobStorageConfiguration
    {
        /// <summary>
        /// Azure Storage connection string
        /// For development with Azurite: "UseDevelopmentStorage=true"
        /// For Azure: "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
        /// </summary>
        public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";

        /// <summary>
        /// Base container name for file storage
        /// </summary>
        public string BaseContainer { get; set; } = "files";

        /// <summary>
        /// Base URL for accessing files (e.g., CDN endpoint)
        /// If not set, will use the blob service URL
        /// </summary>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Whether to create containers automatically if they don't exist
        /// </summary>
        public bool AutoCreateContainers { get; set; } = true;

        /// <summary>
        /// Default public access level for containers
        /// </summary>
        public Azure.Storage.Blobs.Models.PublicAccessType DefaultPublicAccessType { get; set; } =
            Azure.Storage.Blobs.Models.PublicAccessType.Blob;

        /// <summary>
        /// Maximum number of parallel uploads for bulk operations
        /// </summary>
        public int MaxConcurrentUploads { get; set; } = 4;

        /// <summary>
        /// Timeout for individual blob operations
        /// </summary>
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to use hierarchical namespace (folders) in blob names
        /// </summary>
        public bool UseHierarchicalNamespace { get; set; } = true;
    }
}
