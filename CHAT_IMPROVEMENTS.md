# Chat UI Improvements Plan

## Current State Analysis

### AI Response Format (from screenshot)
The AI currently returns results in this format:
```
Aquí tienes algunas recomendaciones para disfrutar de deliciosas pupusas en El Salvador:

1. **Auténticas pupusas salvadoreñas**
- Ubicación: San Salvador
- Especialidades: Pupusas de chicharrón, queso con loroco, y revueltas.
- [🔗 Ver más detalles](/pupuseria-el-comalito)

2. **Comida típica con el sazón de la abuela**
- Ubicación: Santa Tecla
- Delicias: Pupusas, tamales de elote y pisques, yuca frita.
- [🔗 Ver más detalles](/tipicos-dona-maria)

...

¡Espero que esta información te sea útil para disfrutar de unas ricas pupusas!
```

### Problems with Current Implementation
1. **Pattern mismatch**: The regex looks for `[Name](url)` but AI returns `**Name**` with `[Ver más detalles](url)` separately
2. **No cards rendering**: Falls back to text/link rendering instead of graphical cards
3. **Missing rich data**: Location, specialties, prices not being extracted
4. **No call-to-actions**: Only a single "Ver más detalles" link, no Save/Share/Map buttons

---

## Phase 0: Structured RAG Architecture (Priority: CRITICAL)

### Goal
Replace unstructured text parsing with a reliable **Structured RAG** approach that returns typed data from the database, enabling rich UI rendering and agent collaboration.

### Current Tech Stack (from csproj)
```xml
<!-- Microsoft Agent Framework & AI Extensions -->
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-preview.251204.1" />
<PackageReference Include="Microsoft.Extensions.AI" Version="10.0.1" />
<PackageReference Include="Microsoft.Extensions.AI.Abstractions" Version="10.0.1" />
<PackageReference Include="Microsoft.Extensions.AI.Ollama" Version="9.7.0-preview.1.25356.2" />
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="10.0.1-preview.1.25571.5" />
<PackageReference Include="System.Numerics.Tensors" Version="10.0.0" />
```

### Current ORM Capabilities (from Sivar.Os.Data)
- **PostgreSQL** with pgvector extension for embeddings
- **HNSW index** for fast similarity search (`ContentEmbedding vector(384)`)
- **Full-text search** with `SearchVector` tsvector column
- **JSONB columns** for `BusinessMetadata`, `PricingInfo`
- **GIN indexes** on JSON and array columns
- **PostGIS** for geospatial queries (`GeoLocation`)

---

### 0.1 Structured Search Response Schema

#### New Entity: `SearchResult`
```csharp
/// <summary>
/// Structured search result for RAG responses
/// Stored in Sivar_SearchResults table for caching and analytics
/// </summary>
public class SearchResult : BaseEntity
{
    /// <summary>
    /// The conversation this result was generated for
    /// </summary>
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    
    /// <summary>
    /// Original search query from user
    /// </summary>
    public string Query { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of search performed
    /// </summary>
    public SearchResultType ResultType { get; set; }
    
    /// <summary>
    /// Structured results as JSONB
    /// </summary>
    public string ResultsJson { get; set; } = "[]";
    
    /// <summary>
    /// Number of results returned
    /// </summary>
    public int ResultCount { get; set; }
    
    /// <summary>
    /// Search execution time in milliseconds
    /// </summary>
    public double ExecutionTimeMs { get; set; }
    
    /// <summary>
    /// Search metadata (filters applied, location, etc.)
    /// </summary>
    public string? SearchMetadata { get; set; }
}

public enum SearchResultType
{
    Business = 1,      // Restaurants, shops, services
    Event = 2,         // Events, activities
    Profile = 3,       // People, organizations
    Procedure = 4,     // Government procedures
    Location = 5,      // Tourist attractions, landmarks
    Mixed = 6          // Multiple types combined
}
```

