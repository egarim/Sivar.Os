namespace Sivar.Os.Shared.Configuration
{

    /// <summary>
    /// Result of a bulk delete operation
    /// </summary>
    public class BulkDeleteResult
    {
        /// <summary>
        /// File IDs that were successfully deleted
        /// </summary>
        public IList<string> SuccessfulDeletes { get; set; } = new List<string>();

        /// <summary>
        /// File IDs that failed to delete with error messages
        /// </summary>
        public IList<FileDeleteError> FailedDeletes { get; set; } = new List<FileDeleteError>();

        /// <summary>
        /// True if all files were deleted successfully
        /// </summary>
        public bool AllSucceeded => !FailedDeletes.Any();

        /// <summary>
        /// True if some files succeeded and some failed
        /// </summary>
        public bool HasPartialFailures => FailedDeletes.Any();
    }
}
