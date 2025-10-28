using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(ILogger<ValidationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public DetailedValidationResult ValidateObject(object obj)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ValidationService.ValidateObject] START - RequestId={RequestId}, Timestamp={Timestamp}, ObjectType={ObjectType}",
            requestId, startTime, obj?.GetType().Name ?? "null");

        try
        {
            // Validate input
            if (obj == null)
            {
                _logger.LogError("[ValidationService.ValidateObject] VALIDATION ERROR - RequestId={RequestId}, ObjectNull=true",
                    requestId);
                throw new ArgumentNullException(nameof(obj), "Object to validate cannot be null");
            }

            _logger.LogDebug("[ValidationService.ValidateObject] Input validation passed - RequestId={RequestId}, ObjectType={ObjectType}",
                requestId, obj.GetType().Name);

            var context = new ValidationContext(obj);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = Validator.TryValidateObject(obj, context, results, validateAllProperties: true);

            var errorCount = results.Count;
            _logger.LogInformation("[ValidationService.ValidateObject] Validation completed - RequestId={RequestId}, IsValid={IsValid}, ErrorCount={ErrorCount}",
                requestId, isValid, errorCount);

            if (!isValid)
            {
                foreach (var error in results)
                {
                    var fieldName = error.MemberNames.FirstOrDefault() ?? "General";
                    _logger.LogWarning("[ValidationService.ValidateObject] Validation error - RequestId={RequestId}, Field={Field}, Message={Message}",
                        requestId, fieldName, error.ErrorMessage);
                }
            }

            var errorDict = results.ToDictionary(
                r => r.MemberNames.FirstOrDefault() ?? "General",
                r => r.ErrorMessage ?? "Validation error"
            );

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ValidationService.ValidateObject] SUCCESS - RequestId={RequestId}, IsValid={IsValid}, ErrorCount={ErrorCount}, Duration={Duration}ms",
                requestId, isValid, errorCount, elapsed);

            return new DetailedValidationResult
            {
                IsValid = isValid,
                Errors = errorDict
            };
        }
        catch (ArgumentNullException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ValidationService.ValidateObject] VALIDATION ERROR - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
            throw;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ValidationService.ValidateObject] EXCEPTION - RequestId={RequestId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, ex.GetType().Name, elapsed);
            throw;
        }
    }

    public DetailedValidationResult ValidateProperty(object obj, string propertyName, object? value)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ValidationService.ValidateProperty] START - RequestId={RequestId}, Timestamp={Timestamp}, ObjectType={ObjectType}, PropertyName={PropertyName}",
            requestId, startTime, obj?.GetType().Name ?? "null", propertyName);

        try
        {
            // Validate inputs
            if (obj == null)
            {
                _logger.LogError("[ValidationService.ValidateProperty] VALIDATION ERROR - RequestId={RequestId}, ObjectNull=true",
                    requestId);
                throw new ArgumentNullException(nameof(obj), "Object to validate cannot be null");
            }

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                _logger.LogError("[ValidationService.ValidateProperty] VALIDATION ERROR - RequestId={RequestId}, PropertyNameNull=true",
                    requestId);
                throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
            }

            _logger.LogDebug("[ValidationService.ValidateProperty] Input validation passed - RequestId={RequestId}, PropertyName={PropertyName}",
                requestId, propertyName);

            var context = new ValidationContext(obj) { MemberName = propertyName };
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = Validator.TryValidateProperty(value, context, results);

            var errorCount = results.Count;
            _logger.LogInformation("[ValidationService.ValidateProperty] Validation completed - RequestId={RequestId}, PropertyName={PropertyName}, IsValid={IsValid}, ErrorCount={ErrorCount}",
                requestId, propertyName, isValid, errorCount);

            if (!isValid)
            {
                foreach (var error in results)
                {
                    _logger.LogWarning("[ValidationService.ValidateProperty] Property validation error - RequestId={RequestId}, Property={Property}, Message={Message}",
                        requestId, propertyName, error.ErrorMessage);
                }
            }

            var errorDict = results.ToDictionary(
                r => propertyName,
                r => r.ErrorMessage ?? "Validation error"
            );

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ValidationService.ValidateProperty] SUCCESS - RequestId={RequestId}, PropertyName={PropertyName}, IsValid={IsValid}, Duration={Duration}ms",
                requestId, propertyName, isValid, elapsed);

            return new DetailedValidationResult
            {
                IsValid = isValid,
                Errors = errorDict
            };
        }
        catch (ArgumentException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ValidationService.ValidateProperty] VALIDATION ERROR - RequestId={RequestId}, PropertyName={PropertyName}, Duration={Duration}ms",
                requestId, propertyName, elapsed);
            throw;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ValidationService.ValidateProperty] EXCEPTION - RequestId={RequestId}, PropertyName={PropertyName}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, propertyName, ex.GetType().Name, elapsed);
            throw;
        }
    }

    public void ThrowIfInvalid(object obj)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ValidationService.ThrowIfInvalid] START - RequestId={RequestId}, Timestamp={Timestamp}, ObjectType={ObjectType}",
            requestId, startTime, obj?.GetType().Name ?? "null");

        try
        {
            // Validate input
            if (obj == null)
            {
                _logger.LogError("[ValidationService.ThrowIfInvalid] VALIDATION ERROR - RequestId={RequestId}, ObjectNull=true",
                    requestId);
                throw new ArgumentNullException(nameof(obj), "Object to validate cannot be null");
            }

            var result = ValidateObject(obj);
            
            if (!result.IsValid)
            {
                var errorMessage = string.Join("; ", result.Errors.Values);
                _logger.LogError("[ValidationService.ThrowIfInvalid] Validation failed - RequestId={RequestId}, ErrorCount={ErrorCount}, Errors={Errors}",
                    requestId, result.Errors.Count, errorMessage);
                
                throw new ValidationException(errorMessage);
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ValidationService.ThrowIfInvalid] SUCCESS - RequestId={RequestId}, ObjectIsValid=true, Duration={Duration}ms",
                requestId, elapsed);
        }
        catch (ValidationException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ValidationService.ThrowIfInvalid] VALIDATION EXCEPTION - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
            throw;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ValidationService.ThrowIfInvalid] EXCEPTION - RequestId={RequestId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, ex.GetType().Name, elapsed);
            throw;
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
