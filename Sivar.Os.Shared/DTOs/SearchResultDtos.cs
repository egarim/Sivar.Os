using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// Base DTO for all search results
/// </summary>
public abstract record SearchResultBaseDto
{
    public Guid Id { get; init; }
    public SearchResultType ResultType { get; init; }
    public SearchMatchSource MatchSource { get; init; }
    public double RelevanceScore { get; init; }
    public int DisplayOrder { get; init; }
    
    // Common fields
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Handle { get; init; }
    public string? Category { get; init; }
    public string? ImageUrl { get; init; }
    
    // Location
    public string? City { get; init; }
    public string? Department { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? DistanceKm { get; init; }
    
    // Tags
    public string[]? Tags { get; init; }

    /// <summary>
    /// Generated URL to navigate to this result
    /// </summary>
    public string NavigationUrl => !string.IsNullOrEmpty(Handle) 
        ? $"/{Handle.TrimStart('@')}" 
        : $"/post/{Id}";
}

/// <summary>
/// DTO for business/profile search results
/// </summary>
public record BusinessSearchResultDto : SearchResultBaseDto
{
    public Guid? ProfileId { get; init; }
    public string? SubCategory { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public string? WorkingHours { get; init; }
    public string? PriceRange { get; init; }
    public double? Rating { get; init; }
    public int? ReviewCount { get; init; }

    /// <summary>
    /// Call-to-action buttons for this result
    /// </summary>
    public IReadOnlyList<CallToActionDto> Actions => GenerateActions();

    private List<CallToActionDto> GenerateActions()
    {
        var actions = new List<CallToActionDto>
        {
            new() { Label = "Ver Perfil", Url = NavigationUrl, Icon = "visibility", IsPrimary = true }
        };

        if (!string.IsNullOrEmpty(Phone))
            actions.Add(new() { Label = "Llamar", Url = $"tel:{Phone}", Icon = "phone" });

        if (!string.IsNullOrEmpty(Website))
            actions.Add(new() { Label = "Sitio Web", Url = Website, Icon = "language", IsExternal = true });

        if (Latitude.HasValue && Longitude.HasValue)
            actions.Add(new() { Label = "Ver Mapa", Url = $"https://maps.google.com/?q={Latitude},{Longitude}", Icon = "map", IsExternal = true });

        return actions;
    }
}

/// <summary>
/// DTO for event search results
/// </summary>
public record EventSearchResultDto : SearchResultBaseDto
{
    public Guid? PostId { get; init; }
    public DateTime? EventDate { get; init; }
    public DateTime? EventEndDate { get; init; }
    public string? Venue { get; init; }
    public string? Address { get; init; }
    public string? TicketPrice { get; init; }

    /// <summary>
    /// Formatted event date string
    /// </summary>
    public string EventDateFormatted => EventDate?.ToString("dd MMM yyyy, h:mm tt") ?? "Fecha por confirmar";

    /// <summary>
    /// Whether the event is upcoming or past
    /// </summary>
    public bool IsUpcoming => EventDate.HasValue && EventDate.Value > DateTime.UtcNow;

    /// <summary>
    /// Call-to-action buttons for this result
    /// </summary>
    public IReadOnlyList<CallToActionDto> Actions => GenerateActions();

