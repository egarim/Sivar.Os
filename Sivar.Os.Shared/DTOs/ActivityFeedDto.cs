namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for paginated activity feed response
/// </summary>
public class ActivityFeedDto
{
    /// <summary>
    /// List of activities in the feed
    /// </summary>
    public List<ActivityDto> Activities { get; set; } = new();

    /// <summary>
    /// Current page number (0-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of activities
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there are more pages
    /// </summary>
    public bool HasNextPage => Page < TotalPages - 1;

    /// <summary>
    /// Whether there are previous pages
    /// </summary>
    public bool HasPreviousPage => Page > 0;
}
