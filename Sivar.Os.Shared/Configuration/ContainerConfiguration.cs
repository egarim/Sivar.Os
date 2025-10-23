namespace Sivar.Os.Shared.Configuration
{

    /// <summary>
    /// Configuration specific to a storage container
    /// </summary>
    public class ContainerConfiguration
    {
        /// <summary>
        /// Override max files per request for this container
        /// </summary>
        public int? MaxFilesPerRequest { get; set; }

        /// <summary>
        /// Override max total request size for this container
        /// </summary>
        public long? MaxTotalRequestSizeBytes { get; set; }

        /// <summary>
        /// Override max individual file size for this container
        /// </summary>
        public long? MaxIndividualFileSizeBytes { get; set; }

        /// <summary>
        /// Override allowed MIME types for this container
        /// </summary>
        public HashSet<string>? AllowedMimeTypes { get; set; }

        /// <summary>
        /// Container description
        /// </summary>
        public string? Description { get; set; }
    }
}
