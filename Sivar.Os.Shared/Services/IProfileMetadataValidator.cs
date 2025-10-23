using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.DTOs.Metadata;
using Sivar.Os.Shared.Entities;


namespace Sivar.Os.Shared.Services;

/// <summary>
/// Interface for validating profile metadata based on profile types
/// </summary>
public interface IProfileMetadataValidator
{
    /// <summary>
    /// Validates metadata JSON string against profile type requirements
    /// </summary>
    /// <param name="metadata">JSON metadata string</param>
    /// <param name="profileType">Profile type entity</param>
    /// <returns>Validation result with any errors</returns>
    Task<MetadataValidationResult> ValidateMetadataAsync(string? metadata, ProfileType profileType);

    /// <summary>
    /// Validates strongly-typed personal profile metadata
    /// </summary>
    /// <param name="metadata">Personal profile metadata</param>
    /// <returns>Validation result with any errors</returns>
    Task<MetadataValidationResult> ValidatePersonalMetadataAsync(PersonalProfileMetadataDto metadata);

    /// <summary>
    /// Validates strongly-typed business profile metadata
    /// </summary>
    /// <param name="metadata">Business profile metadata</param>
    /// <returns>Validation result with any errors</returns>
    Task<MetadataValidationResult> ValidateBusinessMetadataAsync(BusinessProfileMetadataDto metadata);

    /// <summary>
    /// Validates strongly-typed organization profile metadata
    /// </summary>
    /// <param name="metadata">Organization profile metadata</param>
    /// <returns>Validation result with any errors</returns>
    Task<MetadataValidationResult> ValidateOrganizationMetadataAsync(OrganizationProfileMetadataDto metadata);

    /// <summary>
    /// Gets the default metadata template for a profile type
    /// </summary>
    /// <param name="profileType">Profile type entity</param>
    /// <returns>Default metadata JSON template</returns>
    string GetDefaultMetadataTemplate(ProfileType profileType);

    /// <summary>
    /// Checks if a field is required for a specific profile type
    /// </summary>
    /// <param name="fieldName">Field name to check</param>
    /// <param name="profileType">Profile type entity</param>
    /// <returns>True if field is required, false otherwise</returns>
    bool IsFieldRequired(string fieldName, ProfileType profileType);

    /// <summary>
    /// Gets validation rules for a specific profile type
    /// </summary>
    /// <param name="profileType">Profile type entity</param>
    /// <returns>Dictionary of field names and their validation rules</returns>
    Dictionary<string, MetadataFieldRule> GetValidationRules(ProfileType profileType);
}

/// <summary>
/// Validation result for metadata operations
/// </summary>
public class MetadataValidationResult
{
    /// <summary>
    /// Indicates if the validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings (non-blocking)
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Field-specific validation errors
    /// </summary>
    public Dictionary<string, List<string>> FieldErrors { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static MetadataValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors
    /// </summary>
    /// <param name="errors">Validation error messages</param>
    public static MetadataValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };

    /// <summary>
    /// Creates a failed validation result with field-specific errors
    /// </summary>
    /// <param name="fieldErrors">Field-specific validation errors</param>
    public static MetadataValidationResult FieldFailure(Dictionary<string, List<string>> fieldErrors) => new()
    {
        IsValid = false,
        FieldErrors = fieldErrors
    };
}

/// <summary>
/// Metadata field validation rule
/// </summary>
public class MetadataFieldRule
{
    /// <summary>
    /// Field name
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the field is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Data type of the field
    /// </summary>
    public MetadataFieldType FieldType { get; set; }

    /// <summary>
    /// Minimum length for string fields
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Maximum length for string fields
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Minimum value for numeric fields
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// Maximum value for numeric fields
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Regular expression pattern for validation
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Allowed values for enum-like fields
    /// </summary>
    public List<string> AllowedValues { get; set; } = new();

    /// <summary>
    /// Custom validation error message
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Metadata field data types
/// </summary>
public enum MetadataFieldType
{
    String,
    Number,
    Boolean,
    Date,
    Email,
    Url,
    Phone,
    Array,
    Object
}