    private List<CallToActionDto> GenerateActions()
    {
        var actions = new List<CallToActionDto>
        {
            new() { Label = "Ver Evento", Url = NavigationUrl, Icon = "event", IsPrimary = true }
        };

        if (Latitude.HasValue && Longitude.HasValue)
            actions.Add(new() { Label = "Ubicación", Url = $"https://maps.google.com/?q={Latitude},{Longitude}", Icon = "place", IsExternal = true });

        if (!string.IsNullOrEmpty(TicketPrice) && IsUpcoming)
            actions.Add(new() { Label = "Comprar", Url = NavigationUrl + "/tickets", Icon = "confirmation_number" });

        return actions;
    }
}

/// <summary>
/// DTO for government procedure search results
/// </summary>
public record ProcedureSearchResultDto : SearchResultBaseDto
{
    public Guid? PostId { get; init; }
    public string[]? Requirements { get; init; }
    public string? ProcessingTime { get; init; }
    public string? Cost { get; init; }
    public string? WhereToGo { get; init; }
    public string? OnlineUrl { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? WorkingHours { get; init; }

    /// <summary>
    /// Whether this procedure can be done online
    /// </summary>
    public bool IsOnlineAvailable => !string.IsNullOrEmpty(OnlineUrl);

    /// <summary>
    /// Call-to-action buttons for this result
    /// </summary>
    public IReadOnlyList<CallToActionDto> Actions => GenerateActions();

    private List<CallToActionDto> GenerateActions()
    {
        var actions = new List<CallToActionDto>
        {
            new() { Label = "Ver Detalles", Url = NavigationUrl, Icon = "description", IsPrimary = true }
        };

        if (!string.IsNullOrEmpty(OnlineUrl))
            actions.Add(new() { Label = "Iniciar Trámite", Url = OnlineUrl, Icon = "open_in_new", IsExternal = true });

        if (!string.IsNullOrEmpty(Phone))
            actions.Add(new() { Label = "Llamar", Url = $"tel:{Phone}", Icon = "phone" });

        if (Latitude.HasValue && Longitude.HasValue)
            actions.Add(new() { Label = "Cómo Llegar", Url = $"https://maps.google.com/?q={Latitude},{Longitude}", Icon = "directions", IsExternal = true });

        return actions;
    }
}

/// <summary>
/// DTO for tourism/attraction search results
/// </summary>
public record TourismSearchResultDto : SearchResultBaseDto
{
    public Guid? PostId { get; init; }
    public Guid? ProfileId { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public string? WorkingHours { get; init; }
    public string? TicketPrice { get; init; }
    public double? Rating { get; init; }
    public int? ReviewCount { get; init; }

    /// <summary>
    /// Call-to-action buttons for this result
    /// </summary>
    public IReadOnlyList<CallToActionDto> Actions => GenerateActions();

    private List<CallToActionDto> GenerateActions()
    {
        var actions = new List<CallToActionDto>
        {
            new() { Label = "Ver Detalles", Url = NavigationUrl, Icon = "explore", IsPrimary = true }
        };

        if (Latitude.HasValue && Longitude.HasValue)
            actions.Add(new() { Label = "Ver en Mapa", Url = $"https://maps.google.com/?q={Latitude},{Longitude}", Icon = "map", IsExternal = true });

        if (!string.IsNullOrEmpty(Website))
            actions.Add(new() { Label = "Más Info", Url = Website, Icon = "language", IsExternal = true });

        return actions;
    }
}

/// <summary>
/// DTO for product search results
/// </summary>
public record ProductSearchResultDto : SearchResultBaseDto
{
    public Guid? PostId { get; init; }
    public Guid? ProfileId { get; init; }
    public string? ProfileName { get; init; }
    public string? ProfileHandle { get; init; }
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public bool? IsNegotiable { get; init; }
    public string? AvailabilityStatus { get; init; }

    /// <summary>
    /// Formatted price string
    /// </summary>
    public string PriceFormatted => Price.HasValue 
        ? $"${Price:N2} {Currency ?? "USD"}{(IsNegotiable == true ? " (Negociable)" : "")}" 
        : "Consultar precio";

    /// <summary>
    /// Call-to-action buttons for this result
    /// </summary>
    public IReadOnlyList<CallToActionDto> Actions => GenerateActions();

    private List<CallToActionDto> GenerateActions()
    {
        var actions = new List<CallToActionDto>
        {
            new() { Label = "Ver Producto", Url = NavigationUrl, Icon = "shopping_bag", IsPrimary = true }
        };

        if (!string.IsNullOrEmpty(ProfileHandle))
            actions.Add(new() { Label = "Ver Tienda", Url = $"/{ProfileHandle.TrimStart('@')}", Icon = "store" });

        return actions;
    }
}

/// <summary>
/// DTO for service search results
/// </summary>
public record ServiceSearchResultDto : SearchResultBaseDto
{
    public Guid? PostId { get; init; }
    public Guid? ProfileId { get; init; }
    public string? ProfileName { get; init; }
    public string? ProfileHandle { get; init; }
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public bool? IsNegotiable { get; init; }
    public string? Duration { get; init; }
    public string? AvailabilityStatus { get; init; }
    public string? Phone { get; init; }

    /// <summary>
    /// Formatted price string
    /// </summary>
    public string PriceFormatted => Price.HasValue 
        ? $"${Price:N2} {Currency ?? "USD"}{(IsNegotiable == true ? " (Negociable)" : "")}" 
        : "Consultar precio";

