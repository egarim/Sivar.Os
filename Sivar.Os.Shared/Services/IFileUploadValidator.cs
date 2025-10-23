
namespace Sivar.Os.Shared.Services;

/// <summary>
/// Interface for validating file uploads
/// </summary>
public interface IFileUploadValidator
{
    /// <summary>
    /// Validate a single file upload request
    /// </summary>
    /// <param name="request">File upload request</param>
    /// <param name="container">Target container</param>
    /// <returns>Validation result</returns>
    Task<FileValidationResult> ValidateFileAsync(FileUploadRequest request, string container);
    
    /// <summary>
    /// Validate a bulk file upload request
    /// </summary>
    /// <param name="request">Bulk upload request</param>
    /// <returns>Bulk validation result</returns>
    Task<BulkFileValidationResult> ValidateFilesAsync(BulkFileUploadRequest request);
}

/// <summary>
/// Result of file validation
/// </summary>
public class FileValidationResult
{
    /// <summary>
    /// True if validation passed
    /// </summary>
    public bool IsValid => !Errors.Any();
    
    /// <summary>
    /// Collection of validation errors
    /// </summary>
    public IList<string> Errors { get; set; } = new List<string>();
    
    /// <summary>
    /// Add an error message
    /// </summary>
    /// <param name="error">Error message</param>
    public void AddError(string error)
    {
        Errors.Add(error);
    }
}

/// <summary>
/// Result of bulk file validation
/// </summary>
public class BulkFileValidationResult
{
    /// <summary>
    /// True if all files passed validation
    /// </summary>
    public bool IsValid => !GeneralErrors.Any() && !FileErrors.Any();
    
    /// <summary>
    /// General validation errors (e.g., too many files, total size too large)
    /// </summary>
    public IList<string> GeneralErrors { get; set; } = new List<string>();
    
    /// <summary>
    /// Validation errors specific to individual files
    /// </summary>
    public IList<FileUploadValidationError> FileErrors { get; set; } = new List<FileUploadValidationError>();
    
    /// <summary>
    /// Add a general validation error
    /// </summary>
    /// <param name="error">Error message</param>
    public void AddError(string error)
    {
        GeneralErrors.Add(error);
    }
    
    /// <summary>
    /// Get all validation errors as a flat list
    /// </summary>
    /// <returns>All error messages</returns>
    public IEnumerable<string> GetAllErrors()
    {
        return GeneralErrors.Concat(FileErrors.SelectMany(fe => fe.Errors));
    }
}

/// <summary>
/// Validation error for a specific file
/// </summary>
public class FileUploadValidationError
{
    /// <summary>
    /// Index of the file in the request
    /// </summary>
    public int FileIndex { get; set; }
    
    /// <summary>
    /// Name of the file
    /// </summary>
    public required string FileName { get; set; }
    
    /// <summary>
    /// Validation errors for this file
    /// </summary>
    public IList<string> Errors { get; set; } = new List<string>();
}