namespace Sivar.Os.Shared.Services;

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

/// <summary>
/// Request for uploading multiple files
/// </summary>
public class BulkFileUploadRequest
{
    /// <summary>
    /// Collection of files to upload
    /// </summary>
    public ICollection<FileUploadRequest> Files { get; set; } = new List<FileUploadRequest>();
    
    /// <summary>
    /// Storage container name for all files
    /// </summary>
    public required string Container { get; set; }
    
    /// <summary>
    /// Common metadata to apply to all files
    /// </summary>
    public Dictionary<string, string> CommonMetadata { get; set; } = new();
    
    /// <summary>
    /// Override default max file count for this request
    /// </summary>
    public int? MaxFileCount { get; set; }
    
    /// <summary>
    /// Override default max total size for this request
    /// </summary>
    public long? MaxTotalSizeBytes { get; set; }
}

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

/// <summary>
/// Types of file upload errors
/// </summary>
public enum FileUploadErrorType
{
    FileTooLarge,
    UnsupportedFileType,
    InvalidFileName,
    StorageError,
    ValidationError,
    UnknownError
}

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