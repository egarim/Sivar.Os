namespace Sivar.Os.Shared.Configuration
{

    /// <summary>
    /// Local file storage configuration for development
    /// </summary>
    public class LocalFileStorageConfiguration
    {
        /// <summary>
        /// Root directory for storing files
        /// </summary>
        public string RootDirectory { get; set; } = "wwwroot/uploads";

        /// <summary>
        /// Base URL for accessing files
        /// </summary>
        public string BaseUrl { get; set; } = "/uploads";

        /// <summary>
        /// Whether to create subdirectories by date (yyyy/MM/dd)
        /// </summary>
        public bool UseDataSubdirectories { get; set; } = true;
    }
}