#### DTO: `BusinessSearchResultDto`
```csharp
/// <summary>
/// Structured business result for chat UI cards
/// </summary>
public record BusinessSearchResultDto
{
    // Identity
    public Guid ProfileId { get; init; }
    public Guid? PostId { get; init; }
    public string Handle { get; init; } = string.Empty;
    
    // Display
    public string Name { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string? Category { get; init; }
    public string? Description { get; init; }
    
    // Location
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? DistanceKm { get; init; }
    
    // Contact
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    
    // Business Info
    public string? WorkingHours { get; init; }
    public string? PriceRange { get; init; }
    public bool IsOpenNow { get; init; }
    
    // Social
    public int FollowerCount { get; init; }
    public double? Rating { get; init; }
    public int ReviewCount { get; init; }
    
    // Tags for filtering
    public string[] Tags { get; init; } = Array.Empty<string>();
}
```

#### Extended `ChatResponseDto`
```csharp
public record ChatResponseDto
{
    public ChatMessageDto UserMessage { get; init; } = null!;
    public ChatMessageDto AssistantMessage { get; init; } = null!;
    public Guid ConversationId { get; init; }
    
    // NEW: Structured RAG Results
    public SearchResultType? ResultType { get; init; }
    public List<BusinessSearchResultDto>? BusinessResults { get; init; }
    public List<EventSearchResultDto>? EventResults { get; init; }
    public List<ProcedureSearchResultDto>? ProcedureResults { get; init; }
    
    // Metadata for UI
    public int TotalResultCount { get; init; }
    public bool HasMoreResults { get; init; }
    public string? SuggestedFollowUp { get; init; }
}
```

---

### 0.2 Specialized Search Agents Architecture

#### Agent Hierarchy
```
┌─────────────────────────────────────────────────────────────────┐
│                     Orchestrator Agent                          │
│  - Understands user intent                                      │
│  - Routes to specialized agents                                 │
│  - Combines results and generates response                      │
└─────────────────────────────────────────────────────────────────┘
          │                    │                    │
          ▼                    ▼                    ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│  Business Agent │  │   Event Agent   │  │ Procedure Agent │
│                 │  │                 │  │                 │
│ - Restaurants   │  │ - Concerts      │  │ - DUI process   │
│ - Hotels        │  │ - Sports        │  │ - Passport      │
│ - Banks         │  │ - Festivals     │  │ - Vehicle reg   │
│ - Services      │  │ - Meetups       │  │ - Tax filing    │
└─────────────────┘  └─────────────────┘  └─────────────────┘
          │                    │                    │
          ▼                    ▼                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                    PostgreSQL Database                          │
│  pgvector + Full-text + JSONB + PostGIS                        │
└─────────────────────────────────────────────────────────────────┘
```

#### Agent Definitions

**1. Business Search Agent**
```csharp
public class BusinessSearchAgent
{
    private readonly IPostRepository _postRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IEmbeddingService _embeddingService;
    
    [Description("Search for businesses like restaurants, hotels, banks, shops, and services")]
    public async Task<List<BusinessSearchResultDto>> SearchBusinesses(
        [Description("What the user is looking for (e.g., 'pupusas', 'bank', 'pharmacy')")]
        string query,
        
        [Description("Optional city to search in")]
        string? city = null,
        
        [Description("Optional category filter: Restaurant, Hotel, Bank, Pharmacy, Service, etc.")]
        string? category = null,
        
        [Description("User's latitude for proximity search")]
        double? userLatitude = null,
        
        [Description("User's longitude for proximity search")]
        double? userLongitude = null,
        
        [Description("Maximum distance in kilometers (default 10)")]
        double maxDistanceKm = 10,
        
        [Description("Maximum number of results (default 5)")]
        int limit = 5)
    {
        // 1. Generate embedding for semantic search
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
        
        // 2. Hybrid search: semantic + full-text + filters
        var results = await _postRepository.HybridSearchAsync(
            embedding: queryEmbedding,
            textQuery: query,
            postTypes: new[] { PostType.BusinessLocation },
            city: city,
            userLocation: userLatitude.HasValue 
                ? (userLatitude.Value, userLongitude!.Value) 
                : null,
            maxDistanceKm: maxDistanceKm,
            limit: limit
        );
        
        // 3. Map to DTOs with enrichment
        return results.Select(MapToBusinessResult).ToList();
    }
}
```

