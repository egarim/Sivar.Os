namespace Sivar.Os.Shared.Configuration
{

    /// <summary>
    /// Request for uploading multiple files
    /// </summary>
    public class BulkFileUploadRequest
    {
        /// <summary>
        /// Collection of files to upload
        /// </summary>
        public ICollection<FileUploadRequest> Files { get; set; } = new List<FileUploadRequest>();

        /// <summary>
        /// Storage container name for all files
        /// </summary>
        public required string Container { get; set; }

        /// <summary>
        /// Common metadata to apply to all files
        /// </summary>
        public Dictionary<string, string> CommonMetadata { get; set; } = new();

        /// <summary>
        /// Override default max file count for this request
        /// </summary>
        public int? MaxFileCount { get; set; }

        /// <summary>
        /// Override default max total size for this request
        /// </summary>
        public long? MaxTotalSizeBytes { get; set; }
    }
}
