
namespace Sivar.Os.Shared.Services;

/// <summary>
/// Core interface for file storage operations supporting both single and bulk uploads
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload a single file to storage
    /// </summary>
    /// <param name="request">File upload request</param>
    /// <returns>Upload result with file ID and URL</returns>
    Task<FileUploadResult> UploadFileAsync(FileUploadRequest request);
    
    /// <summary>
    /// Upload multiple files to storage with validation
    /// </summary>
    /// <param name="request">Bulk file upload request</param>
    /// <returns>Bulk upload result with successful uploads and any failures</returns>
    Task<BulkFileUploadResult> UploadFilesAsync(BulkFileUploadRequest request);
    
    /// <summary>
    /// Get the public URL for a stored file
    /// </summary>
    /// <param name="fileId">File identifier</param>
    /// <returns>Public URL to access the file</returns>
    Task<string> GetFileUrlAsync(string fileId);
    
    /// <summary>
    /// Delete a file from storage
    /// </summary>
    /// <param name="fileId">File identifier</param>
    /// <returns>True if file was deleted successfully</returns>
    Task<bool> DeleteFileAsync(string fileId);
    
    /// <summary>
    /// Get metadata for a stored file
    /// </summary>
    /// <param name="fileId">File identifier</param>
    /// <returns>File metadata or null if not found</returns>
    Task<FileMetadata?> GetFileMetadataAsync(string fileId);
    
    /// <summary>
    /// Check if a file exists in storage
    /// </summary>
    /// <param name="fileId">File identifier</param>
    /// <returns>True if file exists</returns>
    Task<bool> FileExistsAsync(string fileId);
    
    /// <summary>
    /// Delete multiple files from storage
    /// </summary>
    /// <param name="fileIds">Collection of file identifiers</param>
    /// <returns>Bulk delete result with success/failure information</returns>
    Task<BulkDeleteResult> DeleteFilesAsync(IEnumerable<string> fileIds);
}