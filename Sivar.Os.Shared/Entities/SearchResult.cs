using System.ComponentModel.DataAnnotations;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a structured search result from the AI chat
/// Enables rendering as graphical cards with call-to-actions
/// </summary>
public class SearchResult : BaseEntity
{
    /// <summary>
    /// The chat message this result belongs to
    /// </summary>
    public virtual Guid ChatMessageId { get; set; }
    public virtual ChatMessage ChatMessage { get; set; } = null!;

    /// <summary>
    /// Type of search result (Business, Event, Procedure, etc.)
    /// </summary>
    public virtual SearchResultType ResultType { get; set; }

    /// <summary>
    /// Source of the match (Semantic, FullText, Geographic, Hybrid)
    /// </summary>
    public virtual SearchMatchSource MatchSource { get; set; }

    /// <summary>
    /// Combined relevance score (0.0 to 1.0)
    /// Higher scores indicate better matches
    /// </summary>
    public virtual double RelevanceScore { get; set; }

    /// <summary>
    /// Semantic similarity score component (0.0 to 1.0)
    /// </summary>
    public virtual double? SemanticScore { get; set; }

    /// <summary>
    /// Full-text search rank component
    /// </summary>
    public virtual double? FullTextRank { get; set; }

    /// <summary>
    /// Distance in kilometers (for geographic searches)
    /// </summary>
    public virtual double? DistanceKm { get; set; }

    /// <summary>
    /// Order position in the result list
    /// </summary>
    public virtual int DisplayOrder { get; set; }

    #region Business/Profile Data
    
    /// <summary>
    /// Reference to the source post (if result is from a post)
    /// </summary>
    public virtual Guid? PostId { get; set; }
    public virtual Post? Post { get; set; }

    /// <summary>
    /// Reference to the source profile (if result is a business/profile)
    /// </summary>
    public virtual Guid? ProfileId { get; set; }
    public virtual Profile? Profile { get; set; }

    /// <summary>
    /// Display name/title of the result
    /// </summary>
    [Required]
    [StringLength(200)]
    public virtual string Title { get; set; } = string.Empty;

    /// <summary>
    /// Short description or summary
    /// </summary>
    [StringLength(500)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// Profile handle for direct linking (e.g., "@pupuseria-dona-maria")
    /// </summary>
    [StringLength(100)]
    public virtual string? Handle { get; set; }

    /// <summary>
    /// Category or type label (e.g., "Restaurante", "Evento", "Trámite")
    /// </summary>
    [StringLength(50)]
    public virtual string? Category { get; set; }

    /// <summary>
    /// Subcategory for more specific classification
    /// </summary>
    [StringLength(50)]
    public virtual string? SubCategory { get; set; }

    /// <summary>
    /// Image URL for the result card
    /// </summary>
    [StringLength(500)]
    public virtual string? ImageUrl { get; set; }

    #endregion

    #region Location Data

    /// <summary>
    /// City/Municipality name
    /// </summary>
    [StringLength(100)]
    public virtual string? City { get; set; }

    /// <summary>
    /// Department/Region name
    /// </summary>
    [StringLength(100)]
    public virtual string? Department { get; set; }

    /// <summary>
    /// Full address
    /// </summary>
    [StringLength(300)]
    public virtual string? Address { get; set; }

    /// <summary>
    /// Latitude for map display
    /// </summary>
    public virtual double? Latitude { get; set; }

    /// <summary>
    /// Longitude for map display
    /// </summary>
    public virtual double? Longitude { get; set; }

    #endregion

    #region Business Details

    /// <summary>
    /// Phone number
    /// </summary>
    [StringLength(50)]
    public virtual string? Phone { get; set; }

    /// <summary>
    /// Website URL
    /// </summary>
    [StringLength(300)]
    public virtual string? Website { get; set; }

    /// <summary>
    /// Working hours summary (e.g., "Lun-Vie: 8AM-5PM")
    /// </summary>
    [StringLength(200)]
    public virtual string? WorkingHours { get; set; }

    /// <summary>
    /// Raw working hours JSON for real-time open/closed calculation
    /// </summary>
    public virtual string? WorkingHoursJson { get; set; }

    /// <summary>
    /// Price range indicator (e.g., "$", "$$", "$$$")
    /// </summary>
    [StringLength(10)]
    public virtual string? PriceRange { get; set; }

    /// <summary>
    /// Average rating (0.0 to 5.0)
    /// </summary>
    public virtual double? Rating { get; set; }

    /// <summary>
    /// Number of reviews
    /// </summary>
    public virtual int? ReviewCount { get; set; }

    #endregion

    #region Event Data

    /// <summary>
    /// Event start date/time
    /// </summary>
    public virtual DateTime? EventDate { get; set; }

    /// <summary>
    /// Event end date/time
    /// </summary>
    public virtual DateTime? EventEndDate { get; set; }

    /// <summary>
    /// Venue name for events
    /// </summary>
    [StringLength(200)]
    public virtual string? Venue { get; set; }

    /// <summary>
    /// Ticket price or price range
    /// </summary>
    [StringLength(50)]
    public virtual string? TicketPrice { get; set; }

    #endregion

    #region Procedure Data (Government Services)

    /// <summary>
    /// Requirements list (JSON array)
    /// </summary>
    public virtual string? Requirements { get; set; }

    /// <summary>
    /// Estimated processing time
    /// </summary>
    [StringLength(100)]
    public virtual string? ProcessingTime { get; set; }

    /// <summary>
    /// Cost/fee information
    /// </summary>
    [StringLength(100)]
    public virtual string? Cost { get; set; }

    /// <summary>
    /// Where to perform the procedure
    /// </summary>
    [StringLength(200)]
    public virtual string? WhereToGo { get; set; }

    /// <summary>
    /// Online procedure URL (if available)
    /// </summary>
    [StringLength(300)]
    public virtual string? OnlineUrl { get; set; }

    #endregion

    #region Tags and Metadata

    /// <summary>
    /// Tags for the result (JSON array)
    /// </summary>
    public virtual string[]? Tags { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public virtual string? Metadata { get; set; }

    #endregion
}
