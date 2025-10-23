namespace Sivar.Os.Shared.Configuration
{

    /// <summary>
    /// Information about a file delete error
    /// </summary>
    public class FileDeleteError
    {
        /// <summary>
        /// File ID that failed to delete
        /// </summary>
        public required string FileId { get; set; }

        /// <summary>
        /// Error message describing the failure
        /// </summary>
        public required string ErrorMessage { get; set; }
    }
}