**2. Event Search Agent**
```csharp
public class EventSearchAgent
{
    [Description("Search for events, concerts, festivals, and activities")]
    public async Task<List<EventSearchResultDto>> SearchEvents(
        [Description("What type of event (e.g., 'concert', 'festival', 'sports')")]
        string query,
        
        [Description("Start date for event search (default: today)")]
        DateTime? fromDate = null,
        
        [Description("End date for event search (default: 30 days from now)")]
        DateTime? toDate = null,
        
        [Description("City where the event is located")]
        string? city = null,
        
        [Description("Maximum number of results")]
        int limit = 10)
    {
        // Search for Event type posts within date range
    }
}
```

**3. Procedure Search Agent**
```csharp
public class ProcedureSearchAgent
{
    [Description("Search for government procedures and requirements (DUI, passport, vehicle registration)")]
    public async Task<List<ProcedureSearchResultDto>> SearchProcedures(
        [Description("What procedure the user needs (e.g., 'DUI', 'passport', 'license')")]
        string query,
        
        [Description("City or municipality for location-specific info")]
        string? city = null)
    {
        // Search government profiles and procedure posts
    }
}
```

**4. Orchestrator Agent**
```csharp
public class ChatOrchestratorAgent
{
    private readonly BusinessSearchAgent _businessAgent;
    private readonly EventSearchAgent _eventAgent;
    private readonly ProcedureSearchAgent _procedureAgent;
    private readonly IChatClient _chatClient;
    
    [Description("Main entry point for user chat queries")]
    public async Task<StructuredChatResponse> ProcessQuery(
        string userQuery,
        Guid conversationId,
        double? userLatitude = null,
        double? userLongitude = null)
    {
        // 1. Classify intent
        var intent = await ClassifyIntent(userQuery);
        
        // 2. Route to appropriate agent(s)
        var response = new StructuredChatResponse();
        
        switch (intent)
        {
            case Intent.FindBusiness:
                response.BusinessResults = await _businessAgent.SearchBusinesses(userQuery, userLatitude, userLongitude);
                response.ResultType = SearchResultType.Business;
                break;
                
            case Intent.FindEvent:
                response.EventResults = await _eventAgent.SearchEvents(userQuery);
                response.ResultType = SearchResultType.Event;
                break;
                
            case Intent.GovernmentProcedure:
                response.ProcedureResults = await _procedureAgent.SearchProcedures(userQuery);
                response.ResultType = SearchResultType.Procedure;
                break;
                
            case Intent.Mixed:
                // Run multiple agents in parallel
                var tasks = new[]
                {
                    _businessAgent.SearchBusinesses(userQuery),
                    _eventAgent.SearchEvents(userQuery)
                };
                await Task.WhenAll(tasks);
                response.ResultType = SearchResultType.Mixed;
                break;
        }
        
        // 3. Generate natural language summary
        response.TextSummary = await GenerateSummary(response);
        
        return response;
    }
}
```

---

### 0.3 Database Changes

#### New Table: `Sivar_SearchResults`
```sql
CREATE TABLE "Sivar_SearchResults" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "ConversationId" uuid NOT NULL REFERENCES "Sivar_Conversations"("Id"),
    "Query" varchar(500) NOT NULL,
    "ResultType" int NOT NULL,
    "ResultsJson" jsonb NOT NULL DEFAULT '[]',
    "ResultCount" int NOT NULL DEFAULT 0,
    "ExecutionTimeMs" double precision NOT NULL DEFAULT 0,
    "SearchMetadata" jsonb,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "IsDeleted" boolean NOT NULL DEFAULT false
);

-- Index for conversation lookup
CREATE INDEX "IX_SearchResults_ConversationId" ON "Sivar_SearchResults" ("ConversationId");

-- GIN index for JSONB search
CREATE INDEX "IX_SearchResults_ResultsJson" ON "Sivar_SearchResults" USING gin ("ResultsJson");
```

#### New Repository Method: `HybridSearchAsync`
```csharp
public interface IPostRepository
{
    /// <summary>
    /// Hybrid search combining semantic (vector) + full-text + filters
    /// </summary>
    Task<List<Post>> HybridSearchAsync(
        float[]? embedding = null,
        string? textQuery = null,
        PostType[]? postTypes = null,
        string? city = null,
        (double lat, double lng)? userLocation = null,
        double maxDistanceKm = 10,
        string[]? tags = null,
        int limit = 10);
}
```

