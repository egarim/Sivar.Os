namespace Sivar.Os.Shared.Enums;

/// <summary>
/// Types of search results that can be returned by the AI chat
/// </summary>
public enum SearchResultType
{
    /// <summary>
    /// Business/profile search result
    /// </summary>
    Business = 1,

    /// <summary>
    /// Event search result
    /// </summary>
    Event = 2,

    /// <summary>
    /// Government procedure/service result
    /// </summary>
    Procedure = 3,

    /// <summary>
    /// Product search result
    /// </summary>
    Product = 4,

    /// <summary>
    /// Service search result
    /// </summary>
    Service = 5,

    /// <summary>
    /// Tourism/attraction search result
    /// </summary>
    Tourism = 6,

    /// <summary>
    /// General post search result
    /// </summary>
    Post = 7
}

/// <summary>
/// Source of the search match for ranking purposes
/// </summary>
public enum SearchMatchSource
{
    /// <summary>
    /// Matched via semantic/vector similarity
    /// </summary>
    Semantic = 1,

    /// <summary>
    /// Matched via full-text search
    /// </summary>
    FullText = 2,

    /// <summary>
    /// Matched via geographic proximity
    /// </summary>
    Geographic = 3,

    /// <summary>
    /// Matched via tag/category filter
    /// </summary>
    Tag = 4,

    /// <summary>
    /// Hybrid match (multiple sources)
    /// </summary>
    Hybrid = 5,
    
    /// <summary>
    /// Sponsored/promoted result from ad auction
    /// </summary>
    Sponsored = 6
}
