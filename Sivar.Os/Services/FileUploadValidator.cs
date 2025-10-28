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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[FileUploadValidator.ValidateFileAsync] START - RequestId={RequestId}, FileName={FileName}, Container={Container}, ContentType={ContentType}",
            requestId, request?.FileName, container, request?.ContentType);

        var result = new FileValidationResult();
        
        try
        {
            // Get limits for this container
            var limits = GetLimitsForContainer(container);
            
            _logger.LogInformation("[FileUploadValidator.ValidateFileAsync] Retrieved limits - RequestId={RequestId}, MaxIndividualSize={MaxIndividualSize}MB, AllowedTypes={AllowedTypes}",
                requestId, limits.MaxIndividualFileSizeBytes / (1024.0 * 1024.0), string.Join(", ", limits.AllowedMimeTypes.Take(3)));
            
            // Validate file name
            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                _logger.LogWarning("[FileUploadValidator.ValidateFileAsync] Validation failed - RequestId={RequestId}, Reason=NullOrEmptyFileName, Container={Container}",
                    requestId, container);
                result.AddError("File name is required");
            }
            else if (request.FileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                _logger.LogWarning("[FileUploadValidator.ValidateFileAsync] Validation failed - RequestId={RequestId}, Reason=InvalidCharactersInFileName, FileName={FileName}",
                    requestId, request.FileName);
                result.AddError("File name contains invalid characters");
            }
            else
            {
                _logger.LogInformation("[FileUploadValidator.ValidateFileAsync] File name valid - RequestId={RequestId}, FileName={FileName}",
                    requestId, request.FileName);
            }
            
            // Validate content type
            if (string.IsNullOrWhiteSpace(request.ContentType))
            {
                _logger.LogWarning("[FileUploadValidator.ValidateFileAsync] Validation failed - RequestId={RequestId}, Reason=NullOrEmptyContentType",
                    requestId);
                result.AddError("Content type is required");
            }
            else if (!limits.AllowedMimeTypes.Contains(request.ContentType.ToLowerInvariant()))
            {
                _logger.LogWarning("[FileUploadValidator.ValidateFileAsync] Validation failed - RequestId={RequestId}, Reason=DisallowedContentType, ContentType={ContentType}, Allowed={AllowedTypes}",
                    requestId, request.ContentType, string.Join(", ", limits.AllowedMimeTypes.Take(3)));
                result.AddError($"File type '{request.ContentType}' is not allowed. Allowed types: {string.Join(", ", limits.AllowedMimeTypes)}");
            }
            else
            {
                _logger.LogInformation("[FileUploadValidator.ValidateFileAsync] Content type valid - RequestId={RequestId}, ContentType={ContentType}",
                    requestId, request.ContentType);
            }
            
            // Validate file size
            if (request.FileStream != null)
            {
                var fileSize = request.FileStream.Length;
                _logger.LogInformation("[FileUploadValidator.ValidateFileAsync] File stream present - RequestId={RequestId}, FileSizeBytes={FileSizeBytes}MB",
                    requestId, fileSize / (1024.0 * 1024.0));
                
                if (fileSize <= 0)
                {
                    _logger.LogWarning("[FileUploadValidator.ValidateFileAsync] Validation failed - RequestId={RequestId}, Reason=EmptyFile",
                        requestId);
                    result.AddError("File is empty");
                }
                else if (fileSize > limits.MaxIndividualFileSizeBytes)
                {
                    var maxSizeMB = limits.MaxIndividualFileSizeBytes / (1024.0 * 1024.0);
                    var fileSizeMB = fileSize / (1024.0 * 1024.0);
                    _logger.LogWarning("[FileUploadValidator.ValidateFileAsync] Validation failed - RequestId={RequestId}, Reason=FileSizeExceeded, FileSizeMB={FileSizeMB}, MaxSizeMB={MaxSizeMB}",
                        requestId, fileSizeMB, maxSizeMB);
                    result.AddError($"File size ({fileSizeMB:F2} MB) exceeds maximum allowed size ({maxSizeMB:F2} MB)");
                }
                else
                {
                    _logger.LogInformation("[FileUploadValidator.ValidateFileAsync] File size valid - RequestId={RequestId}, FileSizeMB={FileSizeMB}",
                        requestId, fileSize / (1024.0 * 1024.0));
                }
            }
            else
            {
                _logger.LogWarning("[FileUploadValidator.ValidateFileAsync] Validation failed - RequestId={RequestId}, Reason=NullFileStream",
                    requestId);
                result.AddError("File stream is required");
            }
            
            // Validate container name
            if (string.IsNullOrWhiteSpace(container))
            {
                _logger.LogWarning("[FileUploadValidator.ValidateFileAsync] Validation failed - RequestId={RequestId}, Reason=NullOrEmptyContainer",
                    requestId);
                result.AddError("Container name is required");
            }
            else if (container.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                _logger.LogWarning("[FileUploadValidator.ValidateFileAsync] Validation failed - RequestId={RequestId}, Reason=InvalidCharactersInContainer, Container={Container}",
                    requestId, container);
                result.AddError("Container name contains invalid characters");
            }
            else
            {
                _logger.LogInformation("[FileUploadValidator.ValidateFileAsync] Container name valid - RequestId={RequestId}, Container={Container}",
                    requestId, container);
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[FileUploadValidator.ValidateFileAsync] SUCCESS - RequestId={RequestId}, IsValid={IsValid}, ErrorCount={ErrorCount}, Duration={Duration}ms",
                requestId, result.IsValid, result.Errors.Count, elapsed);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FileUploadValidator.ValidateFileAsync] ERROR - RequestId={RequestId}, FileName={FileName}, Container={Container}, Duration={Duration}ms",
                requestId, request?.FileName, container, elapsed);
            result.AddError($"Validation error: {ex.Message}");
        }
        
        return await Task.FromResult(result);
    }

    public async Task<BulkFileValidationResult> ValidateFilesAsync(BulkFileUploadRequest request)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[FileUploadValidator.ValidateFilesAsync] START - RequestId={RequestId}, FileCount={FileCount}, Container={Container}",
            requestId, request?.Files?.Count ?? 0, request?.Container);

        var result = new BulkFileValidationResult();
        
        try
        {
            // Get limits for this container
            var limits = GetLimitsForContainer(request.Container);
            
            _logger.LogInformation("[FileUploadValidator.ValidateFilesAsync] Retrieved limits - RequestId={RequestId}, MaxFilesPerRequest={MaxFilesPerRequest}, MaxTotalSize={MaxTotalSizeMB}MB",
                requestId, limits.MaxFilesPerRequest, limits.MaxTotalRequestSizeBytes / (1024.0 * 1024.0));
            
            // Validate container name
            if (string.IsNullOrWhiteSpace(request.Container))
            {
                _logger.LogWarning("[FileUploadValidator.ValidateFilesAsync] Validation failed - RequestId={RequestId}, Reason=NullOrEmptyContainer",
                    requestId);
                result.AddError("Container name is required");
                return result;
            }
            
            // Validate file count
            if (request.Files == null || !request.Files.Any())
            {
                _logger.LogWarning("[FileUploadValidator.ValidateFilesAsync] Validation failed - RequestId={RequestId}, Reason=NoFilesProvided",
                    requestId);
                result.AddError("At least one file is required");
                return result;
            }
            
            _logger.LogInformation("[FileUploadValidator.ValidateFilesAsync] Files received - RequestId={RequestId}, FileCount={FileCount}",
                requestId, request.Files.Count);
            
            var maxFiles = request.MaxFileCount ?? limits.MaxFilesPerRequest;
            if (request.Files.Count > maxFiles)
            {
                _logger.LogWarning("[FileUploadValidator.ValidateFilesAsync] Validation failed - RequestId={RequestId}, Reason=TooManyFiles, FileCount={FileCount}, MaxFiles={MaxFiles}",
                    requestId, request.Files.Count, maxFiles);
                result.AddError($"Too many files. Maximum allowed: {maxFiles}, provided: {request.Files.Count}");
                return result;
            }
            
            // Validate total size
            var totalSize = request.Files.Where(f => f.FileStream != null).Sum(f => f.FileStream!.Length);
            var maxTotalSize = request.MaxTotalSizeBytes ?? limits.MaxTotalRequestSizeBytes;
            
            _logger.LogInformation("[FileUploadValidator.ValidateFilesAsync] Total size calculated - RequestId={RequestId}, TotalSizeMB={TotalSizeMB}, MaxTotalSizeMB={MaxTotalSizeMB}",
                requestId, totalSize / (1024.0 * 1024.0), maxTotalSize / (1024.0 * 1024.0));
            
            if (totalSize > maxTotalSize)
            {
                var maxSizeMB = maxTotalSize / (1024.0 * 1024.0);
                var totalSizeMB = totalSize / (1024.0 * 1024.0);
                _logger.LogWarning("[FileUploadValidator.ValidateFilesAsync] Validation failed - RequestId={RequestId}, Reason=TotalSizeExceeded, TotalSizeMB={TotalSizeMB}, MaxSizeMB={MaxSizeMB}",
                    requestId, totalSizeMB, maxSizeMB);
                result.AddError($"Total size ({totalSizeMB:F2} MB) exceeds maximum allowed ({maxSizeMB:F2} MB)");
                return result;
            }
            
            // Validate individual files
            var validFileCount = 0;
            for (int i = 0; i < request.Files.Count; i++)
            {
                var file = request.Files.ElementAt(i);
                
                _logger.LogInformation("[FileUploadValidator.ValidateFilesAsync] Validating individual file - RequestId={RequestId}, Index={Index}/{Total}, FileName={FileName}",
                    requestId, i + 1, request.Files.Count, file.FileName);

                var fileValidation = await ValidateFileAsync(file, request.Container);
                
                if (!fileValidation.IsValid)
                {
                    _logger.LogWarning("[FileUploadValidator.ValidateFilesAsync] File validation failed - RequestId={RequestId}, Index={Index}, FileName={FileName}, ErrorCount={ErrorCount}",
                        requestId, i, file.FileName, fileValidation.Errors.Count);

                    result.FileErrors.Add(new FileUploadValidationError
                    {
                        FileIndex = i,
                        FileName = file.FileName ?? $"File {i + 1}",
                        Errors = fileValidation.Errors.ToList()
                    });
                }
                else
                {
                    validFileCount++;
                    _logger.LogInformation("[FileUploadValidator.ValidateFilesAsync] File validation passed - RequestId={RequestId}, Index={Index}, FileName={FileName}",
                        requestId, i, file.FileName);
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
                _logger.LogWarning("[FileUploadValidator.ValidateFilesAsync] Duplicate file names detected - RequestId={RequestId}, DuplicateCount={DuplicateCount}, Names={Names}",
                    requestId, duplicateNames.Count, string.Join(", ", duplicateNames));
                result.AddError($"Duplicate file names detected: {string.Join(", ", duplicateNames)}");
            }
            else
            {
                _logger.LogInformation("[FileUploadValidator.ValidateFilesAsync] No duplicate file names - RequestId={RequestId}",
                    requestId);
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[FileUploadValidator.ValidateFilesAsync] SUCCESS - RequestId={RequestId}, TotalFiles={TotalFiles}, ValidFiles={ValidFiles}, InvalidFiles={InvalidFiles}, Duration={Duration}ms",
                requestId, request.Files.Count, validFileCount, result.FileErrors.Count, elapsed);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FileUploadValidator.ValidateFilesAsync] ERROR - RequestId={RequestId}, FileCount={FileCount}, Container={Container}, Duration={Duration}ms",
                requestId, request?.Files?.Count ?? 0, request?.Container, elapsed);
            result.AddError($"Validation error: {ex.Message}");
        }
        
        return result;
    }

    #region Private Helper Methods

    private ContainerLimits GetLimitsForContainer(string container)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[FileUploadValidator.GetLimitsForContainer] START - RequestId={RequestId}, Container={Container}",
            requestId, container);

        try
        {
            // Check if there's a specific configuration for this container
            if (_config.ContainerConfigurations.TryGetValue(container, out var containerConfig))
            {
                _logger.LogInformation("[FileUploadValidator.GetLimitsForContainer] Container-specific config found - RequestId={RequestId}, Container={Container}, HasCustomMaxFiles={HasCustomMaxFiles}, HasCustomMaxSize={HasCustomMaxSize}",
                    requestId, container, containerConfig.MaxFilesPerRequest.HasValue, containerConfig.MaxTotalRequestSizeBytes.HasValue);

                var limits = new ContainerLimits
                {
                    MaxFilesPerRequest = containerConfig.MaxFilesPerRequest ?? _config.MaxFilesPerRequest,
                    MaxTotalRequestSizeBytes = containerConfig.MaxTotalRequestSizeBytes ?? _config.MaxTotalRequestSizeBytes,
                    MaxIndividualFileSizeBytes = containerConfig.MaxIndividualFileSizeBytes ?? _config.MaxIndividualFileSizeBytes,
                    AllowedMimeTypes = containerConfig.AllowedMimeTypes ?? _config.AllowedMimeTypes
                };

                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("[FileUploadValidator.GetLimitsForContainer] SUCCESS - Container-specific - RequestId={RequestId}, Container={Container}, MaxFiles={MaxFiles}, MaxTotalSize={MaxTotalSize}MB, Duration={Duration}ms",
                    requestId, container, limits.MaxFilesPerRequest, limits.MaxTotalRequestSizeBytes / (1024.0 * 1024.0), elapsed);

                return limits;
            }
            
            _logger.LogInformation("[FileUploadValidator.GetLimitsForContainer] Using default limits - RequestId={RequestId}, Container={Container}",
                requestId, container);

            // Return default limits
            var defaultLimits = new ContainerLimits
            {
                MaxFilesPerRequest = _config.MaxFilesPerRequest,
                MaxTotalRequestSizeBytes = _config.MaxTotalRequestSizeBytes,
                MaxIndividualFileSizeBytes = _config.MaxIndividualFileSizeBytes,
                AllowedMimeTypes = _config.AllowedMimeTypes
            };

            var defaultElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[FileUploadValidator.GetLimitsForContainer] SUCCESS - Default limits - RequestId={RequestId}, Container={Container}, MaxFiles={MaxFiles}, MaxTotalSize={MaxTotalSize}MB, Duration={Duration}ms",
                requestId, container, defaultLimits.MaxFilesPerRequest, defaultLimits.MaxTotalRequestSizeBytes / (1024.0 * 1024.0), defaultElapsed);

            return defaultLimits;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FileUploadValidator.GetLimitsForContainer] ERROR - RequestId={RequestId}, Container={Container}, Duration={Duration}ms",
                requestId, container, elapsed);
            throw;
        }
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