#### SQL for Hybrid Search
```sql
-- Hybrid search: vector similarity + full-text + geo + filters
WITH semantic_results AS (
    SELECT 
        p."Id",
        p."ContentEmbedding" <=> $1::vector AS vector_distance,
        ts_rank(p."SearchVector", plainto_tsquery('spanish', $2)) AS text_rank,
        ST_Distance(
            p."GeoLocation"::geography, 
            ST_SetSRID(ST_MakePoint($4, $3), 4326)::geography
        ) / 1000.0 AS distance_km
    FROM "Sivar_Posts" p
    WHERE p."IsDeleted" = false
      AND p."PostType" = ANY($5)
      AND ($6 IS NULL OR p."Location_City" ILIKE '%' || $6 || '%')
      AND ($3 IS NULL OR ST_DWithin(
          p."GeoLocation"::geography,
          ST_SetSRID(ST_MakePoint($4, $3), 4326)::geography,
          $7 * 1000  -- maxDistanceKm in meters
      ))
)
SELECT p.*
FROM "Sivar_Posts" p
JOIN semantic_results sr ON p."Id" = sr."Id"
ORDER BY 
    (0.5 * (1 - sr.vector_distance)) +  -- Semantic similarity (50%)
    (0.3 * sr.text_rank) +               -- Full-text relevance (30%)
    (0.2 * (1 - LEAST(sr.distance_km / $7, 1)))  -- Proximity (20%)
DESC
LIMIT $8;
```

---

### 0.4 Agent Registration in DI

```csharp
// In Program.cs or ServiceCollectionExtensions.cs
public static IServiceCollection AddChatAgents(this IServiceCollection services)
{
    // Register specialized agents
    services.AddScoped<BusinessSearchAgent>();
    services.AddScoped<EventSearchAgent>();
    services.AddScoped<ProcedureSearchAgent>();
    services.AddScoped<ChatOrchestratorAgent>();
    
    // Configure agent with tools
    services.AddChatClient(builder => builder
        .UseFunctionInvocation()
        .UseOpenAI(new OpenAIClientOptions
        {
            Endpoint = config["AI:Endpoint"],
            ApiKey = config["AI:ApiKey"]
        }))
        .AddFunctions(sp => new object[]
        {
            sp.GetRequiredService<BusinessSearchAgent>(),
            sp.GetRequiredService<EventSearchAgent>(),
            sp.GetRequiredService<ProcedureSearchAgent>()
        });
    
    return services;
}
```

---

## Phase 1: Fix Pattern Detection (Priority: HIGH)

### Goal
Correctly parse the new AI response format to extract:
- Business name (from **bold text**)
- Location (from "Ubicación:" line)
- Description/Specialties (from "Especialidades:", "Delicias:", "Ofertas:", "Platos:" lines)
- Profile URL (from markdown link)

### Implementation
Update `ChatMessage.razor` regex to match:
```
Pattern: \d+\.\s*\*\*([^*]+)\*\*\s*(?:.*?Ubicación:\s*([^\n-]+))?(?:.*?(?:Especialidades|Delicias|Ofertas|Platos):\s*([^\n-]+))?.*?\[(?:🔗\s*)?Ver más detalles\]\(([^)]+)\)
```

### Expected Output
Each match produces a `BusinessResult` with:
- `Name`: "Auténticas pupusas salvadoreñas"
- `Location`: "San Salvador"
- `Description`: "Pupusas de chicharrón, queso con loroco, y revueltas"
- `Url`: "/pupuseria-el-comalito"

---

## Phase 2: Enhanced Business Cards (Priority: HIGH)

### Goal
Display rich, interactive cards with more information and actions

### Card Layout Design
```
┌─────────────────────────────────────────────────────┐
│  🫓  Auténticas pupusas salvadoreñas               │
│      📍 San Salvador • 2.3 km                      │
│                                                     │
│  Pupusas de chicharrón, queso con loroco, y        │
│  revueltas.                                         │
│                                                     │
│  ⭐ 4.5 (23 reseñas) • $$ • Abierto ahora          │
│                                                     │
│  ┌──────────┐ ┌────────┐ ┌────────┐ ┌───────────┐  │
│  │ 👁️ Ver  │ │ 📍 Map │ │ 💾Save │ │ 📤 Share  │  │
│  └──────────┘ └────────┘ └────────┘ └───────────┘  │
└─────────────────────────────────────────────────────┘
```

