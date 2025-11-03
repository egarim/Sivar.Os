# Profile Search Implementation Plan

> **Created**: November 2, 2025  
> **Status**: 📋 Planning Phase  
> **Priority**: 🔥 High - Core Social Feature  

---

## Executive Summary

This document outlines the implementation plan for **Profile Search** functionality in Sivar.Os. While the backend already has comprehensive search capabilities through `IProfileService`, there is **NO UI** currently available for users to search and discover profiles. This plan addresses that gap.

---

## 1. Current State Analysis

### ✅ What We Have (Backend - Complete)

The `IProfileService` already provides **4 powerful search methods**:

| Method | Purpose | Parameters | Status |
|--------|---------|------------|--------|
| `SearchProfilesAsync` | Text search by DisplayName or Bio | `searchTerm`, `page`, `pageSize` | ✅ Ready |
| `SearchProfilesByTagsAsync` | Search by tags (metadata) | `tags[]`, `matchAll`, `page`, `pageSize` | ✅ Ready |
| `GetProfilesByLocationAsync` | Search by location | `locationQuery` | ✅ Ready |
| `FindNearbyProfilesAsync` | PostGIS proximity search | `lat`, `lon`, `radiusKm`, `limit` | ✅ Ready |

**Additional Supporting Methods:**
- `GetRecentProfilesAsync` - Latest profiles
- `GetPopularProfilesAsync` - Most viewed profiles
- `GetPublicProfilesAsync` - Paginated public profiles

**Related Services Without UI:**
- `INotificationService` - Notifications system (backend complete)
- `IChatService` - Direct messaging (backend complete)
- `IActivityService` - Activity feed/timeline (backend complete)

### ❌ What We're Missing (Frontend - None)

**NO UI Components exist for:**
- Search input/form
- Search results display
- Search filters (location, tags, type)
- Profile discovery page
- Advanced search options
- Search history/suggestions

---

## 2. Implementation Architecture

### 2.1 Search Types Priority

| Priority | Search Type | Backend Method | Complexity | User Value |
|----------|-------------|----------------|------------|------------|
| 🥇 **P0** | **Basic Text Search** | `SearchProfilesAsync` | Low | High |
| 🥈 **P1** | **Location Search** | `GetProfilesByLocationAsync` | Medium | High |
| 🥉 **P2** | **Nearby Profiles (GPS)** | `FindNearbyProfilesAsync` | Medium | Medium |
| 4️⃣ **P3** | **Tag-based Search** | `SearchProfilesByTagsAsync` | Low | Medium |
| 5️⃣ **P4** | **Advanced Filters** | Multiple methods | High | Medium |

### 2.2 Component Architecture

```
📁 Sivar.Os.Client/
├── 📄 Pages/
│   ├── Search.razor ⭐ NEW - Main search page
│   └── Discover.razor ⭐ NEW - Profile discovery page
├── 📁 Components/
│   └── 📁 Search/ ⭐ NEW FOLDER
│       ├── SearchBar.razor - Reusable search input
│       ├── SearchFilters.razor - Location, tags, type filters
│       ├── SearchResults.razor - Results grid/list
│       ├── ProfileSearchCard.razor - Individual result card
│       ├── LocationSearchInput.razor - Location autocomplete
│       ├── TagSearchInput.razor - Tag chips input
│       └── EmptySearchState.razor - No results state
└── 📁 Services/
    └── SearchStateService.cs ⭐ NEW - Client-side search state management
```

### 2.3 Data Flow

```
┌─────────────────┐
│   User Input    │
│  (Search Page)  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ SearchBar.razor │ ◄──── Debounced input (300ms)
└────────┬────────┘
         │
         ▼
┌──────────────────────┐
│ SearchStateService   │ ◄──── Manages search state, filters
└────────┬─────────────┘
         │
         ▼
┌──────────────────────┐
│   IProfilesClient    │ ◄──── Calls API
└────────┬─────────────┘
         │
         ▼
┌──────────────────────┐
│ ProfilesController   │
│ (/api/profiles/...)  │
└────────┬─────────────┘
         │
         ▼
┌──────────────────────┐
│   IProfileService    │ ◄──── Business logic
└────────┬─────────────┘
         │
         ▼
┌──────────────────────┐
│ IProfileRepository   │ ◄──── Database queries
└──────────────────────┘
```

---

## 3. Phase Implementation Plan

### Phase 1: Basic Text Search (P0) 🎯 START HERE

