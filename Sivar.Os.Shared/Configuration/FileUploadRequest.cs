namespace Sivar.Os.Shared.Configuration;

/// <summary>
/// Request for uploading a single file
/// </summary>
public class FileUploadRequest
{
    /// <summary>
    /// The file content stream
    /// </summary>
    public required Stream FileStream { get; set; }
    
    /// <summary>
    /// Original filename
    /// </summary>
    public required string FileName { get; set; }
    
    /// <summary>
    /// MIME type of the file
    /// </summary>
    public required string ContentType { get; set; }
    
    /// <summary>
    /// Storage container name (e.g., "profile-avatars", "post-attachments")
    /// </summary>
    public required string Container { get; set; }
    
    /// <summary>
    /// Additional metadata to store with the file
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
