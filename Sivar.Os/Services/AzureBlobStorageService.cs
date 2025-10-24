using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sivar.Os.Shared.Configuration;
using Sivar.Os.Shared.Services;
using System.Collections.Concurrent;
using System.Text;

namespace Sivar.Server.Library.Services;

/// <summary>
/// Azure Blob Storage implementation of file storage service with bulk upload support
/// </summary>
public class AzureBlobStorageService : IFileStorageService
{
    private readonly AzureBlobStorageConfiguration _config;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly ConcurrentDictionary<string, BlobContainerClient> _containerClients;

    public AzureBlobStorageService(
        IOptions<AzureBlobStorageConfiguration> config,
        ILogger<AzureBlobStorageService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _containerClients = new ConcurrentDictionary<string, BlobContainerClient>();

        // Initialize blob service client
        _blobServiceClient = new BlobServiceClient(_config.ConnectionString);
    }

    public async Task<FileUploadResult> UploadFileAsync(FileUploadRequest request)
    {
        try
        {
            var fileId = Guid.NewGuid().ToString("N");
            var containerClient = await GetOrCreateContainerAsync(request.Container);
            var blobName = GenerateBlobName(fileId, request.Container, request.FileName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Set content type and metadata
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = request.ContentType
                },
                Metadata = new Dictionary<string, string>(request.Metadata)
                {
                    ["original_filename"] = request.FileName,
                    ["uploaded_at"] = DateTime.UtcNow.ToString("O"),
                    ["file_id"] = fileId
                }
            };

            // Upload the file
            var response = await blobClient.UploadAsync(request.FileStream, uploadOptions);
            
            // Get file size from the response or blob properties
            var properties = await blobClient.GetPropertiesAsync();
            var fileSize = properties.Value.ContentLength;
            var publicUrl = GeneratePublicUrl(blobClient.Uri, request.Container, fileId, request.FileName);

            _logger.LogInformation("Successfully uploaded file {FileName} with ID {FileId} to Azure Blob Storage", 
                request.FileName, fileId);

            return new FileUploadResult
            {
                FileId = fileId,
                Url = publicUrl,
                Container = request.Container,
                FileSizeBytes = fileSize,
                UploadedAt = DateTime.UtcNow,
                OriginalFileName = request.FileName,
                ContentType = request.ContentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName} to Azure Blob Storage", request.FileName);
            throw;
        }
    }

    public async Task<BulkFileUploadResult> UploadFilesAsync(BulkFileUploadRequest request)
    {
        var result = new BulkFileUploadResult();
        var semaphore = new SemaphoreSlim(_config.MaxConcurrentUploads, _config.MaxConcurrentUploads);
        
        var tasks = request.Files.Select(async (fileRequest, index) =>
        {
            await semaphore.WaitAsync();
            try
            {
                // Merge common metadata with file-specific metadata
                var mergedMetadata = new Dictionary<string, string>(request.CommonMetadata);
                foreach (var kvp in fileRequest.Metadata)
                {
                    mergedMetadata[kvp.Key] = kvp.Value;
                }

                var uploadRequest = new FileUploadRequest
                {
                    FileStream = fileRequest.FileStream,
                    FileName = fileRequest.FileName,
                    ContentType = fileRequest.ContentType,
                    Container = request.Container,
                    Metadata = mergedMetadata
                };

                var uploadResult = await UploadFileAsync(uploadRequest);
                
                lock (result)
                {
                    result.SuccessfulUploads.Add(uploadResult);
                }

                _logger.LogDebug("Bulk upload: Successfully uploaded file {Index}/{Total}: {FileName}", 
                    index + 1, request.Files.Count, fileRequest.FileName);
            }
            catch (Exception ex)
            {
                var error = new FileUploadError
                {
                    FileName = fileRequest.FileName,
                    ErrorType = FileUploadErrorType.StorageError,
                    ErrorMessage = ex.Message
                };

                lock (result)
                {
                    result.FailedUploads.Add(error);
                }

                _logger.LogWarning(ex, "Bulk upload: Failed to upload file {Index}/{Total}: {FileName}", 
                    index + 1, request.Files.Count, fileRequest.FileName);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        _logger.LogInformation("Bulk upload completed: {SuccessCount} successful, {FailureCount} failed", 
            result.SuccessfulUploads.Count, result.FailedUploads.Count);

        return result;
    }

    public async Task<bool> DeleteFileAsync(string fileId)
    {
        try
        {
            // Find the blob by searching through containers
            // In a production system, you might want to store container info with the file ID
            var containers = _blobServiceClient.GetBlobContainersAsync();
            
            await foreach (var container in containers)
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(container.Name);
                var blobs = containerClient.GetBlobsAsync(prefix: fileId);
                
                await foreach (var blob in blobs)
                {
                    if (blob.Metadata?.ContainsKey("file_id") == true && 
                        blob.Metadata["file_id"] == fileId)
                    {
                        var blobClient = containerClient.GetBlobClient(blob.Name);
                        await blobClient.DeleteIfExistsAsync();
                        
                        _logger.LogInformation("Successfully deleted file {FileId} from Azure Blob Storage", fileId);
                        return true;
                    }
                }
            }

            _logger.LogWarning("File {FileId} not found in Azure Blob Storage", fileId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileId} from Azure Blob Storage", fileId);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string fileId)
    {
        try
        {
            var containers = _blobServiceClient.GetBlobContainersAsync();
            
            await foreach (var container in containers)
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(container.Name);
                var blobs = containerClient.GetBlobsAsync(prefix: fileId);
                
                await foreach (var blob in blobs)
                {
                    if (blob.Metadata?.ContainsKey("file_id") == true && 
                        blob.Metadata["file_id"] == fileId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if file {FileId} exists in Azure Blob Storage", fileId);
            throw;
        }
    }

    public async Task<string> GetFileUrlAsync(string fileId)
    {
        try
        {
            var containers = _blobServiceClient.GetBlobContainersAsync();
            
            await foreach (var container in containers)
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(container.Name);
                var blobs = containerClient.GetBlobsAsync(prefix: fileId);
                
                await foreach (var blob in blobs)
                {
                    if (blob.Metadata?.ContainsKey("file_id") == true && 
                        blob.Metadata["file_id"] == fileId)
                    {
                        var blobClient = containerClient.GetBlobClient(blob.Name);
                        var originalFileName = blob.Metadata.TryGetValue("original_filename", out var fileName) ? fileName : "unknown";
                        return GeneratePublicUrl(blobClient.Uri, container.Name, fileId, originalFileName);
                    }
                }
            }

            throw new FileNotFoundException($"File with ID {fileId} not found");
        }
        catch (Exception ex) when (!(ex is FileNotFoundException))
        {
            _logger.LogError(ex, "Failed to get URL for file {FileId} from Azure Blob Storage", fileId);
            throw;
        }
    }

    public async Task<BulkDeleteResult> DeleteFilesAsync(IEnumerable<string> fileIds)
    {
        var result = new BulkDeleteResult();
        var semaphore = new SemaphoreSlim(_config.MaxConcurrentUploads, _config.MaxConcurrentUploads);
        
        var tasks = fileIds.Select(async fileId =>
        {
            await semaphore.WaitAsync();
            try
            {
                var deleted = await DeleteFileAsync(fileId);
                
                lock (result)
                {
                    if (deleted)
                    {
                        result.SuccessfulDeletes.Add(fileId);
                    }
                    else
                    {
                        result.FailedDeletes.Add(new FileDeleteError
                        {
                            FileId = fileId,
                            ErrorMessage = "File not found"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                lock (result)
                {
                    result.FailedDeletes.Add(new FileDeleteError
                    {
                        FileId = fileId,
                        ErrorMessage = ex.Message
                    });
                }
                
                _logger.LogWarning(ex, "Failed to delete file {FileId} from Azure Blob Storage", fileId);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        _logger.LogInformation("Bulk delete completed: {SuccessCount} successful, {FailureCount} failed", 
            result.SuccessfulDeletes.Count, result.FailedDeletes.Count);

        return result;
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(string fileId)
    {
        try
        {
            var containers = _blobServiceClient.GetBlobContainersAsync();
            
            await foreach (var container in containers)
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(container.Name);
                var blobs = containerClient.GetBlobsAsync(prefix: fileId);
                
                await foreach (var blob in blobs)
                {
                    if (blob.Metadata?.ContainsKey("file_id") == true && 
                        blob.Metadata["file_id"] == fileId)
                    {
                        return new FileMetadata
                        {
                            FileId = fileId,
                            OriginalFileName = blob.Metadata.TryGetValue("original_filename", out var originalFileName) ? originalFileName : "unknown",
                            ContentType = blob.Properties.ContentType ?? "application/octet-stream",
                            FileSizeBytes = blob.Properties.ContentLength ?? 0,
                            UploadedAt = blob.Properties.CreatedOn?.DateTime ?? DateTime.MinValue
                        };
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for file {FileId} from Azure Blob Storage", fileId);
            throw;
        }
    }

    #region Private Helper Methods

    private async Task<BlobContainerClient> GetOrCreateContainerAsync(string containerName)
    {
        return await _containerClients.GetOrAddAsync(containerName, async name =>
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(GetContainerName(name));
            
            if (_config.AutoCreateContainers)
            {
                await containerClient.CreateIfNotExistsAsync(_config.DefaultPublicAccessType);
                _logger.LogDebug("Ensured container {ContainerName} exists", containerClient.Name);
            }

            return containerClient;
        });
    }

    private string GetContainerName(string logicalContainer)
    {
        // Ensure container name meets Azure naming requirements
        var containerName = $"{_config.BaseContainer}-{logicalContainer}".ToLowerInvariant();
        
        // Replace invalid characters with hyphens
        var sb = new StringBuilder();
        foreach (char c in containerName)
        {
            if (char.IsLetterOrDigit(c) || c == '-')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('-');
            }
        }

        // Ensure it starts and ends with alphanumeric
        containerName = sb.ToString().Trim('-');
        if (containerName.Length < 3) containerName = containerName.PadRight(3, '0');
        if (containerName.Length > 63) containerName = containerName.Substring(0, 63).TrimEnd('-');

        return containerName;
    }

    private string GenerateBlobName(string fileId, string container, string fileName)
    {
        if (_config.UseHierarchicalNamespace)
        {
            return $"{container}/{fileId}_{fileName}";
        }
        else
        {
            return $"{fileId}_{fileName}";
        }
    }

    private string GeneratePublicUrl(Uri blobUri, string container, string fileId, string fileName)
    {
        if (!string.IsNullOrEmpty(_config.BaseUrl))
        {
            return $"{_config.BaseUrl.TrimEnd('/')}/{container}/{fileId}_{fileName}";
        }

        return blobUri.ToString();
    }

    #endregion
}

/// <summary>
/// Extension methods for concurrent dictionary
/// </summary>
internal static class ConcurrentDictionaryExtensions
{
    public static async Task<TValue> GetOrAddAsync<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, Task<TValue>> valueFactory) where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var existingValue))
        {
            return existingValue;
        }

        var newValue = await valueFactory(key);
        return dictionary.GetOrAdd(key, newValue);
    }
}

