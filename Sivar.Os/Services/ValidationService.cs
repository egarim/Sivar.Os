using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Services;

/// <summary>
/// Service for enhanced validation with detailed error messages
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validate an object and return detailed validation results
    /// </summary>
    DetailedValidationResult ValidateObject(object obj);

    /// <summary>
    /// Validate a specific property of an object
    /// </summary>
    DetailedValidationResult ValidateProperty(object obj, string propertyName, object? value);

    /// <summary>
    /// Check if validation errors exist and throw if they do
    /// </summary>
    void ThrowIfInvalid(object obj);
}

public class ValidationService : IValidationService
{
    public DetailedValidationResult ValidateObject(object obj)
    {
        var context = new ValidationContext(obj);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(obj, context, results, validateAllProperties: true);

        return new DetailedValidationResult
        {
            IsValid = isValid,
            Errors = results.ToDictionary(
                r => r.MemberNames.FirstOrDefault() ?? "General",
                r => r.ErrorMessage ?? "Validation error"
            )
        };
    }

    public DetailedValidationResult ValidateProperty(object obj, string propertyName, object? value)
    {
        var context = new ValidationContext(obj) { MemberName = propertyName };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateProperty(value, context, results);

        return new DetailedValidationResult
        {
            IsValid = isValid,
            Errors = results.ToDictionary(
                r => propertyName,
                r => r.ErrorMessage ?? "Validation error"
            )
        };
    }

    public void ThrowIfInvalid(object obj)
    {
        var result = ValidateObject(obj);
        if (!result.IsValid)
        {
            var errorMessage = string.Join("; ", result.Errors.Values);
            throw new ValidationException(errorMessage);
        }
    }
}

/// <summary>
/// Validation result with detailed error information
/// </summary>
public class DetailedValidationResult
{
    public bool IsValid { get; set; }
    public Dictionary<string, string> Errors { get; set; } = new();
    
    public string GetErrorMessage(string fieldName)
    {
        return Errors.TryGetValue(fieldName, out var message) ? message : string.Empty;
    }

    public bool HasError(string fieldName)
    {
        return Errors.ContainsKey(fieldName);
    }
}
