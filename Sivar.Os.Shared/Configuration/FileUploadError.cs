namespace Sivar.Os.Shared.Configuration
{

    /// <summary>
    /// Information about a file upload error
    /// </summary>
    public class FileUploadError
    {
        /// <summary>
        /// Name of the file that failed to upload
        /// </summary>
        public required string FileName { get; set; }

        /// <summary>
        /// Error message describing the failure
        /// </summary>
        public required string ErrorMessage { get; set; }

        /// <summary>
        /// Type of error that occurred
        /// </summary>
        public FileUploadErrorType ErrorType { get; set; }

        /// <summary>
        /// Position of the file in the original request
        /// </summary>
        public int FileIndex { get; set; }
    }
}
