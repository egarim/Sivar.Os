using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Sivar.Os.Shared.DTOs.ValueObjects;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Base entity for all posts in the activity stream
/// Supports different content types based on profile capabilities
/// </summary>
public class Post : BaseEntity
{
    /// <summary>
    /// The profile that created this post
    /// </summary>
    public virtual Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// Type of post (determines available features)
    /// </summary>
    public virtual PostType PostType { get; set; } = PostType.General;

    /// <summary>
    /// Main content/text of the post
    /// </summary>
    [Required]
    [StringLength(5000, MinimumLength = 1, ErrorMessage = "Post content must be between 1 and 5000 characters")]
    public virtual string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional title for structured posts (products, services, events)
    /// </summary>
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public virtual string? Title { get; set; }

    /// <summary>
    /// Location associated with this post (optional)
    /// </summary>
    public virtual Location? Location { get; set; }

    /// <summary>
    /// Pricing information for products/services (JSON)
    /// Structure: { "amount": decimal, "currency": "USD", "isNegotiable": bool }
    /// </summary>
    public virtual string? PricingInfo { get; set; }

    /// <summary>
    /// Business-specific metadata (JSON)
    /// For BusinessLocation: working hours, contact info, location type
    /// For Products: specifications, dimensions, warranty
    /// For Services: duration, requirements, availability
    /// </summary>
    public virtual string? BusinessMetadata { get; set; }

    /// <summary>
    /// Availability status for products/services
    /// </summary>
    public virtual AvailabilityStatus? AvailabilityStatus { get; set; }

    /// <summary>
    /// Tags for categorization and search
    /// </summary>
    public virtual string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Media attachments (images, videos, links)
    /// </summary>
    public virtual ICollection<PostAttachment> Attachments { get; set; } = new ObservableCollection<PostAttachment>();

    /// <summary>
    /// Comments on this post
    /// </summary>
    public virtual ICollection<Comment> Comments { get; set; } = new ObservableCollection<Comment>();

    /// <summary>
    /// Reactions (likes, etc.) on this post
    /// </summary>
    public virtual ICollection<Reaction> Reactions { get; set; } = new ObservableCollection<Reaction>();

    /// <summary>
    /// Number of times this post has been viewed
    /// </summary>
    public virtual int ViewCount { get; set; } = 0;

    /// <summary>
    /// Number of times this post has been shared
    /// </summary>
    public virtual int ShareCount { get; set; } = 0;

    /// <summary>
    /// Indicates if this post is pinned to the profile
    /// </summary>
    public virtual bool IsPinned { get; set; } = false;

    /// <summary>
    /// Indicates if this post is featured/highlighted
    /// </summary>
    public virtual bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Scheduled publication date (null for immediate posts)
    /// </summary>
    public virtual DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// Expiration date for time-sensitive posts (offers, events)
    /// </summary>
    public virtual DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Language of the post content
    /// </summary>
    [StringLength(5)]
    public virtual string Language { get; set; } = "en";

    /// <summary>
    /// Vector embedding of the post content for semantic search
    /// Stored as PostgreSQL vector(384) type for fast similarity search with HNSW index
    /// String representation in C# (following Phase 3 pattern with tsvector)
    /// </summary>
    public virtual string? ContentEmbedding { get; set; }

    /// <summary>
    /// Language-specific full-text search vector (auto-generated from Content and Title)
    /// Uses the Language property to apply correct stemming and stop words
    /// PostgreSQL tsvector for fast, accurate full-text search
    /// Best for searching within a specific language
    /// </summary>
    public virtual string? SearchVector { get; set; }

    /// <summary>
    /// Universal full-text search vector (language-agnostic)
    /// Uses 'simple' configuration - no stemming, works for all languages
    /// Best for cross-language searches and unsupported languages
    /// </summary>
    public virtual string? SearchVectorSimple { get; set; }

    /// <summary>
    /// Visibility level of the post
    /// </summary>
    public virtual VisibilityLevel Visibility { get; set; } = VisibilityLevel.Public;

    /// <summary>
    /// Indicates if this post has been edited after creation
    /// </summary>
    public virtual bool IsEdited { get; set; } = false;

    /// <summary>
    /// Date and time when the post was last edited (null if never edited)
    /// </summary>
    public virtual DateTime? EditedAt { get; set; }

    // ==================== SENTIMENT ANALYSIS FIELDS ====================

    /// <summary>
    /// Primary emotion detected in the post (Joy, Sadness, Anger, Fear, Neutral)
    /// </summary>
    [StringLength(20)]
    public virtual string? PrimaryEmotion { get; set; }

    /// <summary>
    /// Confidence score for the primary emotion (0.0 to 1.0)
    /// </summary>
    public virtual decimal? EmotionScore { get; set; }

    /// <summary>
    /// Sentiment polarity score (-1.0 = negative, 0 = neutral, +1.0 = positive)
    /// </summary>
    public virtual decimal? SentimentPolarity { get; set; }

    /// <summary>
    /// Joy emotion score (0.0 to 1.0)
    /// </summary>
    public virtual decimal? JoyScore { get; set; }

    /// <summary>
    /// Sadness emotion score (0.0 to 1.0)
    /// </summary>
    public virtual decimal? SadnessScore { get; set; }

    /// <summary>
    /// Anger emotion score (0.0 to 1.0)
    /// </summary>
    public virtual decimal? AngerScore { get; set; }

    /// <summary>
    /// Fear emotion score (0.0 to 1.0)
    /// </summary>
    public virtual decimal? FearScore { get; set; }

    /// <summary>
    /// Indicates if anger was detected above threshold (for moderation)
    /// </summary>
    public virtual bool HasAnger { get; set; } = false;