    /// <summary>
    /// Call-to-action buttons for this result
    /// </summary>
    public IReadOnlyList<CallToActionDto> Actions => GenerateActions();

    private List<CallToActionDto> GenerateActions()
    {
        var actions = new List<CallToActionDto>
        {
            new() { Label = "Ver Servicio", Url = NavigationUrl, Icon = "handyman", IsPrimary = true }
        };

        if (!string.IsNullOrEmpty(Phone))
            actions.Add(new() { Label = "Contactar", Url = $"tel:{Phone}", Icon = "phone" });

        if (!string.IsNullOrEmpty(ProfileHandle))
            actions.Add(new() { Label = "Ver Proveedor", Url = $"/{ProfileHandle.TrimStart('@')}", Icon = "person" });

        return actions;
    }
}

/// <summary>
/// DTO for call-to-action buttons on search result cards
/// </summary>
public record CallToActionDto
{
    /// <summary>
    /// Button label text
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// URL to navigate to
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Material icon name
    /// </summary>
    public string Icon { get; init; } = "arrow_forward";

    /// <summary>
    /// Whether this is the primary action
    /// </summary>
    public bool IsPrimary { get; init; }

    /// <summary>
    /// Whether this opens in a new tab (external link)
    /// </summary>
    public bool IsExternal { get; init; }
}

/// <summary>
/// Collection of search results with metadata
/// </summary>
public record SearchResultsCollectionDto
{
    /// <summary>
    /// The original query that produced these results
    /// </summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>
    /// Total number of results found
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Time taken to execute the search in milliseconds
    /// </summary>
    public long SearchTimeMs { get; init; }

    /// <summary>
    /// Business/profile results
    /// </summary>
    public IReadOnlyList<BusinessSearchResultDto> Businesses { get; init; } = [];

    /// <summary>
    /// Event results
    /// </summary>
    public IReadOnlyList<EventSearchResultDto> Events { get; init; } = [];

    /// <summary>
    /// Procedure results
    /// </summary>
    public IReadOnlyList<ProcedureSearchResultDto> Procedures { get; init; } = [];

    /// <summary>
    /// Tourism results
    /// </summary>
    public IReadOnlyList<TourismSearchResultDto> Tourism { get; init; } = [];

    /// <summary>
    /// Product results
    /// </summary>
    public IReadOnlyList<ProductSearchResultDto> Products { get; init; } = [];

    /// <summary>
    /// Service results
    /// </summary>
    public IReadOnlyList<ServiceSearchResultDto> Services { get; init; } = [];

    /// <summary>
    /// Whether any results were found
    /// </summary>
    public bool HasResults => TotalCount > 0;

    /// <summary>
    /// Whether the results include location-based matches
    /// </summary>
    public bool HasLocationBasedResults => 
        Businesses.Any(b => b.DistanceKm.HasValue) ||
        Events.Any(e => e.DistanceKm.HasValue) ||
        Tourism.Any(t => t.DistanceKm.HasValue);
}

/// <summary>
/// Request DTO for hybrid search
/// </summary>
public record HybridSearchRequestDto
{
    /// <summary>
    /// Natural language query
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Pre-computed embedding vector (optional, computed server-side if not provided)
    /// </summary>
    public float[]? QueryEmbedding { get; init; }

    /// <summary>
    /// Filter by result types (empty = all types)
    /// </summary>
    public SearchResultType[]? ResultTypes { get; init; }

    /// <summary>
    /// Filter by category
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// User's latitude for geographic search
    /// </summary>
    public double? UserLatitude { get; init; }

    /// <summary>
    /// User's longitude for geographic search
    /// </summary>
    public double? UserLongitude { get; init; }

    /// <summary>
    /// Maximum distance in kilometers (for geographic search)
    /// </summary>
    public double? MaxDistanceKm { get; init; }

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int Limit { get; init; } = 10;

    /// <summary>
    /// Weight for semantic similarity (0.0 to 1.0)
    /// </summary>
    public double SemanticWeight { get; init; } = 0.5;

    /// <summary>
    /// Weight for full-text search (0.0 to 1.0)
    /// </summary>
    public double FullTextWeight { get; init; } = 0.3;

    /// <summary>
    /// Weight for geographic proximity (0.0 to 1.0)
    /// </summary>
    public double GeoWeight { get; init; } = 0.2;
}
