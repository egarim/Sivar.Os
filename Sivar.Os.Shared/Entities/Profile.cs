using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Sivar.Os.Shared.DTOs.ValueObjects;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a user's profile with PersonalProfile fields
/// </summary>
public class Profile : BaseEntity
{
    /// <summary>
    /// The user who owns this profile
    /// </summary>
    public virtual Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// The type of this profile
    /// </summary>
    public virtual Guid ProfileTypeId { get; set; }
    public virtual ProfileType ProfileType { get; set; } = null!;

    /// <summary>
    /// Display name for the profile (e.g., "John Doe", "Acme Corp")
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Display name must be between 1 and 100 characters")]
    public virtual string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Biography or description text for the profile
    /// </summary>
    [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
    public virtual string Bio { get; set; } = string.Empty;

    /// <summary>
    /// File ID from file storage service for uploaded avatar images
    /// </summary>
    [StringLength(255)]
    public virtual string? AvatarFileId { get; set; }

    /// <summary>
    /// URL or path to the profile's avatar image
    /// </summary>
    [Url(ErrorMessage = "Avatar must be a valid URL")]
    [StringLength(500, ErrorMessage = "Avatar URL cannot exceed 500 characters")]
    public virtual string Avatar { get; set; } = string.Empty;

    /// <summary>
    /// Geographic location associated with the profile
    /// </summary>
    public virtual Location? Location { get; set; }

    /// <summary>
    /// Indicates if this is the user's currently active profile
    /// </summary>
    public virtual bool IsActive { get; set; } = false;

    /// <summary>
    /// Profile visibility level determining who can view the profile
    /// </summary>
    public virtual VisibilityLevel VisibilityLevel { get; set; } = VisibilityLevel.Public;

    /// <summary>
    /// Additional metadata stored as JSON for dynamic profile type-specific data
    /// </summary>
    public virtual string Metadata { get; set; } = "{}";

    /// <summary>
    /// Profile view count for analytics
    /// </summary>
    public virtual int ViewCount { get; set; } = 0;

    /// <summary>
    /// Tags associated with the profile for categorization and discovery
    /// </summary>
    public virtual List<string> Tags { get; set; } = new();

    /// <summary>
    /// Social media links stored as JSON for flexibility
    /// </summary>
    public virtual string SocialMediaLinks { get; set; } = "{}";

    /// <summary>
    /// Website URL associated with the profile
    /// </summary>
    [Url(ErrorMessage = "Website must be a valid URL")]
    [StringLength(500, ErrorMessage = "Website URL cannot exceed 500 characters")]
    public virtual string Website { get; set; } = string.Empty;

    /// <summary>
    /// Contact email for the profile (may differ from user's main email)
    /// </summary>
    [EmailAddress(ErrorMessage = "Contact email must be a valid email address")]
    [StringLength(256, ErrorMessage = "Contact email cannot exceed 256 characters")]
    public virtual string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number
    /// </summary>
    [Phone(ErrorMessage = "Contact phone must be a valid phone number")]
    [StringLength(20, ErrorMessage = "Contact phone cannot exceed 20 characters")]
    public virtual string ContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether to show contact information publicly
    /// </summary>
    public virtual bool ShowContactInfo { get; set; } = true;

    /// <summary>
    /// List of user IDs who are allowed to view this profile (when VisibilityLevel is Restricted)
    /// </summary>
    public virtual List<Guid> AllowedViewers { get; set; } = new();

    /// <summary>
    /// Updates the view count
    /// </summary>
    public void IncrementViewCount()
    {
        ViewCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets this profile as the active profile for the user
    /// </summary>
    public void SetAsActive()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates this profile
    /// </summary>
    public void SetAsInactive()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates if the profile has required fields populated
    /// </summary>
    public bool IsValidForDisplay()
    {
        return !string.IsNullOrWhiteSpace(DisplayName);
    }

    /// <summary>
    /// Gets a formatted location string
    /// </summary>
    public string LocationDisplay => Location?.ToString() ?? string.Empty;

    /// <summary>
    /// Checks if the profile is visible to a specific user
    /// </summary>
    /// <param name="viewerUserId">The ID of the user trying to view the profile</param>
    /// <param name="isOwner">Whether the viewer is the profile owner</param>
    /// <returns>True if the profile is visible to the user</returns>
    public bool IsVisibleTo(Guid? viewerUserId, bool isOwner = false)
    {
        // Owner can always see their own profile
        if (isOwner)
            return true;

        return VisibilityLevel switch
        {
            VisibilityLevel.Public => true,
            VisibilityLevel.Private => false,
            VisibilityLevel.Restricted => viewerUserId.HasValue && AllowedViewers.Contains(viewerUserId.Value),
            VisibilityLevel.ConnectionsOnly => false, // TODO: Implement when connections/friends feature is added
            _ => false
        };
    }

    /// <summary>
    /// Sets metadata from an object, serializing it to JSON
    /// </summary>
    /// <param name="metadataObject">The object to serialize and store as metadata</param>
    public void SetMetadata<T>(T metadataObject) where T : class
    {
        if (metadataObject != null)
        {
            Metadata = JsonSerializer.Serialize(metadataObject);
        }
        else
        {
            Metadata = "{}";
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets strongly-typed metadata from the JSON string
    /// </summary>
    /// <typeparam name="T">The type to deserialize the metadata to</typeparam>
    /// <returns>The deserialized metadata object, or null if no metadata exists</returns>
    public T? GetMetadata<T>() where T : class
    {
        if (string.IsNullOrWhiteSpace(Metadata) || Metadata == "{}")
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(Metadata);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Sets social media links from a dictionary
    /// </summary>
    /// <param name="socialLinks">Dictionary of social media platform to URL</param>
    public void SetSocialMediaLinks(Dictionary<string, string> socialLinks)
    {
        if (socialLinks?.Any() == true)
        {
            SocialMediaLinks = JsonSerializer.Serialize(socialLinks);
        }
        else
        {
            SocialMediaLinks = "{}";
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets social media links as a dictionary
    /// </summary>
    /// <returns>Dictionary of social media platform to URL, or empty dictionary if none exist</returns>
    public Dictionary<string, string> GetSocialMediaLinks()
    {
        if (string.IsNullOrWhiteSpace(SocialMediaLinks) || SocialMediaLinks == "{}")
            return new Dictionary<string, string>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(SocialMediaLinks) 
                   ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Adds a tag to the profile if it doesn't already exist
    /// </summary>
    /// <param name="tag">The tag to add</param>
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        var normalizedTag = tag.Trim().ToLowerInvariant();
        if (!Tags.Contains(normalizedTag, StringComparer.OrdinalIgnoreCase))
        {
            Tags.Add(normalizedTag);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Removes a tag from the profile
    /// </summary>
    /// <param name="tag">The tag to remove</param>
    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        var tagToRemove = Tags.FirstOrDefault(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
        if (tagToRemove != null)
        {
            Tags.Remove(tagToRemove);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Adds a user to the allowed viewers list
    /// </summary>
    /// <param name="userId">The user ID to grant access</param>
    public void GrantAccessTo(Guid userId)
    {
        if (userId != Guid.Empty && !AllowedViewers.Contains(userId))
        {
            AllowedViewers.Add(userId);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Removes a user from the allowed viewers list
    /// </summary>
    /// <param name="userId">The user ID to revoke access</param>
    public void RevokeAccessFrom(Guid userId)
    {
        if (AllowedViewers.Contains(userId))
        {
            AllowedViewers.Remove(userId);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}