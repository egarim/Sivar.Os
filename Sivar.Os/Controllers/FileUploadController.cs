using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for file upload and management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FileUploadController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(IFileStorageService fileStorageService, ILogger<FileUploadController> logger)
    {
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Upload a single file
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="container">Optional container name (default: "uploads")</param>
    /// <returns>File upload result with URL and metadata</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string container = "uploads")
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        // Validate file size (10MB limit)
        const long maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest("File size exceeds 10MB limit");
        }

        // Validate file type (basic check)
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "application/pdf", "text/plain" };
        if (!allowedTypes.Contains(file.ContentType))
        {
            return BadRequest($"File type {file.ContentType} is not allowed");
        }

        try
        {
            var request = new FileUploadRequest
            {
                FileName = file.FileName,
                FileStream = file.OpenReadStream(),
                Container = container,
                ContentType = file.ContentType,
                Metadata = new Dictionary<string, string>
                {
                    ["uploaded_by"] = "api_user", // TODO: Get from authentication
                    ["uploaded_at"] = DateTime.UtcNow.ToString("O"),
                    ["original_name"] = file.FileName
                }
            };

            var result = await _fileStorageService.UploadFileAsync(request);

            return Ok(new
            {
                fileId = result.FileId,
                url = result.Url,
                container = result.Container,
                fileSizeBytes = result.FileSizeBytes,
                uploadedAt = result.UploadedAt,
                originalFileName = result.OriginalFileName,
                contentType = result.ContentType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName}", file.FileName);
            return StatusCode(500, "Internal server error during file upload");
        }
    }

    /// <summary>
    /// Get file metadata by file ID
    /// </summary>
    /// <param name="fileId">The file ID</param>
    /// <returns>File metadata</returns>
    [HttpGet("metadata/{fileId}")]
    public async Task<IActionResult> GetFileMetadata(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return BadRequest("File ID is required");
        }

        try
        {
            var metadata = await _fileStorageService.GetFileMetadataAsync(fileId);
            if (metadata == null)
            {
                return NotFound("File not found");
            }

            return Ok(new
            {
                fileId = metadata.FileId,
                originalFileName = metadata.OriginalFileName,
                contentType = metadata.ContentType,
                fileSizeBytes = metadata.FileSizeBytes,
                uploadedAt = metadata.UploadedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for file {FileId}", fileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a file by file ID
    /// </summary>
    /// <param name="fileId">The file ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{fileId}")]
    public async Task<IActionResult> DeleteFile(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return BadRequest("File ID is required");
        }

        try
        {
            var deleted = await _fileStorageService.DeleteFileAsync(fileId);
            if (!deleted)
            {
                return NotFound("File not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileId}", fileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Check if a file exists
    /// </summary>
    /// <param name="fileId">The file ID</param>
    /// <returns>Existence status</returns>
    [HttpHead("{fileId}")]
    public async Task<IActionResult> FileExists(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return BadRequest();
        }

        try
        {
            var exists = await _fileStorageService.FileExistsAsync(fileId);
            return exists ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of file {FileId}", fileId);
            return StatusCode(500);
        }
    }
}