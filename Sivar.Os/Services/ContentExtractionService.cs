using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Services;

/// <summary>
/// AI-powered service that extracts structured metadata from plain text post content.
/// This enables users to write simple posts while the system automatically extracts:
/// - Business information (phone, hours, location)
/// - Tags and categories
/// - Location data
/// - Post type classification
/// </summary>
public interface IContentExtractionService
{
    /// <summary>
    /// Extracts structured metadata from post content using AI
    /// </summary>
    Task<ExtractedContentMetadata> ExtractMetadataAsync(string content, string language = "es");
    
    /// <summary>
    /// Suggests tags based on content
    /// </summary>
    Task<List<string>> SuggestTagsAsync(string content, string language = "es");
    
    /// <summary>
    /// Classifies the post type based on content
    /// </summary>
    Task<PostType> ClassifyPostTypeAsync(string content);
}

/// <summary>
/// Extracted metadata from post content
/// </summary>
public record ExtractedContentMetadata
{
    /// <summary>
    /// Suggested post type based on content analysis
    /// </summary>
    public PostType SuggestedPostType { get; init; } = PostType.General;
    
    /// <summary>
    /// Confidence score for the post type classification (0-1)
    /// </summary>
    public double PostTypeConfidence { get; init; }
    
    /// <summary>
    /// Extracted/suggested tags
    /// </summary>
    public List<string> Tags { get; init; } = new();
    
    /// <summary>
    /// Extracted location information
    /// </summary>
    public ExtractedLocation? Location { get; init; }
    
    /// <summary>
    /// Extracted business metadata (for business posts)
    /// </summary>
    public ExtractedBusinessMetadata? BusinessMetadata { get; init; }
    
    /// <summary>
    /// Extracted event metadata (for event posts)
    /// </summary>
    public ExtractedEventMetadata? EventMetadata { get; init; }
    
    /// <summary>
    /// Extracted pricing information
    /// </summary>
    public ExtractedPricingInfo? PricingInfo { get; init; }
    