    /// <summary>
    /// Indicates if this post needs manual review (high anger or toxic content)
    /// </summary>
    public virtual bool NeedsReview { get; set; } = false;

    /// <summary>
    /// Timestamp when sentiment analysis was performed
    /// </summary>
    public virtual DateTime? AnalyzedAt { get; set; }

    // Helper methods for business metadata

    /// <summary>
    /// Gets working hours for business location posts
    /// </summary>
    public BusinessHours? GetWorkingHours()
    {
        if (PostType != PostType.BusinessLocation || string.IsNullOrEmpty(BusinessMetadata))
            return null;

        try
        {
            var metadata = JsonSerializer.Deserialize<BusinessLocationMetadata>(BusinessMetadata);
            return metadata?.WorkingHours;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets working hours for business location posts
    /// </summary>
    public void SetWorkingHours(BusinessHours workingHours)
    {
        var metadata = GetBusinessLocationMetadata() ?? new BusinessLocationMetadata();
        metadata.WorkingHours = workingHours;
        BusinessMetadata = JsonSerializer.Serialize(metadata);
    }

    /// <summary>
    /// Gets business location metadata
    /// </summary>
    public BusinessLocationMetadata? GetBusinessLocationMetadata()
    {
        if (PostType != PostType.BusinessLocation || string.IsNullOrEmpty(BusinessMetadata))
            return null;

        try
        {
            return JsonSerializer.Deserialize<BusinessLocationMetadata>(BusinessMetadata);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets product metadata
    /// </summary>
    public ProductMetadata? GetProductMetadata()
    {
        if (PostType != PostType.Product || string.IsNullOrEmpty(BusinessMetadata))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ProductMetadata>(BusinessMetadata);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets service metadata
    /// </summary>
    public ServiceMetadata? GetServiceMetadata()
    {
        if (PostType != PostType.Service || string.IsNullOrEmpty(BusinessMetadata))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ServiceMetadata>(BusinessMetadata);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets pricing information
    /// </summary>
    public PricingInformation? GetPricing()
    {
        if (string.IsNullOrEmpty(PricingInfo))
            return null;

        try
        {
            return JsonSerializer.Deserialize<PricingInformation>(PricingInfo);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets pricing information
    /// </summary>
    public void SetPricing(decimal amount, Currency currency, bool isNegotiable = false)
    {
        var pricing = new PricingInformation
        {
            Amount = amount,
            Currency = currency,
            IsNegotiable = isNegotiable
        };
        PricingInfo = JsonSerializer.Serialize(pricing);
    }

    /// <summary>
    /// Validates if this post type is allowed for the given profile type
    /// </summary>
    public bool IsValidForProfile(ProfileType profileType)
    {
        // All profiles can create general posts
        if (PostType == PostType.General)
            return true;

        // Check if profile has the required features
        return PostType switch
        {
            PostType.BusinessLocation => profileType.HasFeature("AllowsBusinessLocations"),
            PostType.Product => profileType.HasFeature("AllowsProducts"),
            PostType.Service => profileType.HasFeature("AllowsServices"),
            PostType.Event => profileType.HasFeature("AllowsEvents"),
            PostType.JobPosting => profileType.HasFeature("AllowsJobPostings"),
            _ => false
        };
    }

    /// <summary>
    /// Increments the view count
    /// </summary>
    public void IncrementViewCount()
    {
        ViewCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the share count
    /// </summary>
    public void IncrementShareCount()
    {
        ShareCount++;
        UpdatedAt = DateTime.UtcNow;
    }
}

// Supporting classes for structured metadata

/// <summary>
/// Pricing information for products and services
/// </summary>
public class PricingInformation
{
    public decimal Amount { get; set; }
    public Currency Currency { get; set; } = Currency.USD;
    public bool IsNegotiable { get; set; } = false;
    public string? Description { get; set; }
}

/// <summary>
/// Business location metadata
/// </summary>
public class BusinessLocationMetadata
{
    public BusinessLocationType LocationType { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public bool AcceptsWalkIns { get; set; } = true;
    public bool RequiresAppointment { get; set; } = false;
    public BusinessHours? WorkingHours { get; set; }
    public string? SpecialInstructions { get; set; }
}

/// <summary>
/// Product metadata
/// </summary>
public class ProductMetadata
{
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SKU { get; set; }
    public string? Category { get; set; }
    public Dictionary<string, string> Specifications { get; set; } = new();
    public string? WarrantyInfo { get; set; }
    public int? StockQuantity { get; set; }
    public string? Dimensions { get; set; }
    public string? Weight { get; set; }
}

/// <summary>
/// Service metadata
/// </summary>
public class ServiceMetadata
{
    public string? Category { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Requirements { get; set; }
    public string[] IncludedFeatures { get; set; } = Array.Empty<string>();
    public string? BookingInstructions { get; set; }
    public bool RequiresConsultation { get; set; } = false;
}

/// <summary>
/// Business working hours
/// </summary>
public class BusinessHours
{
    public DaySchedule Monday { get; set; } = new();
    public DaySchedule Tuesday { get; set; } = new();
    public DaySchedule Wednesday { get; set; } = new();
    public DaySchedule Thursday { get; set; } = new();
    public DaySchedule Friday { get; set; } = new();
    public DaySchedule Saturday { get; set; } = new();
    public DaySchedule Sunday { get; set; } = new();
    
    public string? SpecialHoursNote { get; set; }
}

/// <summary>
/// Daily schedule for a business
/// </summary>
public class DaySchedule
{
    public bool IsClosed { get; set; } = false;
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public TimeOnly? BreakStart { get; set; }
    public TimeOnly? BreakEnd { get; set; }
    public string? SpecialNote { get; set; }
}