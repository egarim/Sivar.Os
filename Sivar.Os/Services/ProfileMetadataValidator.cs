using Sivar.Os.Shared.DTOs.Metadata;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Services;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace Sivar.Os.Services;

/// <summary>
/// Implementation of profile metadata validation service
/// </summary>
public class ProfileMetadataValidator : IProfileMetadataValidator
{
    private readonly ILogger<ProfileMetadataValidator> _logger;
    private readonly Dictionary<string, Dictionary<string, MetadataFieldRule>> _validationRules;

    public ProfileMetadataValidator(ILogger<ProfileMetadataValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validationRules = InitializeValidationRules();
    }

    /// <summary>
    /// Validates metadata JSON string against profile type requirements
    /// </summary>
    public async Task<MetadataValidationResult> ValidateMetadataAsync(string? metadata, ProfileType profileType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(metadata))
            {
                metadata = "{}";
            }

            // Check metadata size limit (50KB max)
            const int maxMetadataSize = 50 * 1024; // 50KB
            if (metadata.Length > maxMetadataSize)
            {
                return MetadataValidationResult.Failure($"Metadata size exceeds maximum allowed size of {maxMetadataSize} bytes");
            }

            // Parse JSON to ensure it's valid
            JsonDocument? jsonDoc = null;
            try
            {
                jsonDoc = JsonDocument.Parse(metadata);
            }
            catch (JsonException ex)
            {
                return MetadataValidationResult.Failure($"Invalid JSON format: {ex.Message}");
            }

            var result = new MetadataValidationResult { IsValid = true };
            var rules = GetValidationRules(profileType);

            if (jsonDoc?.RootElement.ValueKind == JsonValueKind.Object)
            {
                await ValidateJsonObject(jsonDoc.RootElement, rules, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating metadata for profile type {ProfileType}", profileType.Name);
            return MetadataValidationResult.Failure("Internal validation error");
        }
    }

    /// <summary>
    /// Validates strongly-typed personal profile metadata
    /// </summary>
    public Task<MetadataValidationResult> ValidatePersonalMetadataAsync(PersonalProfileMetadataDto metadata)
    {
        var result = new MetadataValidationResult { IsValid = true };

        if (metadata == null)
        {
            return Task.FromResult(MetadataValidationResult.Failure("Personal metadata is required"));
        }

        // Validate skills
        if (metadata.Skills != null && metadata.Skills.Count > 15)
        {
            result.FieldErrors.Add(nameof(metadata.Skills), new List<string> { "Maximum 15 skills allowed" });
        }

        // Validate languages
        if (metadata.Languages != null && metadata.Languages.Count > 10)
        {
            result.FieldErrors.Add(nameof(metadata.Languages), new List<string> { "Maximum 10 languages allowed" });
        }

        // Validate interests string length
        if (!string.IsNullOrEmpty(metadata.Interests) && metadata.Interests.Length > 1000)
        {
            result.FieldErrors.Add(nameof(metadata.Interests), new List<string> { "Interests cannot exceed 1000 characters" });
        }

        // Validate date of birth (must be realistic)
        if (metadata.DateOfBirth.HasValue)
        {
            var age = DateTime.Now.Year - metadata.DateOfBirth.Value.Year;
            if (age < 13 || age > 120)
            {
                result.FieldErrors.Add(nameof(metadata.DateOfBirth), new List<string> { "Age must be between 13 and 120 years" });
            }
        }

        result.IsValid = !result.FieldErrors.Any() && !result.Errors.Any();
        return Task.FromResult(result);
    }

