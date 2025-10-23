namespace Sivar.Os.Shared.Configuration;

/// <summary>
/// Configuration for file storage services
/// </summary>
public class FileStorageConfiguration
{
    /// <summary>
    /// Maximum number of files that can be uploaded in a single request
    /// </summary>
    public int MaxFilesPerRequest { get; set; } = 10;
    
    /// <summary>
    /// Maximum total size (in bytes) for all files in a single request
    /// </summary>
    public long MaxTotalRequestSizeBytes { get; set; } = 20 * 1024 * 1024; // 20 MB
    
    /// <summary>
    /// Maximum size (in bytes) for an individual file
    /// </summary>
    public long MaxIndividualFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5 MB
    
    /// <summary>
    /// Allowed MIME types for file uploads
    /// </summary>
    public HashSet<string> AllowedMimeTypes { get; set; } = new()
    {
        // Images
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp",
        
        // Videos
        "video/mp4", "video/avi", "video/mov", "video/wmv", "video/mkv",
        
        // Documents
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        
        // Text
        "text/plain", "text/csv",
        
        // Audio
        "audio/mpeg", "audio/wav", "audio/ogg"
    };
    
    /// <summary>
    /// Container-specific configuration overrides
    /// </summary>
    public Dictionary<string, ContainerConfiguration> ContainerConfigurations { get; set; } = new();
    
    /// <summary>
    /// Default container name if none specified
    /// </summary>
    public string DefaultContainer { get; set; } = "general";
}
