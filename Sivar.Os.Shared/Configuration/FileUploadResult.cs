namespace Sivar.Os.Shared.Configuration
{

    /// <summary>
    /// Result of a single file upload
    /// </summary>
    public class FileUploadResult
    {
        /// <summary>
        /// Unique identifier for the uploaded file
        /// </summary>
        public required string FileId { get; set; }

        /// <summary>
        /// Public URL to access the file
        /// </summary>
        public required string Url { get; set; }

        /// <summary>
        /// Container where the file was stored
        /// </summary>
        public required string Container { get; set; }

        /// <summary>
        /// Size of the uploaded file in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// When the file was uploaded
        /// </summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// Original filename
        /// </summary>
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// MIME type of the file
        /// </summary>
        public string? ContentType { get; set; }
    }
}