**Goal**: Enable users to search profiles by name/bio

**Components to Create:**
1. ✨ `Search.razor` - Main search page at `/search`
2. ✨ `SearchBar.razor` - Input component with debouncing
3. ✨ `SearchResults.razor` - Results display (grid/list toggle)
4. ✨ `ProfileSearchCard.razor` - Compact profile card for results
5. ✨ `SearchStateService.cs` - Client state management

**API Endpoints to Add:**
```csharp
// Already exists in IProfilesClient interface:
Task<PagedResult<ProfileSummaryDto>> SearchProfilesAsync(string searchTerm, int page = 1, int pageSize = 20);
```

**Features:**
- Real-time search with debouncing (300ms delay)
- Pagination support (20 results per page)
- Grid/List view toggle
- Empty state when no results
- Loading spinner during search
- Display: Avatar, DisplayName, Bio (truncated), Location

**Localization:**
- `Search.resx` (English)
- `Search.es.resx` (Spanish)
- Keys: PageTitle, SearchPlaceholder, NoResults, SearchingMessage, ResultsCount

**Estimated Time**: 4-6 hours

---

### Phase 2: Location-Based Search (P1)

**Goal**: Search profiles by city/state/country

**Components to Create:**
1. ✨ `LocationSearchInput.razor` - Location autocomplete input
2. ✨ `SearchFilters.razor` - Filter panel with location filter

**API Endpoints to Add:**
```csharp
// Already exists:
Task<IEnumerable<ProfileSummaryDto>> GetProfilesByLocationAsync(string locationQuery);
```

**Features:**
- Location filter in search page
- Autocomplete suggestions (reuse Location cities if available)
- Filter by: City, State, Country
- Combine with text search
- "Near Me" quick filter (if GPS enabled)

**Localization:**
- Add to `Search.resx`: LocationFilter, NearMe, FilterByLocation

**Estimated Time**: 3-4 hours

---

### Phase 3: GPS Proximity Search (P2)

**Goal**: Find profiles near current location

**Components to Create:**
1. ✨ `NearbyProfiles.razor` - Nearby profiles section
2. ✨ Enhance `SearchFilters.razor` - Add radius slider

**API Endpoints to Add:**
```csharp
// Already exists:
Task<IEnumerable<ProfileDto>> FindNearbyProfilesAsync(double latitude, double longitude, double radiusKm = 10, int limit = 50);
```

**Features:**
- "Find Nearby" button in search
- Request GPS permissions
- Radius slider: 5km, 10km, 25km, 50km, 100km
- Map view integration (optional - Leaflet)
- Distance display in results (e.g., "2.3 km away")

**Dependencies:**
- GPS location service (already implemented in Phase 2 of LOCATION_SERVICES)
- Browser geolocation API

**Localization:**
- Add to `Search.resx`: FindNearby, Radius, Distance, KmAway

**Estimated Time**: 4-5 hours

---

### Phase 4: Tag-Based Search (P3)

**Goal**: Search profiles by metadata tags

**Components to Create:**
1. ✨ `TagSearchInput.razor` - Tag chips input with autocomplete
2. ✨ Enhance `SearchFilters.razor` - Add tag filter section

**API Endpoints to Add:**
```csharp
// Already exists:
Task<PagedResult<ProfileSummaryDto>> SearchProfilesByTagsAsync(string[] tags, bool matchAll = false, int page = 1, int pageSize = 20);
```

**Features:**
- Tag autocomplete (from existing profiles)
- Multiple tag selection (chip input)
- Match ALL vs Match ANY toggle
- Popular tags quick filters
- Tag cloud visualization (optional)

**Note**: Tags stored in Profile.Metadata (JSONB field)

**Localization:**
- Add to `Search.resx`: SearchByTags, MatchAll, MatchAny, PopularTags

**Estimated Time**: 3-4 hours

---

### Phase 5: Discovery Page (P4)

**Goal**: Browse and discover profiles without explicit search

**Components to Create:**
1. ✨ `Discover.razor` - Discovery page at `/discover`
2. ✨ `DiscoverSection.razor` - Reusable section component

**API Endpoints to Use:**
```csharp
Task<IEnumerable<ProfileSummaryDto>> GetRecentProfilesAsync(int count = 10);
Task<IEnumerable<ProfileSummaryDto>> GetPopularProfilesAsync(int count = 10);
Task<PagedResult<ProfileSummaryDto>> GetPublicProfilesAsync(int page = 1, int pageSize = 20);
```

