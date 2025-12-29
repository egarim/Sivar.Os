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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[FileUploadController.UploadFile] START - RequestId={RequestId}, FileName={FileName}, Size={Size} bytes, ContentType={ContentType}, Container={Container}", 
            requestId, file?.FileName, file?.Length ?? 0, file?.ContentType, container);

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("[FileUploadController.UploadFile] BAD_REQUEST - No file provided, RequestId={RequestId}", requestId);
            return BadRequest("No file provided");
        }

        // Validate file size (10MB limit)
        const long maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            _logger.LogWarning("[FileUploadController.UploadFile] FILE_TOO_LARGE - FileName={FileName}, Size={Size} bytes, RequestId={RequestId}", 
                file.FileName, file.Length, requestId);
            return BadRequest("File size exceeds 10MB limit");
        }

        // Validate file type (basic check)
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "application/pdf", "text/plain" };
        if (!allowedTypes.Contains(file.ContentType))
        {
            _logger.LogWarning("[FileUploadController.UploadFile] INVALID_FILE_TYPE - FileName={FileName}, ContentType={ContentType}, RequestId={RequestId}", 
                file.FileName, file.ContentType, requestId);
            return BadRequest($"File type {file.ContentType} is not allowed");
        }

        _logger.LogInformation("[FileUploadController.UploadFile] File validation passed - RequestId={RequestId}", requestId);

        try
        {
            // Read the file into a MemoryStream first to ensure we have all bytes
            // IFormFile streams can have issues with Azure SDK when used directly
            using var memoryStream = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            _logger.LogInformation("[FileUploadController.UploadFile] File copied to memory - RequestId={RequestId}, MemoryStreamLength={Length}", 
                requestId, memoryStream.Length);

            var request = new FileUploadRequest
            {
                FileName = file.FileName,
                FileStream = memoryStream,
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
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[FileUploadController.UploadFile] SUCCESS - FileId={FileId}, Size={Size} bytes, RequestId={RequestId}, Duration={Duration}ms", 
                result.FileId, result.FileSizeBytes, requestId, elapsed);

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
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FileUploadController.UploadFile] ERROR - FileName={FileName}, RequestId={RequestId}, Duration={Duration}ms", 
                file.FileName, requestId, elapsed);
            return StatusCode(500, "Internal server error during file upload");
        }
    }

    /// <summary>
    /// Upload a single image for BlazorMarkdownEditor.
    /// Returns JSON with filePath property (required by EasyMDE/MarkdownEditor).
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <returns>JSON object with filePath containing the uploaded image URL</returns>
    [HttpPost("image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[FileUploadController.UploadImage] START - RequestId={RequestId}, FileName={FileName}, Size={Size} bytes, ContentType={ContentType}", 
            requestId, file?.FileName, file?.Length ?? 0, file?.ContentType);

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("[FileUploadController.UploadImage] BAD_REQUEST - No file provided, RequestId={RequestId}", requestId);
            return BadRequest("No file provided");
        }

        // Validate file size (10MB limit)
        const long maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            _logger.LogWarning("[FileUploadController.UploadImage] FILE_TOO_LARGE - FileName={FileName}, Size={Size} bytes, RequestId={RequestId}", 
                file.FileName, file.Length, requestId);
            return BadRequest("File size exceeds 10MB limit");
        }

        // Validate file type (images only)
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
        {
            _logger.LogWarning("[FileUploadController.UploadImage] INVALID_FILE_TYPE - FileName={FileName}, ContentType={ContentType}, RequestId={RequestId}", 
                file.FileName, file.ContentType, requestId);
            return BadRequest($"File type {file.ContentType} is not allowed. Only images are accepted.");
        }

        try
        {
            // Read the file into a MemoryStream first to ensure we have all bytes
            // IFormFile streams can have issues with Azure SDK when used directly
            using var memoryStream = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            _logger.LogInformation("[FileUploadController.UploadImage] File copied to memory - RequestId={RequestId}, MemoryStreamLength={Length}", 
                requestId, memoryStream.Length);

            var request = new FileUploadRequest
            {
                FileName = file.FileName,
                FileStream = memoryStream,
                Container = "blog-images",
                ContentType = file.ContentType,
                Metadata = new Dictionary<string, string>
                {
                    ["uploaded_by"] = "html_editor",
                    ["uploaded_at"] = DateTime.UtcNow.ToString("O"),
                    ["original_name"] = file.FileName
                }
            };

            var result = await _fileStorageService.UploadFileAsync(request);
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[FileUploadController.UploadImage] SUCCESS - FileId={FileId}, Url={Url}, RequestId={RequestId}, Duration={Duration}ms", 
                result.FileId, result.Url, requestId, elapsed);

            // BlazorMarkdownEditor reads response.Content.ReadAsStringAsync() and uses it as URL
            // Must return plain text URL, not JSON
            return Content(result.Url, "text/plain");
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FileUploadController.UploadImage] ERROR - FileName={FileName}, RequestId={RequestId}, Duration={Duration}ms", 
                file.FileName, requestId, elapsed);
            return StatusCode(500, "Internal server error during image upload");
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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[FileUploadController.GetFileMetadata] START - RequestId={RequestId}, FileId={FileId}", 
            requestId, fileId);

        if (string.IsNullOrWhiteSpace(fileId))
        {
            _logger.LogWarning("[FileUploadController.GetFileMetadata] BAD_REQUEST - Null or empty FileId, RequestId={RequestId}", requestId);
            return BadRequest("File ID is required");
        }

        try
        {
            var metadata = await _fileStorageService.GetFileMetadataAsync(fileId);
            if (metadata == null)
            {
                var elapsedNotFound = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[FileUploadController.GetFileMetadata] FILE_NOT_FOUND - FileId={FileId}, RequestId={RequestId}, Duration={Duration}ms", 
                    fileId, requestId, elapsedNotFound);
                return NotFound("File not found");
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[FileUploadController.GetFileMetadata] SUCCESS - FileId={FileId}, Size={Size} bytes, RequestId={RequestId}, Duration={Duration}ms", 
                fileId, metadata.FileSizeBytes, requestId, elapsed);

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
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FileUploadController.GetFileMetadata] ERROR - FileId={FileId}, RequestId={RequestId}, Duration={Duration}ms", 
                fileId, requestId, elapsed);
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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[FileUploadController.DeleteFile] START - RequestId={RequestId}, FileId={FileId}", 
            requestId, fileId);

        if (string.IsNullOrWhiteSpace(fileId))
        {
            _logger.LogWarning("[FileUploadController.DeleteFile] BAD_REQUEST - Null or empty FileId, RequestId={RequestId}", requestId);
            return BadRequest("File ID is required");
        }

        try
        {
            var deleted = await _fileStorageService.DeleteFileAsync(fileId);
            if (!deleted)
            {
                var elapsedNotFound = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[FileUploadController.DeleteFile] FILE_NOT_FOUND - FileId={FileId}, RequestId={RequestId}, Duration={Duration}ms", 
                    fileId, requestId, elapsedNotFound);
                return NotFound("File not found");
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[FileUploadController.DeleteFile] SUCCESS - FileId={FileId}, RequestId={RequestId}, Duration={Duration}ms", 
                fileId, requestId, elapsed);

            return NoContent();
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FileUploadController.DeleteFile] ERROR - FileId={FileId}, RequestId={RequestId}, Duration={Duration}ms", 
                fileId, requestId, elapsed);
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

    /// <summary>
    /// Upload a base64 encoded image (used by DxHtmlEditor for inline images).
    /// Accepts JSON body with base64Data and contentType.
    /// </summary>
    /// <param name="request">The base64 image upload request</param>
    /// <returns>JSON object with url containing the uploaded image URL</returns>
    [HttpPost("base64-image")]
    public async Task<IActionResult> UploadBase64Image([FromBody] Base64ImageUploadRequest request)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[FileUploadController.UploadBase64Image] START - RequestId={RequestId}, ContentType={ContentType}, DataLength={Length}", 
            requestId, request?.ContentType, request?.Base64Data?.Length ?? 0);

        if (request == null || string.IsNullOrWhiteSpace(request.Base64Data))
        {
            _logger.LogWarning("[FileUploadController.UploadBase64Image] BAD_REQUEST - No data provided, RequestId={RequestId}", requestId);
            return BadRequest(new { error = "No image data provided" });
        }

        // Validate content type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (string.IsNullOrWhiteSpace(request.ContentType) || !allowedTypes.Contains(request.ContentType))
        {
            _logger.LogWarning("[FileUploadController.UploadBase64Image] INVALID_TYPE - ContentType={ContentType}, RequestId={RequestId}", 
                request.ContentType, requestId);
            return BadRequest(new { error = $"Invalid content type. Allowed: {string.Join(", ", allowedTypes)}" });
        }

        try
        {
            // Remove data URI prefix if present (e.g., "data:image/png;base64,")
            var base64Data = request.Base64Data;
            if (base64Data.Contains(","))
            {
                base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
            }

            // Decode base64 to bytes
            byte[] imageBytes;
            try
            {
                imageBytes = Convert.FromBase64String(base64Data);
            }
            catch (FormatException)
            {
                _logger.LogWarning("[FileUploadController.UploadBase64Image] INVALID_BASE64 - RequestId={RequestId}", requestId);
                return BadRequest(new { error = "Invalid base64 data" });
            }

            // Validate file size (10MB limit)
            const long maxFileSize = 10 * 1024 * 1024;
            if (imageBytes.Length > maxFileSize)
            {
                _logger.LogWarning("[FileUploadController.UploadBase64Image] FILE_TOO_LARGE - Size={Size} bytes, RequestId={RequestId}", 
                    imageBytes.Length, requestId);
                return BadRequest(new { error = "Image size exceeds 10MB limit" });
            }

            // Generate filename with proper extension
            var extension = request.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".png"
            };
            var fileName = $"blog-inline-{Guid.NewGuid():N}{extension}";

            using var memoryStream = new MemoryStream(imageBytes);

            var uploadRequest = new FileUploadRequest
            {
                FileName = fileName,
                FileStream = memoryStream,
                Container = "blog-images",
                ContentType = request.ContentType,
                Metadata = new Dictionary<string, string>
                {
                    ["uploaded_by"] = "html_editor_base64",
                    ["uploaded_at"] = DateTime.UtcNow.ToString("O"),
                    ["original_size"] = imageBytes.Length.ToString()
                }
            };

            var result = await _fileStorageService.UploadFileAsync(uploadRequest);
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[FileUploadController.UploadBase64Image] SUCCESS - FileId={FileId}, Url={Url}, RequestId={RequestId}, Duration={Duration}ms", 
                result.FileId, result.Url, requestId, elapsed);

            return Ok(new
            {
                url = result.Url,
                fileId = result.FileId,
                fileSizeBytes = result.FileSizeBytes
            });
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FileUploadController.UploadBase64Image] ERROR - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
            return StatusCode(500, new { error = "Internal server error during image upload" });
        }
    }
}

/// <summary>
/// Request model for uploading base64 encoded images
/// </summary>
public class Base64ImageUploadRequest
{
    /// <summary>
    /// The base64 encoded image data (may include data URI prefix)
    /// </summary>
    public string Base64Data { get; set; } = string.Empty;

    /// <summary>
    /// The content type of the image (e.g., "image/png", "image/jpeg")
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}