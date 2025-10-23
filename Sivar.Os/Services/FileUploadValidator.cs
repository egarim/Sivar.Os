using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Sivar.Os.Shared.Configuration;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service for validating file uploads against configured limits and rules
/// </summary>
public class FileUploadValidator : IFileUploadValidator
{
    private readonly FileStorageConfiguration _config;
    private readonly ILogger<FileUploadValidator> _logger;

    public FileUploadValidator(
        IOptions<FileStorageConfiguration> config,
        ILogger<FileUploadValidator> logger)
    {
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FileValidationResult> ValidateFileAsync(FileUploadRequest request, string container)
    {
        var result = new FileValidationResult();
        
        try
        {
            // Get limits for this container
            var limits = GetLimitsForContainer(container);
            
            // Validate file name
            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                result.AddError("File name is required");
            }
            else if (request.FileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                result.AddError("File name contains invalid characters");
            }
            
            // Validate content type
            if (string.IsNullOrWhiteSpace(request.ContentType))
            {
                result.AddError("Content type is required");
            }
            else if (!limits.AllowedMimeTypes.Contains(request.ContentType.ToLowerInvariant()))
            {
                result.AddError($"File type '{request.ContentType}' is not allowed. Allowed types: {string.Join(", ", limits.AllowedMimeTypes)}");
            }
            
            // Validate file size
            if (request.FileStream != null)
            {
                var fileSize = request.FileStream.Length;
                
                if (fileSize <= 0)
                {
                    result.AddError("File is empty");
                }
                else if (fileSize > limits.MaxIndividualFileSizeBytes)
                {
                    var maxSizeMB = limits.MaxIndividualFileSizeBytes / (1024.0 * 1024.0);
                    var fileSizeMB = fileSize / (1024.0 * 1024.0);
                    result.AddError($"File size ({fileSizeMB:F2} MB) exceeds maximum allowed size ({maxSizeMB:F2} MB)");
                }
            }
            else
            {
                result.AddError("File stream is required");
            }
            
            // Validate container name
            if (string.IsNullOrWhiteSpace(container))
            {
                result.AddError("Container name is required");
            }
            else if (container.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                result.AddError("Container name contains invalid characters");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file {FileName}", request.FileName);
            result.AddError($"Validation error: {ex.Message}");
        }
        
        return await Task.FromResult(result);
    }

    public async Task<BulkFileValidationResult> ValidateFilesAsync(BulkFileUploadRequest request)
    {
        var result = new BulkFileValidationResult();
        
        try
        {
            // Get limits for this container
            var limits = GetLimitsForContainer(request.Container);
            
            // Validate container name
            if (string.IsNullOrWhiteSpace(request.Container))
            {
                result.AddError("Container name is required");
                return result;
            }
            
            // Validate file count
            if (request.Files == null || !request.Files.Any())
            {
                result.AddError("At least one file is required");
                return result;
            }
            
            var maxFiles = request.MaxFileCount ?? limits.MaxFilesPerRequest;
            if (request.Files.Count > maxFiles)
            {
                result.AddError($"Too many files. Maximum allowed: {maxFiles}, provided: {request.Files.Count}");
                return result;
            }
            
            // Validate total size
            var totalSize = request.Files.Where(f => f.FileStream != null).Sum(f => f.FileStream!.Length);
            var maxTotalSize = request.MaxTotalSizeBytes ?? limits.MaxTotalRequestSizeBytes;
            
            if (totalSize > maxTotalSize)
            {
                var maxSizeMB = maxTotalSize / (1024.0 * 1024.0);
                var totalSizeMB = totalSize / (1024.0 * 1024.0);
                result.AddError($"Total size ({totalSizeMB:F2} MB) exceeds maximum allowed ({maxSizeMB:F2} MB)");
                return result;
            }
            
            // Validate individual files
            for (int i = 0; i < request.Files.Count; i++)
            {
                var file = request.Files.ElementAt(i);
                var fileValidation = await ValidateFileAsync(file, request.Container);
                
                if (!fileValidation.IsValid)
                {
                    result.FileErrors.Add(new FileUploadValidationError
                    {
                        FileIndex = i,
                        FileName = file.FileName ?? $"File {i + 1}",
                        Errors = fileValidation.Errors.ToList()
                    });
                }
            }
            
            // Check for duplicate file names
            var duplicateNames = request.Files
                .GroupBy(f => f.FileName?.ToLowerInvariant())
                .Where(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key))
                .Select(g => g.Key)
                .ToList();
            
            if (duplicateNames.Any())
            {
                result.AddError($"Duplicate file names detected: {string.Join(", ", duplicateNames)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating bulk file upload request");
            result.AddError($"Validation error: {ex.Message}");
        }
        
        return result;
    }

    #region Private Helper Methods

    private ContainerLimits GetLimitsForContainer(string container)
    {
        // Check if there's a specific configuration for this container
        if (_config.ContainerConfigurations.TryGetValue(container, out var containerConfig))
        {
            return new ContainerLimits
            {
                MaxFilesPerRequest = containerConfig.MaxFilesPerRequest ?? _config.MaxFilesPerRequest,
                MaxTotalRequestSizeBytes = containerConfig.MaxTotalRequestSizeBytes ?? _config.MaxTotalRequestSizeBytes,
                MaxIndividualFileSizeBytes = containerConfig.MaxIndividualFileSizeBytes ?? _config.MaxIndividualFileSizeBytes,
                AllowedMimeTypes = containerConfig.AllowedMimeTypes ?? _config.AllowedMimeTypes
            };
        }
        
        // Return default limits
        return new ContainerLimits
        {
            MaxFilesPerRequest = _config.MaxFilesPerRequest,
            MaxTotalRequestSizeBytes = _config.MaxTotalRequestSizeBytes,
            MaxIndividualFileSizeBytes = _config.MaxIndividualFileSizeBytes,
            AllowedMimeTypes = _config.AllowedMimeTypes
        };
    }

    #endregion
}

/// <summary>
/// Resolved limits for a specific container
/// </summary>
internal class ContainerLimits
{
    public required int MaxFilesPerRequest { get; set; }
    public required long MaxTotalRequestSizeBytes { get; set; }
    public required long MaxIndividualFileSizeBytes { get; set; }
    public required HashSet<string> AllowedMimeTypes { get; set; }
}