### New Action Buttons
1. **👁️ Ver Perfil** - Navigate to profile page
2. **📍 Ver Mapa** - Show location on map (modal or navigate)
3. **💾 Guardar** - Save to user's saved places
4. **📤 Compartir** - Share via native share or copy link
5. **📞 Llamar** - Direct call (if phone available)

### Card Data Structure
```csharp
public class BusinessCardData
{
    public string Name { get; set; }
    public string ProfileUrl { get; set; }
    public string Location { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string Icon { get; set; }
    public string? PhoneNumber { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? PriceRange { get; set; }
}
```

---

## Phase 3: Map Integration (Priority: MEDIUM)

### Goal
Show locations on an interactive map within chat or as modal

### Options
1. **Inline Mini-Map**: Small static map preview in card
2. **Map Modal**: Click "Ver Mapa" opens full map with marker
3. **Map Below Results**: Show all results on a single map below cards

### Implementation
- Use Leaflet.js (already integrated per `LEAFLET_INTEGRATION_GUIDE.md`)
- Pass coordinates from AI function results
- Add "Ver todos en mapa" button when multiple results

---

## Phase 4: Backend Structured Response (Priority: MEDIUM)

> **Note**: This phase is now superseded by Phase 0 (Structured RAG).
> The implementation details are consolidated in Phase 0.

---

## Phase 5: Additional Features (Priority: LOW)

### 5.1 Ratings & Reviews Preview
- Show star rating if available
- Show review count

### 5.2 Quick Filters
After results, show filter chips:
- "Solo abiertos ahora"
- "Más cercanos"
- "Mejor valorados"

### 5.3 "Ask Follow-up" Suggestions
After results, suggest:
- "¿Cuál tiene mejor precio?"
- "¿Cuál está más cerca de mí?"
- "Muéstrame en el mapa"

### 5.4 Result Carousel
For many results, horizontal scroll carousel on mobile

---

## Implementation Order (Updated)

| Phase | Task | Effort | Priority |
|-------|------|--------|----------|
| **0.1** | Create `SearchResult` entity and migration | 2h | 🔴 Critical |
| **0.2** | Create `BusinessSearchResultDto` and response DTOs | 1h | 🔴 Critical |
| **0.3** | Implement `HybridSearchAsync` in PostRepository | 4h | 🔴 Critical |
| **0.4** | Create `BusinessSearchAgent` with tools | 3h | 🔴 Critical |
| **0.5** | Create `EventSearchAgent` with tools | 2h | 🔴 Critical |
| **0.6** | Create `ProcedureSearchAgent` with tools | 2h | 🔴 Critical |
| **0.7** | Create `ChatOrchestratorAgent` | 3h | 🔴 Critical |
| **0.8** | Update `ChatService` to use orchestrator | 2h | 🔴 Critical |
| **0.9** | Update `ChatResponseDto` with structured results | 1h | 🔴 Critical |
| 1.1 | Fix regex pattern for fallback text parsing | 2h | 🟡 Medium |
| 2.1 | Update card layout to consume structured data | 2h | 🟡 Medium |
| 2.2 | Add Map button with Leaflet modal | 3h | 🟡 Medium |
| 2.3 | Add Share button (copy link) | 1h | 🟢 Low |
| 2.4 | Add Save button (local storage first) | 2h | 🟢 Low |
| 3.1 | "Ver todos en mapa" multi-marker map | 2h | 🟢 Low |
| 5.x | Additional features | 8h | 🟢 Low |

**Estimated Total: ~40 hours**

---

## Files to Create/Modify

