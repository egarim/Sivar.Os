using Sivar.Core.Clients.Files;
using Sivar.Core.DTOs;
using Sivar.Core.Interfaces;
using Sivar.Core.Repositories;
using Sivar.Os.Shared.Configuration;

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
        _logger.LogInformation("GetFileMetadataAsync: {FileId}", fileId);
        return new FileMetadata { FileId = fileId.ToString() };
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
}