    /// <summary>
    /// Validates strongly-typed business profile metadata
    /// </summary>
    public Task<MetadataValidationResult> ValidateBusinessMetadataAsync(BusinessProfileMetadataDto metadata)
    {
        var result = new MetadataValidationResult { IsValid = true };

        if (metadata == null)
        {
            return Task.FromResult(MetadataValidationResult.Failure("Business metadata is required"));
        }

        // Validate industry (required for business profiles)
        if (string.IsNullOrEmpty(metadata.Industry))
        {
            result.FieldErrors.Add(nameof(metadata.Industry), new List<string> { "Industry is required for business profiles" });
        }

        // Validate services
        if (metadata.Services != null && metadata.Services.Count > 25)
        {
            result.FieldErrors.Add(nameof(metadata.Services), new List<string> { "Maximum 25 services allowed" });
        }

        // Validate products
        if (metadata.Products != null && metadata.Products.Count > 25)
        {
            result.FieldErrors.Add(nameof(metadata.Products), new List<string> { "Maximum 25 products allowed" });
        }

        // Validate certifications
        if (metadata.Certifications != null && metadata.Certifications.Count > 10)
        {
            result.FieldErrors.Add(nameof(metadata.Certifications), new List<string> { "Maximum 10 certifications allowed" });
        }

        // Validate year founded
        if (metadata.YearFounded.HasValue)
        {
            var currentYear = DateTime.Now.Year;
            if (metadata.YearFounded < 1800 || metadata.YearFounded > currentYear)
            {
                result.FieldErrors.Add(nameof(metadata.YearFounded), 
                    new List<string> { $"Year founded must be between 1800 and {currentYear}" });
            }
        }

        result.IsValid = !result.FieldErrors.Any() && !result.Errors.Any();
        return Task.FromResult(result);
    }

    /// <summary>
    /// Validates strongly-typed organization profile metadata
    /// </summary>
    public Task<MetadataValidationResult> ValidateOrganizationMetadataAsync(OrganizationProfileMetadataDto metadata)
    {
        var result = new MetadataValidationResult { IsValid = true };

        if (metadata == null)
        {
            return Task.FromResult(MetadataValidationResult.Failure("Organization metadata is required"));
        }

        // Validate organization type (required)
        if (string.IsNullOrEmpty(metadata.OrganizationType))
        {
            result.FieldErrors.Add(nameof(metadata.OrganizationType), new List<string> { "Organization type is required" });
        }

        // Validate programs
        if (metadata.Programs != null && metadata.Programs.Count > 20)
        {
            result.FieldErrors.Add(nameof(metadata.Programs), new List<string> { "Maximum 20 programs allowed" });
        }

        // Validate leadership team
        if (metadata.Leadership != null && metadata.Leadership.Count > 15)
        {
            result.FieldErrors.Add(nameof(metadata.Leadership), new List<string> { "Maximum 15 leadership members allowed" });
        }

        // Validate values
        if (metadata.Values != null && metadata.Values.Count > 10)
        {
            result.FieldErrors.Add(nameof(metadata.Values), new List<string> { "Maximum 10 organizational values allowed" });
        }

        // Validate year founded
        if (metadata.YearFounded.HasValue)
        {
            var currentYear = DateTime.Now.Year;
            if (metadata.YearFounded < 1800 || metadata.YearFounded > currentYear)
            {
                result.FieldErrors.Add(nameof(metadata.YearFounded), 
                    new List<string> { $"Year founded must be between 1800 and {currentYear}" });
            }
        }

        result.IsValid = !result.FieldErrors.Any() && !result.Errors.Any();
        return Task.FromResult(result);
    }

    /// <summary>
    /// Gets the default metadata template for a profile type
    /// </summary>
    public string GetDefaultMetadataTemplate(ProfileType profileType)
    {
        return profileType.Name.ToLower() switch
        {
            "personalprofile" => JsonSerializer.Serialize(new PersonalProfileMetadataDto(), new JsonSerializerOptions { WriteIndented = true }),
            "businessprofile" => JsonSerializer.Serialize(new BusinessProfileMetadataDto(), new JsonSerializerOptions { WriteIndented = true }),
            "organizationprofile" => JsonSerializer.Serialize(new OrganizationProfileMetadataDto(), new JsonSerializerOptions { WriteIndented = true }),
            _ => "{}"
        };
    }

    /// <summary>
    /// Checks if a field is required for a specific profile type
    /// </summary>
    public bool IsFieldRequired(string fieldName, ProfileType profileType)
    {
        var rules = GetValidationRules(profileType);
        return rules.ContainsKey(fieldName) && rules[fieldName].IsRequired;
    }