### Phase 0 (Structured RAG)
**New Files:**
- `Sivar.Os.Shared/Entities/SearchResult.cs` - New entity
- `Sivar.Os.Shared/Enums/SearchResultType.cs` - New enum
- `Sivar.Os.Shared/DTOs/SearchResultDtos.cs` - Result DTOs
- `Sivar.Os.Data/Configurations/SearchResultConfiguration.cs` - EF config
- `Sivar.Os.Data/Repositories/SearchResultRepository.cs` - Repository
- `Sivar.Os/Agents/BusinessSearchAgent.cs` - Business agent
- `Sivar.Os/Agents/EventSearchAgent.cs` - Event agent
- `Sivar.Os/Agents/ProcedureSearchAgent.cs` - Procedure agent
- `Sivar.Os/Agents/ChatOrchestratorAgent.cs` - Orchestrator
- `Sivar.Os.Data/Migrations/YYYYMMDD_AddSearchResults.cs` - Migration

**Modified Files:**
- `Sivar.Os.Data/Context/SivarDbContext.cs` - Add DbSet
- `Sivar.Os.Shared/DTOs/ChatDTOs.cs` - Extend response
- `Sivar.Os/Services/ChatService.cs` - Use orchestrator
- `Sivar.Os.Shared/Repositories/IPostRepository.cs` - Add HybridSearchAsync
- `Sivar.Os.Data/Repositories/PostRepository.cs` - Implement hybrid search
- `Sivar.Os/Program.cs` - Register agents

### Phase 1-2 (Client-side)
- `Sivar.Os.Client/Components/AIChat/ChatMessage.razor` - Pattern matching & card rendering
- `Sivar.Os.Client/Components/AIChat/ChatMessage.razor.css` - Card styles
- `Sivar.Os.Client/Pages/Home.razor.Models.cs` - BusinessCardData class
- `Sivar.Os.Client/Layout/MainLayout.razor` - Handle structured response

### Phase 3 (Map Integration)
- `Sivar.Os.Client/Components/AIChat/ChatMapModal.razor` - New component
- `Sivar.Os.Client/wwwroot/js/leaflet-helper.js` - Map initialization

---

## Success Metrics

1. ✅ AI returns structured data (not just text)
2. ✅ Business results render as graphical cards (not plain text)
3. ✅ Each card shows: Icon, Name, Location, Distance, Description
4. ✅ Cards have actionable buttons: Ver Perfil, Mapa, Guardar, Compartir
5. ✅ Clicking "Ver Perfil" navigates to profile page
6. ✅ Clicking "Mapa" shows location (Google Maps link or modal)
7. ✅ Search uses hybrid (semantic + full-text + geo) for best results
8. ✅ Cards work on mobile (responsive)
9. ✅ Response time < 2 seconds for typical queries

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              Client (Blazor)                            │
│  ┌─────────────┐  ┌─────────────┐  ┌──────────────┐  ┌───────────────┐ │
│  │ ChatMessage │  │ BusinessCard│  │ MapModal     │  │ SavedResults  │ │
│  └─────────────┘  └─────────────┘  └──────────────┘  └───────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           API (ChatController)                          │
│                  POST /api/chat/conversations/{id}/messages             │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        ChatOrchestratorAgent                            │
│  ┌──────────────────┐                                                   │
│  │ Intent Classifier│ → Determines query type                          │
│  └──────────────────┘                                                   │
│           │                                                              │
│           ├── Business? ─── BusinessSearchAgent ── HybridSearchAsync   │
│           ├── Event? ────── EventSearchAgent ───── EventSearchAsync    │
│           └── Procedure? ── ProcedureSearchAgent ─ ProcedureSearch     │
│                                                                         │
│  ┌──────────────────┐                                                   │
│  │ Response Builder │ → Combines results + generates text summary      │
│  └──────────────────┘                                                   │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         PostgreSQL Database                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────────┐  │
│  │ pgvector    │  │ Full-Text   │  │ PostGIS     │  │ JSONB         │  │
│  │ (Semantic)  │  │ (Keywords)  │  │ (Geo)       │  │ (Metadata)    │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  └───────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Next Steps

1. **Start with Phase 0.1-0.2**: Create entities and DTOs for structured responses
2. **Implement Phase 0.3**: Build hybrid search capability in PostRepository
3. **Build agents (Phase 0.4-0.7)**: Create specialized search agents
4. **Integrate (Phase 0.8-0.9)**: Update ChatService and response handling
5. **Update UI**: Modify ChatMessage to render structured cards
6. **Test end-to-end**: Verify complete flow from query to card display

---

