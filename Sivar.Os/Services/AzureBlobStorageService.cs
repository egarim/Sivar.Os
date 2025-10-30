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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[AzureBlobStorageService.UploadFileAsync] START - RequestId={RequestId}, FileName={FileName}, Container={Container}, ContentType={ContentType}, FileSizeBytes={FileSizeBytes}",
            requestId, request?.FileName ?? "NULL", request?.Container ?? "NULL", request?.ContentType ?? "NULL", request?.FileStream?.Length ?? 0);

        try
        {
            if (request == null)
            {
                _logger.LogWarning("[AzureBlobStorageService.UploadFileAsync] Invalid request - RequestId={RequestId}",
                    requestId);
                throw new ArgumentNullException(nameof(request));
            }

            var fileId = Guid.NewGuid().ToString("N");
            _logger.LogInformation("[AzureBlobStorageService.UploadFileAsync] Generated FileId - RequestId={RequestId}, FileId={FileId}",
                requestId, fileId);

            var containerClient = await GetOrCreateContainerAsync(request.Container);
            _logger.LogInformation("[AzureBlobStorageService.UploadFileAsync] Container client obtained - RequestId={RequestId}, Container={Container}",
                requestId, request.Container);

            var blobName = GenerateBlobName(fileId, request.Container, request.FileName);
            _logger.LogInformation("[AzureBlobStorageService.UploadFileAsync] Generated blob name - RequestId={RequestId}, BlobName={BlobName}",
                requestId, blobName);

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

            _logger.LogInformation("[AzureBlobStorageService.UploadFileAsync] Uploading file - RequestId={RequestId}, FileId={FileId}, FileName={FileName}",
                requestId, fileId, request.FileName);

            // Upload the file
            var response = await blobClient.UploadAsync(request.FileStream, uploadOptions);
            
            _logger.LogInformation("[AzureBlobStorageService.UploadFileAsync] File uploaded - RequestId={RequestId}, FileId={FileId}, Status={Status}",
                requestId, fileId, response.GetRawResponse().Status);

            // Get file size from the response or blob properties
            var properties = await blobClient.GetPropertiesAsync();
            var fileSize = properties.Value.ContentLength;
            var publicUrl = GeneratePublicUrl(blobClient.Uri, request.Container, fileId, request.FileName);

            _logger.LogInformation("[AzureBlobStorageService.UploadFileAsync] File properties retrieved - RequestId={RequestId}, FileId={FileId}, FileSizeBytes={FileSizeBytes}",
                requestId, fileId, fileSize);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[AzureBlobStorageService.UploadFileAsync] SUCCESS - RequestId={RequestId}, FileId={FileId}, FileSizeBytes={FileSizeBytes}, Duration={Duration}ms",
                requestId, fileId, fileSize, elapsed);

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
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[AzureBlobStorageService.UploadFileAsync] ERROR - RequestId={RequestId}, FileName={FileName}, Duration={Duration}ms",
                requestId, request?.FileName ?? "NULL", elapsed);
            throw;
        }
    }

    public async Task<BulkFileUploadResult> UploadFilesAsync(BulkFileUploadRequest request)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[AzureBlobStorageService.UploadFilesAsync] START - RequestId={RequestId}, FileCount={FileCount}, Container={Container}, MaxConcurrentUploads={MaxConcurrent}",
            requestId, request?.Files?.Count ?? 0, request?.Container ?? "NULL", _config.MaxConcurrentUploads);

        try
        {
            var result = new BulkFileUploadResult();
            var semaphore = new SemaphoreSlim(_config.MaxConcurrentUploads, _config.MaxConcurrentUploads);
            
            var tasks = request.Files.Select(async (fileRequest, index) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    _logger.LogInformation("[AzureBlobStorageService.UploadFilesAsync] Uploading file {Index}/{Total} - RequestId={RequestId}, FileName={FileName}",
                        index + 1, request.Files.Count, requestId, fileRequest.FileName);

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

                    _logger.LogInformation("[AzureBlobStorageService.UploadFilesAsync] File uploaded successfully {Index}/{Total} - RequestId={RequestId}, FileName={FileName}, FileId={FileId}, FileSizeBytes={FileSizeBytes}",
                        index + 1, request.Files.Count, requestId, fileRequest.FileName, uploadResult.FileId, uploadResult.FileSizeBytes);
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

                    _logger.LogWarning(ex, "[AzureBlobStorageService.UploadFilesAsync] File upload failed {Index}/{Total} - RequestId={RequestId}, FileName={FileName}, Error={Error}",
                        index + 1, request.Files.Count, requestId, fileRequest.FileName, ex.Message);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[AzureBlobStorageService.UploadFilesAsync] SUCCESS - RequestId={RequestId}, SuccessCount={SuccessCount}, FailureCount={FailureCount}, TotalCount={TotalCount}, Duration={Duration}ms",
                requestId, result.SuccessfulUploads.Count, result.FailedUploads.Count, request.Files.Count, elapsed);

            return result;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[AzureBlobStorageService.UploadFilesAsync] ERROR - RequestId={RequestId}, Container={Container}, Duration={Duration}ms",
                requestId, request?.Container ?? "NULL", elapsed);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[AzureBlobStorageService.DeleteFileAsync] START - RequestId={RequestId}, FileId={FileId}",
            requestId, fileId);

        try
        {
            // Find the blob by searching through containers
            // In a production system, you might want to store container info with the file ID
            var containers = _blobServiceClient.GetBlobContainersAsync();
            var foundAndDeleted = false;
            var containerCount = 0;

            await foreach (var container in containers)
            {
                containerCount++;
                _logger.LogInformation("[AzureBlobStorageService.DeleteFileAsync] Searching container {ContainerCount} - RequestId={RequestId}, Container={Container}",
                    containerCount, requestId, container.Name);

                var containerClient = _blobServiceClient.GetBlobContainerClient(container.Name);
                var blobs = containerClient.GetBlobsAsync(prefix: fileId);
                
                await foreach (var blob in blobs)
                {
                    if (blob.Metadata?.ContainsKey("file_id") == true && 
                        blob.Metadata["file_id"] == fileId)
                    {
                        _logger.LogInformation("[AzureBlobStorageService.DeleteFileAsync] File found - RequestId={RequestId}, FileId={FileId}, BlobName={BlobName}, Container={Container}",
                            requestId, fileId, blob.Name, container.Name);

                        var blobClient = containerClient.GetBlobClient(blob.Name);
                        await blobClient.DeleteIfExistsAsync();
                        
                        _logger.LogInformation("[AzureBlobStorageService.DeleteFileAsync] File deleted - RequestId={RequestId}, FileId={FileId}, BlobName={BlobName}",
                            requestId, fileId, blob.Name);

                        foundAndDeleted = true;
                        break;
                    }
                }

                if (foundAndDeleted)
                    break;
            }

            if (!foundAndDeleted)
            {
                _logger.LogWarning("[AzureBlobStorageService.DeleteFileAsync] File not found - RequestId={RequestId}, FileId={FileId}, ContainersSearched={ContainersSearched}",
                    requestId, fileId, containerCount);
                return false;
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[AzureBlobStorageService.DeleteFileAsync] SUCCESS - RequestId={RequestId}, FileId={FileId}, Duration={Duration}ms",
                requestId, fileId, elapsed);

            return true;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[AzureBlobStorageService.DeleteFileAsync] ERROR - RequestId={RequestId}, FileId={FileId}, Duration={Duration}ms",
                requestId, fileId, elapsed);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string fileId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[AzureBlobStorageService.FileExistsAsync] START - RequestId={RequestId}, FileId={FileId}",
            requestId, fileId);

        try
        {
            var containers = _blobServiceClient.GetBlobContainersAsync();
            var containerCount = 0;
            
            await foreach (var container in containers)
            {
                containerCount++;
                var containerClient = _blobServiceClient.GetBlobContainerClient(container.Name);
                var blobs = containerClient.GetBlobsAsync(prefix: fileId);
                
                await foreach (var blob in blobs)
                {
                    if (blob.Metadata?.ContainsKey("file_id") == true && 
                        blob.Metadata["file_id"] == fileId)
                    {
                        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                        _logger.LogInformation("[AzureBlobStorageService.FileExistsAsync] SUCCESS - File found - RequestId={RequestId}, FileId={FileId}, Container={Container}, Duration={Duration}ms",
                            requestId, fileId, container.Name, elapsed);

                        return true;
                    }
                }
            }

            var elapsedNotFound = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[AzureBlobStorageService.FileExistsAsync] SUCCESS - File not found - RequestId={RequestId}, FileId={FileId}, ContainersSearched={ContainersSearched}, Duration={Duration}ms",
                requestId, fileId, containerCount, elapsedNotFound);

            return false;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[AzureBlobStorageService.FileExistsAsync] ERROR - RequestId={RequestId}, FileId={FileId}, Duration={Duration}ms",
                requestId, fileId, elapsed);
            throw;
        }
    }

    public async Task<string> GetFileUrlAsync(string fileId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[AzureBlobStorageService.GetFileUrlAsync] START - RequestId={RequestId}, FileId={FileId}",
            requestId, fileId);

        try
        {
            var containers = _blobServiceClient.GetBlobContainersAsync();
            var containerCount = 0;
            
            _logger.LogInformation("[AzureBlobStorageService.GetFileUrlAsync] Starting container search - RequestId={RequestId}, FileId={FileId}",
                requestId, fileId);
            
            await foreach (var container in containers)
            {
                containerCount++;
                _logger.LogInformation("[AzureBlobStorageService.GetFileUrlAsync] Searching container - RequestId={RequestId}, FileId={FileId}, Container={Container}",
                    requestId, fileId, container.Name);
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(container.Name);
                
                // ⭐ Generate the correct blob prefix based on namespace configuration
                // This matches the logic in GenerateBlobName()
                var searchPrefix = _config.UseHierarchicalNamespace 
                    ? $"posts/{fileId}"  // Hierarchical: "posts/abc123"
                    : fileId;            // Flat: "abc123"
                
                _logger.LogInformation("[AzureBlobStorageService.GetFileUrlAsync] Using search prefix - RequestId={RequestId}, Prefix={Prefix}, UseHierarchicalNamespace={UseHierarchical}",
                    requestId, searchPrefix, _config.UseHierarchicalNamespace);
                
                // ⭐ CRITICAL: Request metadata with BlobTraits.Metadata
                var blobs = containerClient.GetBlobsAsync(traits: BlobTraits.Metadata, prefix: searchPrefix);
                
                var blobCount = 0;
                await foreach (var blob in blobs)
                {
                    blobCount++;
                    _logger.LogInformation("[AzureBlobStorageService.GetFileUrlAsync] Found blob with prefix - RequestId={RequestId}, FileId={FileId}, BlobName={BlobName}, HasMetadata={HasMetadata}",
                        requestId, fileId, blob.Name, blob.Metadata != null);
                    
                    if (blob.Metadata?.ContainsKey("file_id") == true)
                    {
                        _logger.LogInformation("[AzureBlobStorageService.GetFileUrlAsync] Blob has file_id metadata - RequestId={RequestId}, FileId={FileId}, MetadataFileId={MetadataFileId}, Match={Match}",
                            requestId, fileId, blob.Metadata["file_id"], blob.Metadata["file_id"] == fileId);
                    }
                    
                    if (blob.Metadata?.ContainsKey("file_id") == true && 
                        blob.Metadata["file_id"] == fileId)
                    {
                        _logger.LogInformation("[AzureBlobStorageService.GetFileUrlAsync] File found - RequestId={RequestId}, FileId={FileId}, BlobName={BlobName}, Container={Container}",
                            requestId, fileId, blob.Name, container.Name);

                        var blobClient = containerClient.GetBlobClient(blob.Name);
                        var originalFileName = blob.Metadata.TryGetValue("original_filename", out var fileName) ? fileName : "unknown";
                        var url = GeneratePublicUrl(blobClient.Uri, container.Name, fileId, originalFileName);

                        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                        _logger.LogInformation("[AzureBlobStorageService.GetFileUrlAsync] SUCCESS - RequestId={RequestId}, FileId={FileId}, OriginalFileName={OriginalFileName}, Duration={Duration}ms",
                            requestId, fileId, originalFileName, elapsed);

                        return url;
                    }
                }
                
                _logger.LogInformation("[AzureBlobStorageService.GetFileUrlAsync] Container search complete - RequestId={RequestId}, FileId={FileId}, Container={Container}, BlobsFound={BlobsFound}",
                    requestId, fileId, container.Name, blobCount);
            }

            _logger.LogWarning("[AzureBlobStorageService.GetFileUrlAsync] File not found - RequestId={RequestId}, FileId={FileId}, ContainersSearched={ContainersSearched}",
                requestId, fileId, containerCount);

            throw new FileNotFoundException($"File with ID {fileId} not found");
        }
        catch (Exception ex) when (!(ex is FileNotFoundException))
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[AzureBlobStorageService.GetFileUrlAsync] ERROR - RequestId={RequestId}, FileId={FileId}, Duration={Duration}ms",
                requestId, fileId, elapsed);
            throw;
        }
    }

    public async Task<BulkDeleteResult> DeleteFilesAsync(IEnumerable<string> fileIds)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var result = new BulkDeleteResult();
        var semaphore = new SemaphoreSlim(_config.MaxConcurrentUploads, _config.MaxConcurrentUploads);
        
        var fileIdList = fileIds.ToList();
        var totalCount = fileIdList.Count;

        _logger.LogInformation("[AzureBlobStorageService.DeleteFilesAsync] START - RequestId={RequestId}, TotalFiles={TotalFiles}, MaxConcurrent={MaxConcurrent}",
            requestId, totalCount, _config.MaxConcurrentUploads);

        var index = 0;
        var tasks = fileIdList.Select(async fileId =>
        {
            var currentIndex = Interlocked.Increment(ref index);
            
            await semaphore.WaitAsync();
            try
            {
                _logger.LogInformation("[AzureBlobStorageService.DeleteFilesAsync] Processing file - RequestId={RequestId}, Index={Index}/{Total}, FileId={FileId}",
                    requestId, currentIndex, totalCount, fileId);

                var deleted = await DeleteFileAsync(fileId);
                
                lock (result)
                {
                    if (deleted)
                    {
                        result.SuccessfulDeletes.Add(fileId);
                        _logger.LogInformation("[AzureBlobStorageService.DeleteFilesAsync] File deleted successfully - RequestId={RequestId}, FileId={FileId}",
                            requestId, fileId);
                    }
                    else
                    {
                        result.FailedDeletes.Add(new FileDeleteError
                        {
                            FileId = fileId,
                            ErrorMessage = "File not found"
                        });
                        _logger.LogWarning("[AzureBlobStorageService.DeleteFilesAsync] File not found - RequestId={RequestId}, FileId={FileId}",
                            requestId, fileId);
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
                
                _logger.LogWarning(ex, "[AzureBlobStorageService.DeleteFilesAsync] Delete failed - RequestId={RequestId}, FileId={FileId}, Error={Error}",
                    requestId, fileId, ex.Message);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation("[AzureBlobStorageService.DeleteFilesAsync] SUCCESS - RequestId={RequestId}, SuccessfulDeletes={SuccessfulDeletes}, FailedDeletes={FailedDeletes}, TotalFiles={TotalFiles}, Duration={Duration}ms",
            requestId, result.SuccessfulDeletes.Count, result.FailedDeletes.Count, totalCount, elapsed);

        return result;
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(string fileId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[AzureBlobStorageService.GetFileMetadataAsync] START - RequestId={RequestId}, FileId={FileId}",
            requestId, fileId);

        try
        {
            var containers = _blobServiceClient.GetBlobContainersAsync();
            var containerCount = 0;
            
            await foreach (var container in containers)
            {
                containerCount++;
                var containerClient = _blobServiceClient.GetBlobContainerClient(container.Name);
                var blobs = containerClient.GetBlobsAsync(prefix: fileId);
                
                await foreach (var blob in blobs)
                {
                    if (blob.Metadata?.ContainsKey("file_id") == true && 
                        blob.Metadata["file_id"] == fileId)
                    {
                        _logger.LogInformation("[AzureBlobStorageService.GetFileMetadataAsync] File found - RequestId={RequestId}, FileId={FileId}, BlobName={BlobName}, Container={Container}",
                            requestId, fileId, blob.Name, container.Name);

                        var metadata = new FileMetadata
                        {
                            FileId = fileId,
                            OriginalFileName = blob.Metadata.TryGetValue("original_filename", out var originalFileName) ? originalFileName : "unknown",
                            ContentType = blob.Properties.ContentType ?? "application/octet-stream",
                            FileSizeBytes = blob.Properties.ContentLength ?? 0,
                            UploadedAt = blob.Properties.CreatedOn?.DateTime ?? DateTime.MinValue
                        };

                        _logger.LogInformation("[AzureBlobStorageService.GetFileMetadataAsync] Metadata retrieved - RequestId={RequestId}, FileId={FileId}, OriginalFileName={OriginalFileName}, SizeBytes={SizeBytes}, ContentType={ContentType}",
                            requestId, fileId, metadata.OriginalFileName, metadata.FileSizeBytes, metadata.ContentType);

                        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                        _logger.LogInformation("[AzureBlobStorageService.GetFileMetadataAsync] SUCCESS - RequestId={RequestId}, FileId={FileId}, Duration={Duration}ms",
                            requestId, fileId, elapsed);

                        return metadata;
                    }
                }
            }

            _logger.LogWarning("[AzureBlobStorageService.GetFileMetadataAsync] File not found - RequestId={RequestId}, FileId={FileId}, ContainersSearched={ContainersSearched}",
                requestId, fileId, containerCount);

            var elapsedNotFound = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[AzureBlobStorageService.GetFileMetadataAsync] SUCCESS - Returning null - RequestId={RequestId}, FileId={FileId}, Duration={Duration}ms",
                requestId, fileId, elapsedNotFound);

            return null;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[AzureBlobStorageService.GetFileMetadataAsync] ERROR - RequestId={RequestId}, FileId={FileId}, Duration={Duration}ms",
                requestId, fileId, elapsed);
            throw;
        }
    }

    #region Private Helper Methods

    private async Task<BlobContainerClient> GetOrCreateContainerAsync(string containerName)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[AzureBlobStorageService.GetOrCreateContainerAsync] START - RequestId={RequestId}, ContainerName={ContainerName}",
            requestId, containerName);

        try
        {
            return await _containerClients.GetOrAddAsync(containerName, async name =>
            {
                _logger.LogInformation("[AzureBlobStorageService.GetOrCreateContainerAsync] Retrieving/creating container - RequestId={RequestId}, LogicalName={LogicalName}",
                    requestId, name);

                var containerClient = _blobServiceClient.GetBlobContainerClient(GetContainerName(name));
                
                if (_config.AutoCreateContainers)
                {
                    _logger.LogInformation("[AzureBlobStorageService.GetOrCreateContainerAsync] Creating container if not exists - RequestId={RequestId}, ContainerClientName={ContainerClientName}",
                        requestId, containerClient.Name);

                    await containerClient.CreateIfNotExistsAsync(_config.DefaultPublicAccessType);
                    
                    _logger.LogDebug("[AzureBlobStorageService.GetOrCreateContainerAsync] Container ensured - RequestId={RequestId}, ContainerName={ContainerName}, PublicAccessType={PublicAccessType}",
                        requestId, containerClient.Name, _config.DefaultPublicAccessType);
                }
                else
                {
                    _logger.LogInformation("[AzureBlobStorageService.GetOrCreateContainerAsync] Auto-create disabled, using existing container - RequestId={RequestId}, ContainerName={ContainerName}",
                        requestId, containerClient.Name);
                }

                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("[AzureBlobStorageService.GetOrCreateContainerAsync] Container ready - RequestId={RequestId}, ContainerName={ContainerName}, Duration={Duration}ms",
                    requestId, containerClient.Name, elapsed);

                return containerClient;
            });
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[AzureBlobStorageService.GetOrCreateContainerAsync] ERROR - RequestId={RequestId}, ContainerName={ContainerName}, Duration={Duration}ms",
                requestId, containerName, elapsed);
            throw;
        }
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
        _logger.LogDebug("[AzureBlobStorageService.GeneratePublicUrl] START - BlobUri={BlobUri}, Container={Container}, FileId={FileId}, FileName={FileName}",
            blobUri, container, fileId, fileName);
        
        // In development, use the blob proxy to avoid CORS issues with Azurite
        // In production, use the configured BaseUrl or blob URI directly
        
        if (!string.IsNullOrEmpty(_config.BaseUrl))
        {
            var url = $"{_config.BaseUrl.TrimEnd('/')}/{container}/{fileId}_{fileName}";
            _logger.LogInformation("[AzureBlobStorageService.GeneratePublicUrl] Using BaseUrl - URL={URL}", url);
            return url;
        }

        // Check if running against Azurite (development)
        if (blobUri.Host.Contains("127.0.0.1") || blobUri.Host.Contains("localhost"))
        {
            // Use blob proxy endpoint to avoid CORS issues
            // Include prefix if using hierarchical namespace
            var blobName = _config.UseHierarchicalNamespace 
                ? $"posts/{fileId}_{fileName}"
                : $"{fileId}_{fileName}";
            var proxyUrl = $"/api/blob-proxy/{container}/{blobName}";
            _logger.LogInformation("[AzureBlobStorageService.GeneratePublicUrl] Detected Azurite, using proxy - Host={Host}, ProxyUrl={ProxyUrl}",
                blobUri.Host, proxyUrl);
            return proxyUrl;
        }

        // Production Azure Blob Storage URL
        var productionUrl = blobUri.ToString();
        _logger.LogInformation("[AzureBlobStorageService.GeneratePublicUrl] Using production blob URL - Host={Host}, URL={URL}",
            blobUri.Host, productionUrl);
        return productionUrl;
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