**Features:**
- **Sections:**
  - 🆕 New Profiles (GetRecentProfilesAsync)
  - 🔥 Popular Profiles (GetPopularProfilesAsync)
  - 📍 Nearby (if GPS enabled)
  - 🏷️ Suggested Based on Tags (future AI recommendation)
- Horizontal scrollable carousels
- "See All" links to filtered search
- Random shuffle on each visit

**Localization:**
- `Discover.resx`: PageTitle, NewProfiles, PopularProfiles, SuggestedForYou

**Estimated Time**: 5-6 hours

---

### Phase 6: Advanced Search (P5)

**Goal**: Power user search with multiple filters

**Components to Create:**
1. ✨ `AdvancedSearch.razor` - Advanced search modal/page
2. ✨ Enhance `SearchFilters.razor` - Add all filter types

**Features:**
- **Filter Options:**
  - Text search (DisplayName, Bio)
  - Location (City, State, Country)
  - GPS proximity (radius)
  - Tags (match all/any)
  - Profile Type (if multiple types supported)
  - Verified profiles only
  - Active in last X days
- Save search filters
- Search history
- Quick filter presets

**Localization:**
- `AdvancedSearch.resx`: Full filter labels and descriptions

**Estimated Time**: 6-8 hours

---

## 4. Client API Requirements

### 4.1 Check IProfilesClient Interface

Current interface in `Sivar.Os.Shared/Clients/IProfilesClient.cs`:

**✅ Already Implemented:**
```csharp
Task<ProfileDto?> GetPublicProfileAsync(Guid profileId);
Task<ProfileDto?> GetProfileByIdentifierAsync(string identifier);
```

**❓ Need to Verify/Add:**
```csharp
Task<PagedResult<ProfileSummaryDto>> SearchProfilesAsync(string searchTerm, int page = 1, int pageSize = 20);
Task<PagedResult<ProfileSummaryDto>> SearchProfilesByTagsAsync(string[] tags, bool matchAll = false, int page = 1, int pageSize = 20);
Task<IEnumerable<ProfileSummaryDto>> GetProfilesByLocationAsync(string locationQuery);
Task<IEnumerable<ProfileDto>> FindNearbyProfilesAsync(double latitude, double longitude, double radiusKm = 10, int limit = 50);
Task<IEnumerable<ProfileSummaryDto>> GetRecentProfilesAsync(int count = 10);
Task<IEnumerable<ProfileSummaryDto>> GetPopularProfilesAsync(int count = 10);
Task<PagedResult<ProfileSummaryDto>> GetPublicProfilesAsync(int page = 1, int pageSize = 20);
```

### 4.2 Controller Endpoints to Add

File: `Sivar.Os/Controllers/ProfilesController.cs`

**New Endpoints Required:**

```csharp
/// <summary>
/// Searches public profiles by display name or bio
/// </summary>
[HttpGet("search")]
[AllowAnonymous]
public async Task<ActionResult<PagedResult<ProfileSummaryDto>>> SearchProfiles(
    [FromQuery] string searchTerm, 
    [FromQuery] int page = 1, 
    [FromQuery] int pageSize = 20)

/// <summary>
/// Searches profiles by tags
/// </summary>
[HttpGet("search/tags")]
[AllowAnonymous]
public async Task<ActionResult<PagedResult<ProfileSummaryDto>>> SearchByTags(
    [FromQuery] string[] tags, 
    [FromQuery] bool matchAll = false,
    [FromQuery] int page = 1, 
    [FromQuery] int pageSize = 20)

/// <summary>
/// Gets profiles by location query
/// </summary>
[HttpGet("search/location")]
[AllowAnonymous]
public async Task<ActionResult<IEnumerable<ProfileSummaryDto>>> SearchByLocation(
    [FromQuery] string locationQuery)

/// <summary>
/// Finds profiles near GPS coordinates
/// </summary>
[HttpGet("search/nearby")]
[AllowAnonymous]
public async Task<ActionResult<IEnumerable<ProfileDto>>> FindNearby(
    [FromQuery] double latitude, 
    [FromQuery] double longitude,
    [FromQuery] double radiusKm = 10,
    [FromQuery] int limit = 50)

/// <summary>
/// Gets recent public profiles
/// </summary>
[HttpGet("recent")]
[AllowAnonymous]
public async Task<ActionResult<IEnumerable<ProfileSummaryDto>>> GetRecent(
    [FromQuery] int count = 10)

/// <summary>
/// Gets popular profiles by view count
/// </summary>
[HttpGet("popular")]
[AllowAnonymous]
public async Task<ActionResult<IEnumerable<ProfileSummaryDto>>> GetPopular(
    [FromQuery] int count = 10)

/// <summary>
/// Gets paginated public profiles
/// </summary>
[HttpGet("public")]
[AllowAnonymous]
public async Task<ActionResult<PagedResult<ProfileSummaryDto>>> GetPublicProfiles(
    [FromQuery] int page = 1, 
    [FromQuery] int pageSize = 20)
```

