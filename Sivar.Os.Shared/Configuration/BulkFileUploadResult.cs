namespace Sivar.Os.Shared.Configuration
{

    /// <summary>
    /// Result of a bulk file upload operation
    /// </summary>
    public class BulkFileUploadResult
    {
        /// <summary>
        /// Files that were successfully uploaded
        /// </summary>
        public IList<FileUploadResult> SuccessfulUploads { get; set; } = new List<FileUploadResult>();

        /// <summary>
        /// Files that failed to upload
        /// </summary>
        public IList<FileUploadError> FailedUploads { get; set; } = new List<FileUploadError>();

        /// <summary>
        /// True if some files succeeded and some failed
        /// </summary>
        public bool HasPartialFailures => FailedUploads.Any();

        /// <summary>
        /// True if all files uploaded successfully
        /// </summary>
        public bool AllSucceeded => !FailedUploads.Any();

        /// <summary>
        /// Total number of files in the request
        /// </summary>
        public int TotalFiles => SuccessfulUploads.Count + FailedUploads.Count;

        /// <summary>
        /// Total bytes successfully uploaded
        /// </summary>
        public long TotalUploadedBytes => SuccessfulUploads.Sum(u => u.FileSizeBytes);
    }
}
