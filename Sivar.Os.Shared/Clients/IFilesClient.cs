using Sivar.Os.Shared.Services;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client for file operations
/// </summary>
public interface IFilesClient
{
    // Upload operations - TODO: Implement multipart/form-data upload
    // Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    // Task<BulkFileUploadResult> UploadBulkAsync(IEnumerable<(Stream stream, string fileName, string contentType)> files, CancellationToken cancellationToken = default);

    // Get operations
    Task<string> GetFileUrlAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<FileMetadata> GetFileMetadataAsync(Guid fileId, CancellationToken cancellationToken = default);

    // Delete operations
    Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default);
    // Task DeleteBulkAsync(IEnumerable<Guid> fileIds, CancellationToken cancellationToken = default);
}
