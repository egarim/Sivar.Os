namespace Sivar.Os.Shared.Configuration
{

    /// <summary>
    /// Metadata information for a stored file
    /// </summary>
    public class FileMetadata
    {
        /// <summary>
        /// File identifier
        /// </summary>
        public required string FileId { get; set; }

        /// <summary>
        /// Original filename
        /// </summary>
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// MIME type
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// When the file was uploaded
        /// </summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// Container where the file is stored
        /// </summary>
        public string? Container { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