    /// <summary>
    /// Whether extraction was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Error message if extraction failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}

public record ExtractedLocation
{
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; } = "El Salvador";
    public string? Address { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

public record ExtractedBusinessMetadata
{
    public string? BusinessName { get; init; }
    public string? BusinessType { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? WorkingHours { get; init; }
    public List<string>? Specialties { get; init; }
    public bool? AcceptsWalkIns { get; init; }
    public bool? RequiresAppointment { get; init; }
}

public record ExtractedEventMetadata
{
    public string? EventName { get; init; }
    public DateTime? EventDate { get; init; }
    public DateTime? EventEndDate { get; init; }
    public string? Venue { get; init; }
    public string? TicketPrice { get; init; }
    public string? TicketUrl { get; init; }
}

public record ExtractedPricingInfo
{
    public decimal? Amount { get; init; }
    public string? Currency { get; init; } = "USD";
    public string? PriceRange { get; init; }
    public bool? IsNegotiable { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Implementation of content extraction using AI
/// </summary>
public class ContentExtractionService : IContentExtractionService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<ContentExtractionService> _logger;
    
    // Known cities in El Salvador for location extraction
    private static readonly HashSet<string> SalvadoranCities = new(StringComparer.OrdinalIgnoreCase)
    {
        "San Salvador", "Santa Ana", "San Miguel", "Santa Tecla", "Soyapango",
        "Mejicanos", "Apopa", "Delgado", "Ilopango", "Tonacatepeque",
        "La Libertad", "Antiguo Cuscatlán", "Chalatenango", "Usulután",
        "Ahuachapán", "Sonsonate", "La Unión", "Cojutepeque", "Zacatecoluca",
        "San Vicente", "Sensuntepeque", "San Francisco Gotera", "Metapán",
        "Quezaltepeque", "Colón", "Lourdes", "San Juan Opico", "Nejapa",
        "El Congo", "Coatepeque", "Izalco", "Nahuizalco", "Juayúa",
        "Ataco", "Apaneca", "Berlín", "Alegría", "Jucuapa", "Chinameca"
    };
    
    // Known departments in El Salvador
    private static readonly Dictionary<string, string> SalvadoranDepartments = new(StringComparer.OrdinalIgnoreCase)
    {
        { "San Salvador", "San Salvador" },
        { "Santa Ana", "Santa Ana" },
        { "San Miguel", "San Miguel" },
        { "La Libertad", "La Libertad" },
        { "Usulután", "Usulután" },
        { "Sonsonate", "Sonsonate" },
        { "La Unión", "La Unión" },
        { "La Paz", "La Paz" },
        { "Chalatenango", "Chalatenango" },
        { "Cuscatlán", "Cuscatlán" },
        { "Ahuachapán", "Ahuachapán" },
        { "Morazán", "Morazán" },
        { "San Vicente", "San Vicente" },
        { "Cabañas", "Cabañas" }
    };

    public ContentExtractionService(
        IChatClient chatClient,
        ILogger<ContentExtractionService> logger)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ExtractedContentMetadata> ExtractMetadataAsync(string content, string language = "es")
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new ExtractedContentMetadata
            {
                Success = false,
                ErrorMessage = "Content is empty"
            };
        }

        var requestId = Guid.NewGuid();
        _logger.LogInformation("[ContentExtractionService.ExtractMetadataAsync] START - RequestId={RequestId}, ContentLength={Length}",
            requestId, content.Length);

        try
        {
            // First, do quick rule-based extraction for common patterns
            var quickExtraction = QuickExtract(content);
            
            // Then use AI for more sophisticated extraction
            var aiExtraction = await AiExtractAsync(content, language, requestId);
            
            // Merge results, preferring AI extraction but using quick extraction as fallback
            var merged = MergeExtractions(quickExtraction, aiExtraction);
            
            _logger.LogInformation("[ContentExtractionService.ExtractMetadataAsync] SUCCESS - RequestId={RequestId}, PostType={PostType}, TagCount={TagCount}",
                requestId, merged.SuggestedPostType, merged.Tags.Count);
            
            return merged;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentExtractionService.ExtractMetadataAsync] ERROR - RequestId={RequestId}", requestId);
            
            // Return rule-based extraction as fallback
            var fallback = QuickExtract(content);
            fallback = fallback with { ErrorMessage = $"AI extraction failed: {ex.Message}" };
            return fallback;
        }
    }

    public async Task<List<string>> SuggestTagsAsync(string content, string language = "es")
    {
        var metadata = await ExtractMetadataAsync(content, language);
        return metadata.Tags;
    }

    public async Task<PostType> ClassifyPostTypeAsync(string content)
    {
        var metadata = await ExtractMetadataAsync(content);
        return metadata.SuggestedPostType;
    }

    /// <summary>
    /// Quick rule-based extraction for common patterns (no AI needed)
    /// </summary>
    private ExtractedContentMetadata QuickExtract(string content)
    {
        var lowerContent = content.ToLowerInvariant();
        var tags = new List<string>();
        ExtractedLocation? location = null;
        ExtractedBusinessMetadata? businessMeta = null;
        ExtractedPricingInfo? pricingInfo = null;
        var suggestedType = PostType.General;
        
        // === PHONE NUMBER EXTRACTION ===
        var phonePattern = new System.Text.RegularExpressions.Regex(
            @"(?:\+503\s?)?(?:2|6|7)\d{3}[-\s]?\d{4}");
        var phoneMatch = phonePattern.Match(content);
        string? phone = phoneMatch.Success ? phoneMatch.Value : null;
        
        // === EMAIL EXTRACTION ===
        var emailPattern = new System.Text.RegularExpressions.Regex(
            @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
        var emailMatch = emailPattern.Match(content);
        string? email = emailMatch.Success ? emailMatch.Value : null;
        
        // === WEBSITE EXTRACTION ===
        var websitePattern = new System.Text.RegularExpressions.Regex(
            @"(?:https?://)?(?:www\.)?[a-zA-Z0-9-]+\.[a-zA-Z]{2,}(?:/[^\s]*)?");
        var websiteMatch = websitePattern.Match(content);
        string? website = websiteMatch.Success ? websiteMatch.Value : null;
        
        // === WORKING HOURS EXTRACTION ===
        var hoursPattern = new System.Text.RegularExpressions.Regex(
            @"(?:abierto|horario|horas?|de)\s*(?:de\s*)?(\d{1,2}(?::\d{2})?\s*(?:am|pm|a\.?m\.?|p\.?m\.?)?)\s*(?:a|hasta|-)\s*(\d{1,2}(?::\d{2})?\s*(?:am|pm|a\.?m\.?|p\.?m\.?)?)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var hoursMatch = hoursPattern.Match(content);
        string? workingHours = hoursMatch.Success ? $"{hoursMatch.Groups[1].Value} - {hoursMatch.Groups[2].Value}" : null;
        
        // === PRICE EXTRACTION ===
        var pricePattern = new System.Text.RegularExpressions.Regex(
            @"\$\s*(\d+(?:\.\d{2})?)|(\d+(?:\.\d{2})?)\s*(?:dólares?|USD)");
        var priceMatch = pricePattern.Match(content);
        if (priceMatch.Success)
        {
            var priceStr = priceMatch.Groups[1].Success ? priceMatch.Groups[1].Value : priceMatch.Groups[2].Value;
            if (decimal.TryParse(priceStr, out var amount))
            {
                pricingInfo = new ExtractedPricingInfo
                {
                    Amount = amount,
                    Currency = "USD"
                };
            }
        }
        
        // === LOCATION EXTRACTION ===
        foreach (var city in SalvadoranCities)
        {
            if (content.Contains(city, StringComparison.OrdinalIgnoreCase))
            {
                // Try to find the department
                string? department = null;
                foreach (var (dept, deptName) in SalvadoranDepartments)
                {
                    if (content.Contains(dept, StringComparison.OrdinalIgnoreCase))
                    {
                        department = deptName;
                        break;
                    }
                }
                
                location = new ExtractedLocation
                {
                    City = city,
                    State = department ?? GetDepartmentForCity(city),
                    Country = "El Salvador"
                };
                break;
            }
        }
        
        // === POST TYPE & TAG CLASSIFICATION ===
        
        // Restaurant/Food indicators
        if (ContainsAny(lowerContent, "restaurante", "comida", "pupusa", "cocina", "menú", "plato",
            "desayuno", "almuerzo", "cena", "café", "cafetería", "bar", "cantina", "típico"))
        {
            suggestedType = PostType.BusinessLocation;
            tags.AddRange(new[] { "restaurant", "food" });
            
            if (lowerContent.Contains("pupusa")) tags.Add("pupusas");
            if (lowerContent.Contains("típico") || lowerContent.Contains("tipico")) tags.Add("salvadoran");
            if (lowerContent.Contains("café") || lowerContent.Contains("cafetería")) tags.Add("cafe");
        }
        // Hotel/Lodging indicators
        else if (ContainsAny(lowerContent, "hotel", "hospedaje", "habitación", "reservación", "hostal", "airbnb"))
        {
            suggestedType = PostType.BusinessLocation;
            tags.AddRange(new[] { "hotel", "lodging" });
        }
        // Event indicators
        else if (ContainsAny(lowerContent, "evento", "concierto", "festival", "fiesta", "celebración",
            "entrada", "boleto", "ticket"))
        {
            suggestedType = PostType.Event;
            tags.Add("event");
            
            if (lowerContent.Contains("concierto")) tags.Add("concert");
            if (lowerContent.Contains("festival")) tags.Add("festival");
        }
        // Service indicators
        else if (ContainsAny(lowerContent, "servicio", "reparación", "mantenimiento", "instalación",
            "consulta", "asesoría", "profesional"))
        {
            suggestedType = PostType.Service;
            tags.Add("service");
        }
        // Product indicators
        else if (ContainsAny(lowerContent, "vendo", "venta", "producto", "artículo", "disponible",
            "precio", "oferta", "promoción"))
        {
            suggestedType = PostType.Product;
            tags.Add("product");
        }
        // Tourism indicators
        else if (ContainsAny(lowerContent, "turismo", "tour", "excursión", "playa", "volcán", 
            "lago", "parque", "atracción", "visita"))
        {
            suggestedType = PostType.BusinessLocation;
            tags.AddRange(new[] { "tourism", "attraction" });
        }
        
        // Build business metadata if we have business indicators
        if (suggestedType == PostType.BusinessLocation || phone != null || workingHours != null)
        {
            businessMeta = new ExtractedBusinessMetadata
            {
                Phone = phone,
                Email = email,
                Website = website,
                WorkingHours = workingHours,
                AcceptsWalkIns = true // Default
            };
        }
        
        // Add price tags if applicable
        if (pricingInfo?.Amount != null)
        {
            if (pricingInfo.Amount < 5) tags.Add("price-budget");
            else if (pricingInfo.Amount < 15) tags.Add("price-moderate");
            else tags.Add("price-premium");
        }
        
        return new ExtractedContentMetadata
        {
            SuggestedPostType = suggestedType,
            PostTypeConfidence = 0.7, // Rule-based has moderate confidence
            Tags = tags.Distinct().ToList(),
            Location = location,
            BusinessMetadata = businessMeta,
            PricingInfo = pricingInfo,
            Success = true
        };
    }

    /// <summary>
    /// AI-powered extraction for sophisticated understanding
    /// </summary>
    private async Task<ExtractedContentMetadata> AiExtractAsync(string content, string language, Guid requestId)
    {
        var prompt = $@"Analyze the following post content and extract structured information.
Return ONLY valid JSON with no additional text.

Post content:
---
{content}
---

Extract and return JSON with this structure:
{{
  ""postType"": ""General|BusinessLocation|Event|Product|Service|Blog"",
  ""postTypeConfidence"": 0.0-1.0,
  ""tags"": [""tag1"", ""tag2""],
  ""location"": {{
    ""city"": ""city name or null"",
    ""state"": ""department/state or null"",
    ""country"": ""El Salvador"",
    ""address"": ""street address if mentioned or null""
  }},
  ""business"": {{
    ""name"": ""business name or null"",
    ""type"": ""Restaurant|Hotel|Service|Store|etc or null"",
    ""phone"": ""phone number or null"",
    ""email"": ""email or null"",
    ""website"": ""website or null"",
    ""workingHours"": ""hours description or null"",
    ""specialties"": [""specialty1"", ""specialty2""]
  }},
  ""event"": {{
    ""name"": ""event name or null"",
    ""date"": ""YYYY-MM-DD or null"",
    ""venue"": ""venue name or null"",
    ""ticketPrice"": ""price or null""
  }},
  ""pricing"": {{
    ""amount"": 0.00,
    ""currency"": ""USD"",
    ""priceRange"": ""$|$$|$$$|null"",
    ""isNegotiable"": true/false
  }}
}}

Important rules:
- For El Salvador, common cities include: San Salvador, Santa Ana, San Miguel, Santa Tecla, La Libertad
- Phone numbers in El Salvador start with +503 or are 8 digits starting with 2, 6, or 7
- Extract ALL relevant tags for searchability
- Set postType based on content intent (what is the user trying to share?)
- If uncertain about a field, set it to null";

        var response = await _chatClient.GetResponseAsync(prompt);
        var responseText = response.Text?.Trim() ?? "";
        
        _logger.LogDebug("[ContentExtractionService.AiExtractAsync] AI Response - RequestId={RequestId}, Response={Response}",
            requestId, responseText);
        
        // Parse the JSON response
        try
        {
            // Clean up the response - remove markdown code blocks if present
            responseText = responseText
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();
            
            var json = JsonDocument.Parse(responseText);
            var root = json.RootElement;
            
            // Parse post type
            var postTypeStr = root.TryGetProperty("postType", out var pt) ? pt.GetString() : "General";
            var postType = postTypeStr switch
            {
                "BusinessLocation" => PostType.BusinessLocation,
                "Event" => PostType.Event,
                "Product" => PostType.Product,
                "Service" => PostType.Service,
                "Blog" => PostType.Blog,
                _ => PostType.General
            };
            
            var confidence = root.TryGetProperty("postTypeConfidence", out var conf) ? conf.GetDouble() : 0.8;
            
            // Parse tags
            var tags = new List<string>();
            if (root.TryGetProperty("tags", out var tagsElement) && tagsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var tag in tagsElement.EnumerateArray())
                {
                    var tagStr = tag.GetString();
                    if (!string.IsNullOrEmpty(tagStr))
                        tags.Add(tagStr.ToLowerInvariant());
                }
            }
            
            // Parse location
            ExtractedLocation? location = null;
            if (root.TryGetProperty("location", out var locElement) && locElement.ValueKind == JsonValueKind.Object)
            {
                var city = locElement.TryGetProperty("city", out var c) ? c.GetString() : null;
                if (!string.IsNullOrEmpty(city))
                {
                    location = new ExtractedLocation
                    {
                        City = city,
                        State = locElement.TryGetProperty("state", out var s) ? s.GetString() : null,
                        Country = locElement.TryGetProperty("country", out var co) ? co.GetString() ?? "El Salvador" : "El Salvador",
                        Address = locElement.TryGetProperty("address", out var a) ? a.GetString() : null
                    };
                }
            }
            
            // Parse business metadata
            ExtractedBusinessMetadata? businessMeta = null;
            if (root.TryGetProperty("business", out var bizElement) && bizElement.ValueKind == JsonValueKind.Object)
            {
                var phone = bizElement.TryGetProperty("phone", out var ph) ? ph.GetString() : null;
                var name = bizElement.TryGetProperty("name", out var n) ? n.GetString() : null;
                
                if (!string.IsNullOrEmpty(phone) || !string.IsNullOrEmpty(name))
                {
                    var specialties = new List<string>();
                    if (bizElement.TryGetProperty("specialties", out var specElement) && specElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var spec in specElement.EnumerateArray())
                        {
                            var specStr = spec.GetString();
                            if (!string.IsNullOrEmpty(specStr))
                                specialties.Add(specStr);
                        }
                    }
                    
                    businessMeta = new ExtractedBusinessMetadata
                    {
                        BusinessName = name,
                        BusinessType = bizElement.TryGetProperty("type", out var t) ? t.GetString() : null,
                        Phone = phone,
                        Email = bizElement.TryGetProperty("email", out var e) ? e.GetString() : null,
                        Website = bizElement.TryGetProperty("website", out var w) ? w.GetString() : null,
                        WorkingHours = bizElement.TryGetProperty("workingHours", out var h) ? h.GetString() : null,
                        Specialties = specialties.Count > 0 ? specialties : null
                    };
                }
            }
            
            // Parse pricing
            ExtractedPricingInfo? pricingInfo = null;
            if (root.TryGetProperty("pricing", out var priceElement) && priceElement.ValueKind == JsonValueKind.Object)
            {
                var amount = priceElement.TryGetProperty("amount", out var amt) && amt.ValueKind == JsonValueKind.Number 
                    ? (decimal?)amt.GetDecimal() 
                    : null;
                
                if (amount.HasValue && amount > 0)
                {
                    pricingInfo = new ExtractedPricingInfo
                    {
                        Amount = amount,
                        Currency = priceElement.TryGetProperty("currency", out var cur) ? cur.GetString() ?? "USD" : "USD",
                        PriceRange = priceElement.TryGetProperty("priceRange", out var pr) ? pr.GetString() : null,
                        IsNegotiable = priceElement.TryGetProperty("isNegotiable", out var neg) && neg.ValueKind == JsonValueKind.True
                    };
                }
            }
            
            return new ExtractedContentMetadata
            {
                SuggestedPostType = postType,
                PostTypeConfidence = confidence,
                Tags = tags,
                Location = location,
                BusinessMetadata = businessMeta,
                PricingInfo = pricingInfo,
                Success = true
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[ContentExtractionService.AiExtractAsync] Failed to parse AI response - RequestId={RequestId}", requestId);
            return new ExtractedContentMetadata
            {
                Success = false,
                ErrorMessage = "Failed to parse AI response"
            };
        }
    }

    /// <summary>
    /// Merge quick extraction with AI extraction, preferring AI results when available
    /// </summary>
    private ExtractedContentMetadata MergeExtractions(ExtractedContentMetadata quick, ExtractedContentMetadata ai)
    {
        if (!ai.Success)
            return quick;
        
        // Merge tags (AI + quick, deduplicated)
        var mergedTags = new HashSet<string>(ai.Tags, StringComparer.OrdinalIgnoreCase);
        foreach (var tag in quick.Tags)
            mergedTags.Add(tag);
        
        // Use AI's location if available, otherwise quick
        var location = ai.Location ?? quick.Location;
        
        // Merge business metadata
        ExtractedBusinessMetadata? businessMeta = null;
        if (ai.BusinessMetadata != null || quick.BusinessMetadata != null)
        {
            var aiMeta = ai.BusinessMetadata ?? new ExtractedBusinessMetadata();
            var quickMeta = quick.BusinessMetadata ?? new ExtractedBusinessMetadata();
            
            businessMeta = new ExtractedBusinessMetadata
            {
                BusinessName = aiMeta.BusinessName ?? quickMeta.BusinessName,
                BusinessType = aiMeta.BusinessType ?? quickMeta.BusinessType,
                Phone = aiMeta.Phone ?? quickMeta.Phone,
                Email = aiMeta.Email ?? quickMeta.Email,
                Website = aiMeta.Website ?? quickMeta.Website,
                WorkingHours = aiMeta.WorkingHours ?? quickMeta.WorkingHours,
                Specialties = aiMeta.Specialties ?? quickMeta.Specialties,
                AcceptsWalkIns = aiMeta.AcceptsWalkIns ?? quickMeta.AcceptsWalkIns,
                RequiresAppointment = aiMeta.RequiresAppointment ?? quickMeta.RequiresAppointment
            };
        }
        
        // Use AI's pricing if available, otherwise quick
        var pricingInfo = ai.PricingInfo ?? quick.PricingInfo;
        
        // Use AI's post type if confidence is high, otherwise use quick
        var postType = ai.PostTypeConfidence >= 0.7 ? ai.SuggestedPostType : quick.SuggestedPostType;
        var confidence = Math.Max(ai.PostTypeConfidence, quick.PostTypeConfidence);
        
        return new ExtractedContentMetadata
        {
            SuggestedPostType = postType,
            PostTypeConfidence = confidence,
            Tags = mergedTags.ToList(),
            Location = location,
            BusinessMetadata = businessMeta,
            EventMetadata = ai.EventMetadata,
            PricingInfo = pricingInfo,
            Success = true
        };
    }

    private static bool ContainsAny(string content, params string[] keywords)
    {
        return keywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetDepartmentForCity(string city)
    {
        return city.ToLowerInvariant() switch
        {
            "san salvador" or "mejicanos" or "soyapango" or "apopa" or "delgado" or "ilopango" => "San Salvador",
            "santa tecla" or "antiguo cuscatlán" or "la libertad" or "colón" or "quezaltepeque" => "La Libertad",
            "santa ana" or "metapán" or "el congo" or "coatepeque" => "Santa Ana",
            "san miguel" or "chinameca" => "San Miguel",
            "sonsonate" or "izalco" or "nahuizalco" or "juayúa" or "ataco" or "apaneca" => "Sonsonate",
            "usulután" or "berlín" or "alegría" or "jucuapa" => "Usulután",
            "chalatenango" => "Chalatenango",
            "ahuachapán" => "Ahuachapán",
            _ => null
        };
    }
}
