using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Sivar.Os.Shared.Services;
using System.Text.Json;

namespace Sivar.Os.Controllers;

/// <summary>
/// Controller for file upload and management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileService;
    private readonly IFileUploadValidator _validator;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IFileStorageService fileService,
        IFileUploadValidator validator,
        ILogger<FilesController> logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Upload a single file
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="container">Storage container name (e.g., "profile-avatars", "post-attachments")</param>
    /// <param name="metadata">Optional metadata as JSON string</param>
    /// <returns>File upload result</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FileUploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FileUploadResult>> UploadFile(
        IFormFile file,
        [FromForm] string container = "general",
        [FromForm] string? metadata = null)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[FilesController.UploadFile] START - RequestId={RequestId}, FileName={FileName}, Size={Size}, Container={Container}", 
            requestId, file?.FileName, file?.Length, container);

        try
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("[FilesController.UploadFile] BAD_REQUEST - No file or empty, RequestId={RequestId}", requestId);
                return BadRequest(new { Error = "No file provided or file is empty" });
            }

            // Parse metadata if provided
            var metadataDict = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(metadata))
            {
                try
                {
                    metadataDict = JsonSerializer.Deserialize<Dictionary<string, string>>(metadata) 
                                   ?? new Dictionary<string, string>();
                    _logger.LogInformation("[FilesController.UploadFile] Metadata parsed - Count={Count}, RequestId={RequestId}", 
                        metadataDict.Count, requestId);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[FilesController.UploadFile] INVALID_METADATA - RequestId={RequestId}", requestId);
                    return BadRequest(new { Error = $"Invalid metadata JSON: {ex.Message}" });
                }
            }

            // Create upload request
            var request = new FileUploadRequest
            {
                FileStream = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Container = container,
                Metadata = metadataDict
            };

            // Validate the request
            _logger.LogInformation("[FilesController.UploadFile] Validating file - RequestId={RequestId}", requestId);
            var validation = await _validator.ValidateFileAsync(request, container);
            if (!validation.IsValid)
            {
                _logger.LogWarning("[FilesController.UploadFile] VALIDATION_FAILED - Errors={Errors}, RequestId={RequestId}", 
                    string.Join(", ", validation.Errors), requestId);
                return BadRequest(new { Errors = validation.Errors });
            }

            // Reset stream position if possible
            if (request.FileStream.CanSeek)
            {
                request.FileStream.Position = 0;
            }
            
            // Upload the file
            _logger.LogInformation("[FilesController.UploadFile] Uploading to storage - RequestId={RequestId}", requestId);
            var result = await _fileService.UploadFileAsync(request);
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[FilesController.UploadFile] SUCCESS - FileId={FileId}, FileName={FileName}, RequestId={RequestId}, Duration={Duration}ms", 
                result.FileId, file.FileName, requestId, elapsed);

            return Ok(result);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FilesController.UploadFile] ERROR - FileName={FileName}, RequestId={RequestId}, Duration={Duration}ms", 
                file?.FileName, requestId, elapsed);
            return StatusCode(500, new { Error = "Internal server error occurred while uploading file" });
        }
    }

    /// <summary>
    /// Upload multiple files in a single request
    /// </summary>
    /// <param name="files">The files to upload</param>
    /// <param name="container">Storage container name</param>
    /// <param name="metadata">Optional common metadata as JSON string</param>
    /// <param name="maxFiles">Override max files limit for this request</param>
    /// <param name="maxTotalSizeMB">Override max total size limit (in MB) for this request</param>
    /// <returns>Bulk upload result</returns>
    [HttpPost("upload-bulk")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BulkFileUploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BulkFileUploadResult), StatusCodes.Status207MultiStatus)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BulkFileUploadResult>> UploadFiles(
        IFormFileCollection files,
        [FromForm] string container = "general",
        [FromForm] string? metadata = null,
        [FromForm] int? maxFiles = null,
        [FromForm] double? maxTotalSizeMB = null)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { Error = "No files provided" });
            }

            // Parse metadata if provided
            var metadataDict = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(metadata))
            {
                try
                {
                    metadataDict = JsonSerializer.Deserialize<Dictionary<string, string>>(metadata) 
                                   ?? new Dictionary<string, string>();
                }
                catch (JsonException ex)
                {
                    return BadRequest(new { Error = $"Invalid metadata JSON: {ex.Message}" });
                }
            }

            // Create bulk upload request
            var request = new BulkFileUploadRequest
            {
                Container = container,
                CommonMetadata = metadataDict,
                MaxFileCount = maxFiles,
                MaxTotalSizeBytes = maxTotalSizeMB.HasValue ? (long)(maxTotalSizeMB.Value * 1024 * 1024) : null,
                Files = files.Where(f => f.Length > 0).Select(f => new FileUploadRequest
                {
                    FileStream = f.OpenReadStream(),
                    FileName = f.FileName,
                    ContentType = f.ContentType,
                    Container = container,
                    Metadata = new Dictionary<string, string>()
                }).ToList()
            };

            // Validate the request
            var validation = await _validator.ValidateFilesAsync(request);
            if (!validation.IsValid)
            {
                return BadRequest(new { 
                    Errors = validation.GetAllErrors().ToList(),
                    GeneralErrors = validation.GeneralErrors,
                    FileErrors = validation.FileErrors
                });
            }

            // Upload files
            var result = await _fileService.UploadFilesAsync(request);
            
            _logger.LogInformation("Bulk upload completed: {SuccessCount} successful, {FailureCount} failed", 
                result.SuccessfulUploads.Count, result.FailedUploads.Count);

            // Return appropriate status code
            if (result.AllSucceeded)
            {
                return Ok(result);
            }
            else if (result.HasPartialFailures)
            {
                // 207 Multi-Status indicates partial success
                return StatusCode(207, result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload files in bulk request");
            return StatusCode(500, new { Error = "Internal server error occurred while uploading files" });
        }
    }

    /// <summary>
    /// Get public URL for a file
    /// </summary>
    /// <param name="fileId">File identifier</param>
    /// <returns>File URL</returns>
    [HttpGet("{fileId}/url")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetFileUrl(string fileId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                return BadRequest(new { Error = "File ID is required" });
            }

            var exists = await _fileService.FileExistsAsync(fileId);
            if (!exists)
            {
                return NotFound(new { Error = $"File with ID {fileId} not found" });
            }

            var url = await _fileService.GetFileUrlAsync(fileId);
            return Ok(new { FileId = fileId, Url = url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get URL for file {FileId}", fileId);
            return StatusCode(500, new { Error = "Internal server error occurred while getting file URL" });
        }
    }

    /// <summary>
    /// Get file metadata
    /// </summary>
    /// <param name="fileId">File identifier</param>
    /// <returns>File metadata</returns>
    [HttpGet("{fileId}/metadata")]
    [ProducesResponseType(typeof(FileMetadata), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FileMetadata>> GetFileMetadata(string fileId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                return BadRequest(new { Error = "File ID is required" });
            }

            var metadata = await _fileService.GetFileMetadataAsync(fileId);
            return Ok(metadata);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { Error = $"File with ID {fileId} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for file {FileId}", fileId);
            return StatusCode(500, new { Error = "Internal server error occurred while getting file metadata" });
        }
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    /// <param name="fileId">File identifier</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{fileId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteFile(string fileId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[FilesController.DeleteFile] START - RequestId={RequestId}, FileId={FileId}", 
            requestId, fileId);

        try
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                _logger.LogWarning("[FilesController.DeleteFile] BAD_REQUEST - Empty FileId, RequestId={RequestId}", requestId);
                return BadRequest(new { Error = "File ID is required" });
            }

            var deleted = await _fileService.DeleteFileAsync(fileId);
            
            if (deleted)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("[FilesController.DeleteFile] SUCCESS - FileId={FileId}, RequestId={RequestId}, Duration={Duration}ms", 
                    fileId, requestId, elapsed);
                return Ok(new { Message = $"File {fileId} deleted successfully" });
            }
            else
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[FilesController.DeleteFile] NOT_FOUND - FileId={FileId}, RequestId={RequestId}, Duration={Duration}ms", 
                    fileId, requestId, elapsed);
                return NotFound(new { Error = $"File with ID {fileId} not found" });
            }
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FilesController.DeleteFile] ERROR - FileId={FileId}, RequestId={RequestId}, Duration={Duration}ms", 
                fileId, requestId, elapsed);
            return StatusCode(500, new { Error = "Internal server error occurred while deleting file" });
        }
    }

    /// <summary>
    /// Delete multiple files
    /// </summary>
    /// <param name="fileIds">Array of file identifiers</param>
    /// <returns>Bulk deletion result</returns>
    [HttpDelete("bulk")]
    [ProducesResponseType(typeof(BulkDeleteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BulkDeleteResult), StatusCodes.Status207MultiStatus)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BulkDeleteResult>> DeleteFiles([FromBody] string[] fileIds)
    {
        try
        {
            if (fileIds == null || fileIds.Length == 0)
            {
                return BadRequest(new { Error = "File IDs are required" });
            }

            var result = await _fileService.DeleteFilesAsync(fileIds);
            
            _logger.LogInformation("Bulk delete completed: {SuccessCount} successful, {FailureCount} failed", 
                result.SuccessfulDeletes.Count, result.FailedDeletes.Count);

            if (result.AllSucceeded)
            {
                return Ok(result);
            }
            else if (result.HasPartialFailures)
            {
                return StatusCode(207, result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete files in bulk request");
            return StatusCode(500, new { Error = "Internal server error occurred while deleting files" });
        }
    }
}