## Phase X: AI Content Extraction Agent (IMPLEMENTED ✅)

### Goal
Enable users to create **simple plain text posts** while an AI agent automatically extracts structured metadata for the Structured RAG system. This removes the friction of requiring users to manually enter tags, location, business hours, phone numbers, etc.

### Problem Statement
The Structured RAG search relies on rich metadata (tags, location, business info, pricing) to return relevant results. However, requiring users to fill in forms with all this data creates poor UX. Users should be able to write natural text like:

```
"Abrimos nueva pupusería en Santa Tecla! Pupusas de chicharrón, loroco y revueltas.
Horario: Lunes a Domingo 6am-9pm. Tel: 2228-4567. Cerca del Parque San Martín."
```

And have the system automatically extract:
- **Location**: Santa Tecla, La Libertad, El Salvador
- **Tags**: pupusería, pupusas, chicharrón, loroco
- **Post Type**: BusinessLocation
- **Business Hours**: Lunes a Domingo 6am-9pm
- **Phone**: 2228-4567
- **Specialties**: chicharrón, loroco, revueltas

### Implementation

#### X.1 ContentExtractionService
**File**: `Sivar.Os/Services/ContentExtractionService.cs`

```csharp
public interface IContentExtractionService
{
    Task<ExtractedContentMetadata> ExtractMetadataAsync(string content, string language = "es");
    Task<List<string>> SuggestTagsAsync(string content, string language = "es");
    Task<PostType> ClassifyPostTypeAsync(string content);
}
```

**Key Features:**
- AI-powered extraction using IChatClient (Ollama/OpenAI)
- Supports Spanish and English content
- Rule-based extraction for known patterns (phone numbers, URLs)
- Location recognition for Salvadoran cities and departments
- Fallback to rule-based when AI is unavailable

#### X.2 Extracted Metadata Structure

```csharp
public record ExtractedContentMetadata
{
    public PostType SuggestedPostType { get; init; }     // General, Blog, BusinessLocation, Event, Product, Service
    public double PostTypeConfidence { get; init; }       // 0-1 confidence score
    public List<string> Tags { get; init; }               // Extracted/suggested tags
    public ExtractedLocation? Location { get; init; }     // City, State, Country, Lat/Lon
    public ExtractedBusinessMetadata? BusinessMetadata { get; init; }  // Phone, Hours, Website
    public ExtractedEventMetadata? EventMetadata { get; init; }        // Date, Venue, Tickets
    public ExtractedPricingInfo? PricingInfo { get; init; }            // Price range, Currency
    public bool Success { get; init; }
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
```

#### X.3 Integration with PostService

**File**: `Sivar.Os/Services/PostService.cs`

The `CreatePostAsync` method now:
1. Receives plain text content from user
2. Calls `ContentExtractionService.ExtractMetadataAsync()` 
3. Enriches the Post entity with AI-extracted metadata
4. User-provided data takes priority over AI extraction

```csharp
// ==================== AI CONTENT EXTRACTION ====================
ExtractedContentMetadata? extractedMetadata = null;
try
{
    extractedMetadata = await _contentExtractionService.ExtractMetadataAsync(
        createPostDto.Content, 
        createPostDto.Language ?? "es");
    
    if (extractedMetadata?.Success == true)
    {
        _logger.LogInformation("AI extraction successful: PostType={PostType}, Tags={TagCount}",
            extractedMetadata.SuggestedPostType,
            extractedMetadata.Tags?.Count ?? 0);
    }
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "AI content extraction failed, continuing with user data");
}

// Create post with AI-enriched metadata
var post = new Post
{
    // Use AI-detected PostType if user selected General and AI is confident
    PostType = (createPostDto.PostType == PostType.General && 
                extractedMetadata?.Success == true && 
                extractedMetadata.PostTypeConfidence > 0.6) 
        ? extractedMetadata.SuggestedPostType 
        : createPostDto.PostType,
    
    // Merge user tags with AI-extracted tags
    Tags = MergeTagsWithAiExtracted(createPostDto.Tags, extractedMetadata?.Tags),
    
    // Use AI-extracted business metadata if user didn't provide any
    BusinessMetadata = !string.IsNullOrEmpty(createPostDto.BusinessMetadata) 
        ? createPostDto.BusinessMetadata 
        : BuildBusinessMetadataFromExtraction(extractedMetadata),
    // ...
};

// Apply AI-extracted location if user didn't provide one
if (createPostDto.Location == null && extractedMetadata?.Location != null)
{
    post.Location = new Location
    {
        City = extractedMetadata.Location.City,
        State = extractedMetadata.Location.State,
        Country = extractedMetadata.Location.Country ?? "El Salvador"
    };
}
```

### Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         User Creates Post                               │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ "Nueva pupusería en Santa Tecla! Horario: 6am-9pm. Tel: 2228-4567│  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                   PostService.CreatePostAsync()                         │
│  ┌─────────────────┐                                                   │
│  │ AI Extraction   │ → ContentExtractionService.ExtractMetadataAsync() │
│  └─────────────────┘                                                   │
│           │                                                             │
│           ▼                                                             │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ ExtractedContentMetadata                                        │   │
│  │   PostType: BusinessLocation                                    │   │
│  │   Tags: [pupusería, santa tecla, comida típica]                │   │
│  │   Location: { City: Santa Tecla, State: La Libertad }          │   │
│  │   Business: { Phone: 2228-4567, Hours: 6am-9pm }               │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│           │                                                             │
│           ▼                                                             │
│  ┌─────────────────┐                                                   │
│  │ Enrich Post     │ → Merge AI data with user data (user priority)   │
│  └─────────────────┘                                                   │
│           │                                                             │
│           ▼                                                             │
│  ┌─────────────────┐                                                   │
│  │ Generate        │ → VectorEmbeddingService generates 384D vector    │
│  │ Embeddings      │                                                   │
│  └─────────────────┘                                                   │
│           │                                                             │
│           ▼                                                             │
│  ┌─────────────────┐                                                   │
│  │ Sentiment       │ → SentimentAnalysisService analyzes mood          │
│  │ Analysis        │                                                   │
│  └─────────────────┘                                                   │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         PostgreSQL Database                             │
│  Post stored with:                                                      │
│  - ContentEmbedding: vector(384) for semantic search                   │
│  - Tags[]: extracted keywords for filtering                            │
│  - BusinessMetadata: JSONB with phone, hours, website                  │
│  - Location: city, state, country for geo-search                       │
│  - SearchVector: tsvector for full-text search                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Benefits for Structured RAG

With automatic metadata extraction, posts now have:

| Field | Source | RAG Usage |
|-------|--------|-----------|
| Tags | AI-extracted | Keyword filtering, faceted search |
| Location | AI-extracted | Geo-spatial queries, "near me" |
| PostType | AI-classified | Category filtering |
| BusinessMetadata | AI-extracted | Business hours, contact filtering |
| ContentEmbedding | Server-generated | Semantic similarity search |
| SearchVector | PostgreSQL | Full-text keyword search |

This enables queries like:
- "pupuserías cerca de mí abiertas ahora" → Uses Location + BusinessHours
- "restaurantes en Santa Tecla" → Uses Tags + Location
- "dónde puedo comer comida típica" → Uses Semantic search + Tags

### Files Modified

1. **`Sivar.Os/Services/ContentExtractionService.cs`** (NEW)
   - `IContentExtractionService` interface
   - `ContentExtractionService` implementation
   - Record types for extracted metadata

2. **`Sivar.Os/Services/PostService.cs`** (MODIFIED)
   - Injected `ContentExtractionService`
   - Added AI extraction step in `CreatePostAsync()`
   - Added helper methods: `MergeTagsWithAiExtracted()`, `BuildBusinessMetadataFromExtraction()`

3. **`Sivar.Os/Program.cs`** (MODIFIED)
   - Added `builder.Services.AddScoped<ContentExtractionService>();`

### Configuration

The service uses the existing AI configuration from `appsettings.json`:

```json
{
  "AIServices": {
    "Provider": "Ollama",
    "OllamaEndpoint": "http://localhost:11434",
    "DefaultModel": "llama3.2"
  }
}
```

### Error Handling

- If AI extraction fails, the post is created with user-provided data only
- If AI model is unavailable, falls back to rule-based extraction
- All extraction errors are logged but don't block post creation

---