---

## 5. UI/UX Design Specifications

### 5.1 Search Page Layout (`/search`)

```
┌─────────────────────────────────────────────────────────┐
│  Header (MainLayout)                                     │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌────────────────────────────────────────────────────┐ │
│  │  🔍 Search profiles...              [Grid] [List] │ │
│  └────────────────────────────────────────────────────┘ │
│                                                          │
│  ┌─────────────────┐  ┌──────────────────────────────┐ │
│  │   FILTERS       │  │      RESULTS                 │ │
│  │                 │  │                              │ │
│  │ 📍 Location     │  │  ┌────┐  ┌────┐  ┌────┐     │ │
│  │ 🏷️  Tags        │  │  │ P1 │  │ P2 │  │ P3 │     │ │
│  │ 📊 Sort By      │  │  └────┘  └────┘  └────┘     │ │
│  │ ⚙️  Advanced    │  │                              │ │
│  │                 │  │  ┌────┐  ┌────┐  ┌────┐     │ │
│  │ [Clear All]     │  │  │ P4 │  │ P5 │  │ P6 │     │ │
│  └─────────────────┘  │  └────┘  └────┘  └────┘     │ │
│                       │                              │ │
│                       │  Showing 1-20 of 156 results │ │
│                       │  [<] [1] [2] [3] ... [8] [>] │ │
│                       └──────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### 5.2 Profile Search Card Design

**Grid View (3-column responsive):**
```
┌──────────────────────────┐
│   ┌──────────────────┐   │
│   │   Avatar Image   │   │
│   │    (120x120)     │   │
│   └──────────────────┘   │
│                          │
│   Jose Ojeda             │ ← DisplayName (bold, 18px)
│   @jose-ojeda           │ ← Handle (gray, 14px)
│   ────────────────────  │
│   Software developer...  │ ← Bio (2 lines, truncated)
│                          │
│   📍 San Salvador, SV   │ ← Location (if available)
│   🏷️  Tech, Coffee      │ ← Tags (max 3, truncated)
│                          │
│   [View Profile]         │ ← Button
└──────────────────────────┘
```

**List View (1-column):**
```
┌────────────────────────────────────────────────────────┐
│  ┌───┐  Jose Ojeda            📍 San Salvador, SV     │
│  │ A │  @jose-ojeda           🏷️  Tech, Coffee        │
│  └───┘  Software developer passionate about...        │
│         [View Profile]                                 │
└────────────────────────────────────────────────────────┘
```

### 5.3 Color Scheme & MudBlazor Components

**Components to Use:**
- `MudTextField` - Search input
- `MudAutocomplete` - Location/tag autocomplete
- `MudSelect` - Sort, filter dropdowns
- `MudChip` - Tags display
- `MudCard` - Profile cards
- `MudPagination` - Results pagination
- `MudIconButton` - Grid/list toggle
- `MudSlider` - Radius slider (nearby search)
- `MudSkeleton` - Loading placeholders

**Icons:**
- 🔍 `Icons.Material.Filled.Search` - Search
- 📍 `Icons.Material.Filled.LocationOn` - Location
- 🏷️ `Icons.Material.Filled.Label` - Tags
- 👤 `Icons.Material.Filled.Person` - Default avatar
- ⚙️ `Icons.Material.Filled.Tune` - Filters
- 🎯 `Icons.Material.Filled.MyLocation` - GPS/Nearby

---

## 6. Performance Considerations

### 6.1 Frontend Optimization

| Technique | Implementation | Benefit |
|-----------|----------------|---------|
| **Debouncing** | 300ms delay on search input | Reduce API calls by 80% |
| **Virtual Scrolling** | MudBlazor Virtualize | Handle 1000+ results smoothly |
| **Lazy Loading** | Load images on scroll | Faster initial render |
| **Client-Side Caching** | Cache search results 5min | Instant back navigation |
| **Pagination** | Default 20 items/page | Reduce payload size |
| **Skeleton Loading** | MudSkeleton while loading | Perceived performance boost |

### 6.2 Backend Optimization (Already Implemented)

✅ PostgreSQL text search indexes on DisplayName, Bio  
✅ PostGIS spatial indexes for GeoLocation  
✅ Pagination support in all search methods  
✅ JSONB indexes for metadata/tags  

### 6.3 Monitoring & Metrics

**Track:**
- Search query frequency
- Most searched terms
- Average results per search
- Filter usage statistics
- Page load time (target: <2s)
- API response time (target: <500ms)

---

## 7. Testing Strategy

### 7.1 Unit Tests

**Client Components:**
- SearchBar input validation
- SearchFilters state management
- SearchResults pagination logic
- ProfileSearchCard rendering

**Services:**
- SearchStateService filter combinations
- Debouncing logic
- Cache expiration

### 7.2 Integration Tests

**API Endpoints:**
- Search with pagination
- Location-based search
- GPS proximity search
- Tag-based search (match all/any)
- Empty result handling
- Invalid input handling

### 7.3 E2E Tests (Playwright)

**User Flows:**
1. User searches for "jose" → sees results → clicks profile
2. User filters by location → results update
3. User enables GPS → sees nearby profiles
4. User searches by tags → applies multiple tags
5. User toggles grid/list view → layout changes
6. User navigates pagination → results load

---

## 8. Accessibility (a11y) Requirements

✅ **Keyboard Navigation:**
- Tab through search input → filters → results → pagination
- Enter to submit search
- Arrow keys for navigation

✅ **Screen Reader Support:**
- ARIA labels on search input
- Result count announcements
- Loading state announcements
- Empty state descriptions

✅ **Visual:**
- Minimum 4.5:1 contrast ratio
- Focus indicators on interactive elements
- Alt text on profile avatars

---

## 9. Localization Checklist

Following DEVELOPMENT_RULES.md Section 5:

### Files to Create:

1. ✅ `Search.resx` (English)
2. ✅ `Search.es.resx` (Spanish)
3. ✅ `Discover.resx` (English)
4. ✅ `Discover.es.resx` (Spanish)
5. ✅ `SearchBar.resx` (English)
6. ✅ `SearchBar.es.resx` (Spanish)
7. ✅ `SearchFilters.resx` (English)
8. ✅ `SearchFilters.es.resx` (Spanish)

### Resource Keys Needed:

**Search.resx:**
```xml
<data name="PageTitle" xml:space="preserve"><value>Search Profiles</value></data>
<data name="SearchPlaceholder" xml:space="preserve"><value>Search by name or bio...</value></data>
<data name="NoResults" xml:space="preserve"><value>No profiles found matching your search</value></data>
<data name="SearchingMessage" xml:space="preserve"><value>Searching...</value></data>
<data name="ResultsCount" xml:space="preserve"><value>Found {0} profile(s)</value></data>
<data name="GridView" xml:space="preserve"><value>Grid View</value></data>
<data name="ListView" xml:space="preserve"><value>List View</value></data>
```

**Search.es.resx:**
```xml
<data name="PageTitle" xml:space="preserve"><value>Buscar Perfiles</value></data>
<data name="SearchPlaceholder" xml:space="preserve"><value>Buscar por nombre o biografía...</value></data>
<data name="NoResults" xml:space="preserve"><value>No se encontraron perfiles</value></data>
<data name="SearchingMessage" xml:space="preserve"><value>Buscando...</value></data>
<data name="ResultsCount" xml:space="preserve"><value>Se encontraron {0} perfil(es)</value></data>
<data name="GridView" xml:space="preserve"><value>Vista de Cuadrícula</value></data>
<data name="ListView" xml:space="preserve"><value>Vista de Lista</value></data>
```

---

## 10. Implementation Roadmap

### Week 1: Foundation (Phase 1 - Basic Search)
- ✅ Day 1-2: API endpoints + IProfilesClient updates
- ✅ Day 3-4: Search.razor page + SearchBar component
- ✅ Day 5: SearchResults + ProfileSearchCard components
- ✅ Day 6: Localization + Testing
- ✅ Day 7: Polish + Bug fixes

### Week 2: Location Features (Phase 2 + 3)
- ✅ Day 1-2: LocationSearchInput + SearchFilters
- ✅ Day 3-4: GPS proximity search integration
- ✅ Day 5-6: Map integration (optional)
- ✅ Day 7: Testing + Refinement

### Week 3: Advanced Features (Phase 4 + 5)
- ✅ Day 1-2: Tag-based search
- ✅ Day 3-5: Discover page with sections
- ✅ Day 6-7: Advanced search filters

### Week 4: Polish & Launch (Phase 6)
- ✅ Day 1-3: Performance optimization
- ✅ Day 4-5: E2E testing
- ✅ Day 6: Documentation updates
- ✅ Day 7: Production deployment

---

## 11. Success Metrics

**Launch Targets (Week 4):**
- ✅ 90%+ user satisfaction (usability testing)
- ✅ <2s average page load time
- ✅ <500ms average API response time
- ✅ Zero accessibility violations (aXe audit)
- ✅ 100% localization coverage (EN + ES)

**Post-Launch (Month 1):**
- 📊 50%+ of users try search feature
- 📊 30%+ search-to-profile-view conversion
- 📊 20+ average searches per day
- 📊 <5% bounce rate on search page

---

## 12. Dependencies & Blockers

### External Dependencies:
- ✅ IProfileService (complete)
- ✅ PostgreSQL full-text search (configured)
- ✅ PostGIS for spatial queries (configured)
- ✅ MudBlazor v7+ (installed)

### Potential Blockers:
- ❓ GPS permissions UX (requires user approval)
- ❓ Performance with 10,000+ profiles (needs load testing)
- ❓ Tag autocomplete data source (requires aggregation query)

---

## 13. Related Features to Consider

**Future Enhancements (Post-Launch):**
1. 🤖 **AI-Powered Search**: Semantic search using vector embeddings (IClientEmbeddingService available)
2. 💬 **Search Suggestions**: Autocomplete based on popular searches
3. 🔔 **Saved Searches**: Save filters + get notifications for new matches
4. 📊 **Search Analytics Dashboard**: Admin view of search patterns
5. 🎯 **Profile Recommendations**: ML-based "Profiles you may like"
6. 🌐 **Multi-Language Bio Search**: Search across translated bios

---

## 14. Documentation Updates Required

### Files to Update:
1. ✅ `DEVELOPMENT_RULES.md` - Add Search component guidelines
2. ✅ `README.md` - Add search feature to feature list
3. ✅ Create `SEARCH_FEATURE_GUIDE.md` - User-facing documentation
4. ✅ Update API documentation (Swagger)

---

## 15. Questions & Decisions Needed

### Architecture Decisions:
1. **Search State Management**: Client-side service vs server-side session?
   - ✅ **Decision**: Client-side SearchStateService (better UX, instant filters)

2. **Results Display Default**: Grid vs List view?
   - ❓ **TBD**: User preference saved in localStorage

3. **GPS Permissions**: Request on page load vs on "Find Nearby" click?
   - ✅ **Decision**: On click (less intrusive)

4. **Tag Autocomplete**: Where to get tag suggestions?
   - ❓ **TBD**: Aggregate from Profile.Metadata JSONB field

5. **Search History**: Store in browser or database?
   - ❓ **TBD**: localStorage for now, database later

---

## 16. Next Steps

### Immediate Actions:
1. ✅ **Review this plan** with team/stakeholders
2. ✅ **Approve Phase 1 scope** (Basic Text Search)
3. ✅ **Create git branch**: `feature/profile-search`
4. ✅ **Update IProfilesClient interface** with search methods
5. ✅ **Add controller endpoints** to ProfilesController
6. ✅ **Start Phase 1 implementation**

### Ready to Start? 🚀

**Recommended Start Command:**
```bash
git checkout -b feature/profile-search
```

**First File to Create:**
```
Sivar.Os.Client/Pages/Search.razor
```

---

## 17. Summary

**Problem**: Backend has robust profile search (7 methods), but **ZERO UI** exists for users to search profiles.

**Solution**: Implement progressive search UI starting with basic text search, then add location, GPS, tags, and discovery features.

**Impact**: 
- Enable core social discovery feature
- Improve user engagement (+50% target)
- Leverage existing backend investment
- Complete the profile discovery loop

**Effort**: ~3-4 weeks (60-80 hours)

**Risk**: Low - Backend complete, frontend components well-defined

**Priority**: 🔥 **HIGH** - Essential for social platform functionality

---

_Ready to implement? Let's start with Phase 1! 🎯_