    /// <summary>
    /// Gets validation rules for a specific profile type
    /// </summary>
    public Dictionary<string, MetadataFieldRule> GetValidationRules(ProfileType profileType)
    {
        var profileTypeName = profileType.Name.ToLower();
        return _validationRules.ContainsKey(profileTypeName) 
            ? _validationRules[profileTypeName] 
            : new Dictionary<string, MetadataFieldRule>();
    }

    #region Private Helper Methods

    private async Task ValidateJsonObject(JsonElement jsonElement, Dictionary<string, MetadataFieldRule> rules, MetadataValidationResult result)
    {
        // Check required fields
        foreach (var rule in rules.Where(r => r.Value.IsRequired))
        {
            if (!jsonElement.TryGetProperty(rule.Key, out _))
            {
                if (!result.FieldErrors.ContainsKey(rule.Key))
                    result.FieldErrors[rule.Key] = new List<string>();
                
                result.FieldErrors[rule.Key].Add($"Field '{rule.Key}' is required");
                result.IsValid = false;
            }
        }

        // Validate existing fields
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (rules.ContainsKey(property.Name))
            {
                await ValidateFieldValue(property.Name, property.Value, rules[property.Name], result);
            }
        }
    }

    private Task ValidateFieldValue(string fieldName, JsonElement value, MetadataFieldRule rule, MetadataValidationResult result)
    {
        var errors = new List<string>();

        switch (rule.FieldType)
        {
            case MetadataFieldType.String:
                if (value.ValueKind == JsonValueKind.String)
                {
                    var stringValue = value.GetString() ?? string.Empty;
                    
                    if (rule.MinLength.HasValue && stringValue.Length < rule.MinLength.Value)
                        errors.Add($"Minimum length is {rule.MinLength.Value} characters");
                    
                    if (rule.MaxLength.HasValue && stringValue.Length > rule.MaxLength.Value)
                        errors.Add($"Maximum length is {rule.MaxLength.Value} characters");
                    
                    if (!string.IsNullOrEmpty(rule.Pattern) && !Regex.IsMatch(stringValue, rule.Pattern))
                        errors.Add(rule.ErrorMessage ?? "Invalid format");
                }
                break;

            case MetadataFieldType.Email:
                if (value.ValueKind == JsonValueKind.String && !IsValidEmail(value.GetString()))
                    errors.Add("Invalid email format");
                break;

            case MetadataFieldType.Url:
                if (value.ValueKind == JsonValueKind.String && !IsValidUrl(value.GetString()))
                    errors.Add("Invalid URL format");
                break;

            case MetadataFieldType.Phone:
                if (value.ValueKind == JsonValueKind.String && !IsValidPhone(value.GetString()))
                    errors.Add("Invalid phone number format");
                break;

            case MetadataFieldType.Number:
                if (value.ValueKind != JsonValueKind.Number)
                    errors.Add("Field must be a number");
                else
                {
                    var numberValue = value.GetDecimal();
                    
                    if (rule.MinValue.HasValue && numberValue < rule.MinValue.Value)
                        errors.Add($"Minimum value is {rule.MinValue.Value}");
                    
                    if (rule.MaxValue.HasValue && numberValue > rule.MaxValue.Value)
                        errors.Add($"Maximum value is {rule.MaxValue.Value}");
                }
                break;

            case MetadataFieldType.Boolean:
                if (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False)
                    errors.Add("Field must be a boolean");
                break;

            case MetadataFieldType.Array:
                if (value.ValueKind != JsonValueKind.Array)
                    errors.Add("Field must be an array");
                else
                {
                    var arrayLength = value.GetArrayLength();
                    
                    // Apply specific limits based on field name
                    if (fieldName.Equals("interests", StringComparison.OrdinalIgnoreCase) && arrayLength > 10)
                        errors.Add("Maximum 10 interests allowed");
                    else if (fieldName.Equals("skills", StringComparison.OrdinalIgnoreCase) && arrayLength > 15)
                        errors.Add("Maximum 15 skills allowed");
                }
                break;
        }

        // Check allowed values
        if (rule.AllowedValues.Any() && value.ValueKind == JsonValueKind.String)
        {
            var stringValue = value.GetString();
            if (!rule.AllowedValues.Contains(stringValue, StringComparer.OrdinalIgnoreCase))
                errors.Add($"Allowed values are: {string.Join(", ", rule.AllowedValues)}");
        }

        if (errors.Any())
        {
            if (!result.FieldErrors.ContainsKey(fieldName))
                result.FieldErrors[fieldName] = new List<string>();
            
            result.FieldErrors[fieldName].AddRange(errors);
            result.IsValid = false;
        }
        
        return Task.CompletedTask;
    }

    private Dictionary<string, Dictionary<string, MetadataFieldRule>> InitializeValidationRules()
    {
        return new Dictionary<string, Dictionary<string, MetadataFieldRule>>
        {
            ["personalprofile"] = new()
            {
                ["email"] = new MetadataFieldRule { FieldName = "email", FieldType = MetadataFieldType.Email, IsRequired = false },
                ["phone"] = new MetadataFieldRule { FieldName = "phone", FieldType = MetadataFieldType.Phone, IsRequired = false },
                ["website"] = new MetadataFieldRule { FieldName = "website", FieldType = MetadataFieldType.Url, IsRequired = false },
                ["interests"] = new MetadataFieldRule { FieldName = "interests", FieldType = MetadataFieldType.Array, IsRequired = false },
                ["skills"] = new MetadataFieldRule { FieldName = "skills", FieldType = MetadataFieldType.Array, IsRequired = false }
            },
            ["businessprofile"] = new()
            {
                ["industry"] = new MetadataFieldRule { FieldName = "industry", FieldType = MetadataFieldType.String, IsRequired = true },
                ["businessEmail"] = new MetadataFieldRule { FieldName = "businessEmail", FieldType = MetadataFieldType.Email, IsRequired = true },
                ["businessPhone"] = new MetadataFieldRule { FieldName = "businessPhone", FieldType = MetadataFieldType.Phone, IsRequired = false },
                ["website"] = new MetadataFieldRule { FieldName = "website", FieldType = MetadataFieldType.Url, IsRequired = false },
                ["companySize"] = new MetadataFieldRule { FieldName = "companySize", FieldType = MetadataFieldType.String, IsRequired = false },
                ["foundedYear"] = new MetadataFieldRule { FieldName = "foundedYear", FieldType = MetadataFieldType.Number, IsRequired = false }
            },
            ["organizationprofile"] = new()
            {
                ["organizationType"] = new MetadataFieldRule { FieldName = "organizationType", FieldType = MetadataFieldType.String, IsRequired = true },
                ["contactEmail"] = new MetadataFieldRule { FieldName = "contactEmail", FieldType = MetadataFieldType.Email, IsRequired = false },
                ["contactPhone"] = new MetadataFieldRule { FieldName = "contactPhone", FieldType = MetadataFieldType.Phone, IsRequired = false },
                ["website"] = new MetadataFieldRule { FieldName = "website", FieldType = MetadataFieldType.Url, IsRequired = false },
                ["missionStatement"] = new MetadataFieldRule { FieldName = "missionStatement", FieldType = MetadataFieldType.String, IsRequired = false },
                ["foundedYear"] = new MetadataFieldRule { FieldName = "foundedYear", FieldType = MetadataFieldType.Number, IsRequired = false }
            }
        };
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone)) return false;
        
        // Basic phone validation - allows various formats
        return Regex.IsMatch(phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", ""), 
                           @"^[\+]?[1-9][\d]{9,14}$");
    }

    private static bool IsValidBusinessHours(string? hours)
    {
        if (string.IsNullOrEmpty(hours)) return true; // Optional field
        
        // Format: "09:00-17:00" or "Closed"
        if (hours.Equals("Closed", StringComparison.OrdinalIgnoreCase)) return true;
        
        var hoursPattern = @"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]-([0-1]?[0-9]|2[0-3]):[0-5][0-9]$";
        return Regex.IsMatch(hours, hoursPattern);
    }

    #endregion
}