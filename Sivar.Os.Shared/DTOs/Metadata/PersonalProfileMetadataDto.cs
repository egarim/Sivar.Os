using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.DTOs.Metadata;

/// <summary>
/// Metadata DTO for Personal Profile type
/// </summary>
public class PersonalProfileMetadataDto
{
    /// <summary>
    /// Personal interests and hobbies
    /// </summary>
    [StringLength(1000, ErrorMessage = "Interests cannot exceed 1000 characters")]
    public string Interests { get; set; } = string.Empty;

    /// <summary>
    /// Professional skills
    /// </summary>
    public List<string> Skills { get; set; } = new();

    /// <summary>
    /// Educational background
    /// </summary>
    [StringLength(500, ErrorMessage = "Education cannot exceed 500 characters")]
    public string Education { get; set; } = string.Empty;

    /// <summary>
    /// Current occupation or job title
    /// </summary>
    [StringLength(200, ErrorMessage = "Occupation cannot exceed 200 characters")]
    public string Occupation { get; set; } = string.Empty;

    /// <summary>
    /// Company or organization name
    /// </summary>
    [StringLength(200, ErrorMessage = "Company cannot exceed 200 characters")]
    public string Company { get; set; } = string.Empty;

    /// <summary>
    /// Date of birth (optional, for age-appropriate content)
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Relationship status
    /// </summary>
    [StringLength(50, ErrorMessage = "Relationship status cannot exceed 50 characters")]
    public string RelationshipStatus { get; set; } = string.Empty;

    /// <summary>
    /// Languages spoken
    /// </summary>
    public List<string> Languages { get; set; } = new();

    /// <summary>
    /// Personal motto or quote
    /// </summary>
    [StringLength(300, ErrorMessage = "Motto cannot exceed 300 characters")]
    public string Motto { get; set; } = string.Empty;

    /// <summary>
    /// Privacy preferences
    /// </summary>
    public PersonalPrivacyPreferences Privacy { get; set; } = new();
}

/// <summary>
/// Privacy preferences for personal profiles
/// </summary>
public class PersonalPrivacyPreferences
{
    /// <summary>
    /// Whether to show age publicly
    /// </summary>
    public bool ShowAge { get; set; } = false;

    /// <summary>
    /// Whether to show occupation publicly
    /// </summary>
    public bool ShowOccupation { get; set; } = true;

    /// <summary>
    /// Whether to show education publicly
    /// </summary>
    public bool ShowEducation { get; set; } = true;

    /// <summary>
    /// Whether to show relationship status publicly
    /// </summary>
    public bool ShowRelationshipStatus { get; set; } = false;
}