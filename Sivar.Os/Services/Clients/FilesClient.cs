using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of files client
/// </summary>
public class FilesClient : BaseRepositoryClient, IFilesClient
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FilesClient> _logger;

    public FilesClient(
        IFileStorageService fileStorageService,
        ILogger<FilesClient> logger)
    {
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Upload operations
    public async Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string contentType, string container = "posts", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UploadFileAsync: {FileName}, Container: {Container}", fileName, container);

        try
        {
            var uploadRequest = new FileUploadRequest
            {
                FileStream = fileStream,
                FileName = fileName,
                ContentType = contentType,
                Container = container,
                Metadata = new Dictionary<string, string>()
            };

            var result = await _fileStorageService.UploadFileAsync(uploadRequest);
            _logger.LogInformation("File uploaded successfully: {FileId}", result.FileId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", fileName);
            throw;
        }
    }

    public async Task<BulkFileUploadResult> UploadBulkAsync(IEnumerable<(Stream stream, string fileName, string contentType)> files, string container = "posts", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UploadBulkAsync: Container: {Container}, FileCount: {Count}", container, files.Count());

        try
        {
            var fileRequests = files.Select(f => new FileUploadRequest
            {
                FileStream = f.stream,
                FileName = f.fileName,
                ContentType = f.contentType,
                Container = container,
                Metadata = new Dictionary<string, string>()
            }).ToList();

            var bulkRequest = new BulkFileUploadRequest
            {
                Files = fileRequests,
                Container = container,
                CommonMetadata = new Dictionary<string, string>()
            };

            var result = await _fileStorageService.UploadFilesAsync(bulkRequest);
            _logger.LogInformation("Bulk upload completed: Success={Success}, Failed={Failed}", 
                result.SuccessfulUploads.Count, result.FailedUploads.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk upload");
            throw;
        }
    }

    // Get operations
    public async Task<string> GetFileUrlAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        if (fileId == Guid.Empty)
        {
            _logger.LogWarning("GetFileUrlAsync called with empty file ID");
            return string.Empty;
        }

        try
        {
            var fileUrl = await _fileStorageService.GetFileUrlAsync(fileId.ToString());
            _logger.LogInformation("File URL retrieved: {FileId}", fileId);
            return fileUrl ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file URL {FileId}", fileId);
            throw;
        }
    }

    public async Task<FileMetadata> GetFileMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        if (fileId == Guid.Empty)
        {
            _logger.LogWarning("GetFileMetadataAsync called with empty file ID");
            return new FileMetadata { FileId = fileId.ToString() };
        }

        try
        {
            var metadata = await _fileStorageService.GetFileMetadataAsync(fileId.ToString());
            _logger.LogInformation("File metadata retrieved: {FileId}", fileId);
            return metadata ?? new FileMetadata { FileId = fileId.ToString() };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file metadata {FileId}", fileId);
            throw;
        }
    }

    // Delete operations
    public async Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        if (fileId == Guid.Empty)
        {
            _logger.LogWarning("DeleteFileAsync called with empty file ID");
            return;
        }

        try
        {
            await _fileStorageService.DeleteFileAsync(fileId.ToString());
            _logger.LogInformation("File deleted successfully: {FileId}", fileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", fileId);
            throw;
        }
    }

    public async Task DeleteBulkAsync(IEnumerable<Guid> fileIds, CancellationToken cancellationToken = default)
    {
        var fileIdList = fileIds.ToList();
        _logger.LogInformation("DeleteBulkAsync: FileCount: {Count}", fileIdList.Count);

        try
        {
            var fileIdStrings = fileIdList.Select(id => id.ToString());
            var result = await _fileStorageService.DeleteFilesAsync(fileIdStrings);
            _logger.LogInformation("Bulk delete completed: Success={Success}, Failed={Failed}", 
                result.SuccessfulDeletes.Count, result.FailedDeletes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk delete");
            throw;
        }
    }
}
