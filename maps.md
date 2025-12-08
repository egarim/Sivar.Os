# Maps in Posts Implementation Plan

**Date:** December 8, 2025  
**Feature:** Map display for location-based posts  
**Target Post Types:** BusinessLocation, Place (future), Office, Branch  
**Map Library:** Leaflet.js + OpenStreetMap (FREE, no API key)

---

## 1. Overview

### 1.1 Goal
Display interactive maps for posts that have location data. This applies to:
- **BusinessLocation** posts (offices, branches, stores, warehouses)
- **Future "Place" post type** (restaurants, landmarks, points of interest)
- Any post with `Location` (latitude/longitude) data

### 1.2 User Stories
1. As a user viewing a BusinessLocation post, I want to see a map showing the exact location
2. As a user creating a BusinessLocation post, I want to pick the location on a map or search by address
3. As a user viewing a profile's locations tab, I want to see all their locations on a single map
4. As a user, I want to get directions to a location from the post

### 1.3 Existing Infrastructure
- ✅ `Location` value object with City, State, Country, Latitude, Longitude
- ✅ `Post.Location` property for storing location data
- ✅ `Post.GeoLocation` for PostGIS spatial queries (POINT format)
- ✅ `BusinessLocationMetadata` with contact info, hours, location type
- ✅ `BusinessLocationType` enum (MainOffice, CustomerBranch, RetailStore, etc.)
- ✅ `LEAFLET_INTEGRATION_GUIDE.md` with implementation patterns
- ❌ Leaflet.js not yet integrated
- ❌ Map components not yet created

---

## 2. Implementation Phases

### Phase 1: Leaflet.js Integration (Foundation)
**Goal:** Set up Leaflet.js in the Blazor application

#### Tasks:
1. **Add Leaflet CDN to App.razor**
   - Add Leaflet CSS to `<head>`
   - Add Leaflet JS before `</body>`
   - Location: `Sivar.Os/Components/App.razor`

2. **Create JavaScript interop file**
   - Create `wwwroot/js/leaflet-interop.js`
   - Functions: `initializeMap`, `updateMapMarker`, `displayLocations`, `destroyMap`
   - Add script reference to App.razor

3. **Create base MapComponent.razor**
   - Reusable component for displaying a single map
   - Supports: center point, zoom level, marker, popup
   - Location: `Sivar.Os.Client/Components/Maps/MapComponent.razor`

### Phase 2: Location Display in Posts (Read-only)
**Goal:** Show maps in BusinessLocation posts when viewing

#### Tasks:
1. **Create PostLocationMap.razor component**
   - Displays location from PostDto
   - Shows marker with popup (title, address)
   - "Get Directions" link (opens Google Maps/Apple Maps)
   - Location: `Sivar.Os.Client/Components/Feed/PostLocationMap.razor`

2. **Integrate into PostCard.razor**
   - Detect when post has Location with lat/lng
   - Show map below content for BusinessLocation posts
   - Collapse by default with "Show Map" button for performance
   - Only load map when expanded (lazy loading)

3. **Create PostLocationMapStyles.css**
   - Map container styling
   - Responsive height/width
   - Loading skeleton

### Phase 3: Location Picker for Post Creation (Write)
**Goal:** Allow users to set location when creating BusinessLocation posts

#### Tasks:
1. **Create LocationPicker.razor component**
   - Search by address (uses Nominatim API via backend)
   - Click on map to set location
   - Draggable marker
   - Display selected address
   - Location: `Sivar.Os.Client/Components/Maps/LocationPicker.razor`

2. **Integrate into PostEditModal.razor**
   - Show LocationPicker for BusinessLocation post type
   - Bind selected location to CreatePostDto
   - Validate location is set before submit

3. **Backend: Geocoding endpoint**
   - Create `LocationController.cs` with endpoints:
     - `POST /api/location/geocode` (address → lat/lng)
     - `POST /api/location/reverse-geocode` (lat/lng → address)
   - Use existing `NominatimService` or similar

### Phase 4: Multi-Location Map View
**Goal:** Display all locations from a profile on a single map

#### Tasks:
1. **Create ProfileLocationsMap.razor component**
   - Fetches all BusinessLocation posts for a profile
   - Displays markers for each location
   - Marker popups with post title and link
   - Auto-zoom to fit all markers
   - Location: `Sivar.Os.Client/Components/Profile/ProfileLocationsMap.razor`

2. **Add "Locations" tab to ProfilePage**
   - New tab in ProfileContentTabs
   - Shows ProfileLocationsMap + list of locations
   - Filter by BusinessLocationType

3. **API: Get locations for profile**
   - Add endpoint: `GET /api/posts/profile/{profileId}/locations`
   - Returns PostDto[] filtered to BusinessLocation type with Location data

### Phase 5: Nearby Locations Search (Future)
**Goal:** Find posts/profiles near a location

#### Tasks:
1. **PostGIS spatial queries**
   - Already have GeoLocation column
   - Create repository method: `GetPostsNearLocationAsync(lat, lng, radiusKm)`

2. **Nearby search UI**
   - "Find Nearby" feature on home/search page
   - Use browser geolocation or manual input

---

## 3. Component Architecture

```
Components/
├── Maps/
│   ├── MapComponent.razor           # Base reusable map
│   ├── LocationPicker.razor         # For selecting/searching locations
│   └── MultiLocationMap.razor       # For displaying multiple markers
├── Feed/
│   └── PostLocationMap.razor        # Map display within a post
└── Profile/
    └── ProfileLocationsMap.razor    # All locations for a profile
```

---

## 4. Data Flow

### 4.1 Viewing a Post with Location
```
PostCard.razor
    └── if (post.PostType == BusinessLocation && post.Location?.Latitude != null)
            └── <PostLocationMap Post="@post" />
                    └── JS: initializeMap(mapId, lat, lng)
                            └── Leaflet renders OpenStreetMap tiles
```

### 4.2 Creating a Post with Location
```
PostEditModal.razor
    └── if (PostType == BusinessLocation)
            └── <LocationPicker @bind-SelectedLocation="location" />
                    ├── User searches address
                    │       └── API: /api/location/geocode
                    │               └── Backend calls Nominatim
                    └── User clicks map
                            └── JS: OnMapClick → Blazor: ReverseGeocode
                                    └── API: /api/location/reverse-geocode
```

---

## 5. JavaScript Interop API

### 5.1 leaflet-interop.js Functions

```javascript
// Initialize a new map instance
window.initializeMap(mapId, lat, lng, zoom, options)

// Update marker position
window.updateMapMarker(mapId, lat, lng)

// Display multiple locations with markers
window.displayLocations(mapId, locations[])
// locations: [{ lat, lng, title, description, iconType }]

// Destroy map instance (cleanup)
window.destroyMap(mapId)

// Fit map bounds to show all markers
window.fitMapBounds(mapId)

// Get current map center
window.getMapCenter(mapId) → { lat, lng }
```

---

## 6. PostLocationMap Component Specification

### 6.1 Input Parameters
```csharp
[Parameter] public PostDto Post { get; set; }
[Parameter] public bool ShowDirectionsLink { get; set; } = true;
[Parameter] public bool ExpandedByDefault { get; set; } = false;
[Parameter] public int Height { get; set; } = 200; // pixels
```

### 6.2 Behavior
- Lazy load: Map initializes only when visible (IntersectionObserver or manual expand)
- Cleanup: Dispose map on component disposal
- Mobile: Full-width, touch-friendly controls
- Popup: Shows post title, address, business hours (if available)
- Directions: "Get Directions" button opens external maps app

### 6.3 BusinessLocation Marker Icons
Different icons based on `BusinessLocationType`:
- 🏢 MainOffice (blue marker)
- 🏪 CustomerBranch (green marker)
- 📋 AdministrativeOffice (gray marker)
- 🛒 RetailStore (orange marker)
- 📦 Warehouse (brown marker)
- 🔧 ServiceCenter (red marker)

---

## 7. LocationPicker Component Specification

### 7.1 Input/Output Parameters
```csharp
[Parameter] public Location? SelectedLocation { get; set; }
[Parameter] public EventCallback<Location?> SelectedLocationChanged { get; set; }
[Parameter] public string Placeholder { get; set; } = "Search address...";
[Parameter] public bool Required { get; set; } = false;
```

### 7.2 Features
- Address search with autocomplete (debounced)
- Click-to-select on map
- Draggable marker
- "Use My Location" button (browser geolocation)
- Clear button
- Display formatted address when selected

---

## 8. API Endpoints

### 8.1 LocationController.cs

```csharp
[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    // POST /api/location/geocode
    // Body: { "address": "123 Main St, City, Country" }
    // Returns: { latitude, longitude, formattedAddress, city, state, country }
    
    // POST /api/location/reverse-geocode
    // Body: { "latitude": 40.7128, "longitude": -74.0060 }
    // Returns: { formattedAddress, city, state, country }
}
```

### 8.2 PostsController.cs Additions

```csharp
// GET /api/posts/profile/{profileId}/locations
// Returns all BusinessLocation posts with coordinates for map display
```

---

## 9. Styling

### 9.1 Map Container CSS

```css
.post-location-map {
    width: 100%;
    height: 200px;
    border-radius: 8px;
    overflow: hidden;
    margin-top: 12px;
}

.post-location-map.expanded {
    height: 300px;
}

.location-picker-map {
    width: 100%;
    height: 400px;
    border-radius: 8px;
    border: 1px solid var(--mud-palette-lines-default);
}

.map-loading-skeleton {
    background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
    background-size: 200% 100%;
    animation: shimmer 1.5s infinite;
}
```

---

## 10. Implementation Order

### Sprint 1: Foundation (2-3 days) ✅ COMPLETE
- [x] Add Leaflet CDN to App.razor
- [x] Create `wwwroot/js/leaflet-interop.js` with basic functions
- [x] Create `MapComponent.razor` base component
- [ ] Test: Display simple map with single marker

### Sprint 2: Post Map Display (2 days) ✅ COMPLETE
- [x] Create `PostLocationMap.razor`
- [x] Integrate into `PostCard.razor` for BusinessLocation posts
- [x] Add "Get Directions" link
- [x] Add expand/collapse for performance
- [x] Add localization (English/Spanish)

### Sprint 3: Location Picker (2-3 days)
- [ ] Create `LocationController.cs` with geocoding endpoints
- [ ] Create `LocationPicker.razor` component
- [ ] Integrate into `PostEditModal.razor`
- [ ] Test: Create BusinessLocation post with map-selected location

### Sprint 4: Profile Locations (1-2 days)
- [ ] Create `ProfileLocationsMap.razor`
- [ ] Add API endpoint for profile locations
- [ ] Add "Locations" tab to ProfileContentTabs
- [ ] Test: View all business locations on map

### Sprint 5: Polish & Mobile (1 day)
- [ ] Mobile-responsive map sizing
- [ ] Touch-friendly controls
- [ ] Loading states and error handling
- [ ] Localization for map UI strings

---

## 11. Dependencies

### 11.1 External
- Leaflet.js 1.9.4 (CDN)
- OpenStreetMap tiles (free, no API key)
- Nominatim geocoding (backend only, rate-limited)

### 11.2 Internal
- `Location` value object (exists)
- `Post.Location` property (exists)
- `BusinessLocationMetadata` (exists)
- `PostService` (exists)
- `ILocationService` (may need to create or extend)

---

## 12. Testing Plan

### 12.1 Unit Tests
- [ ] MapComponent renders correctly
- [ ] LocationPicker binds location properly
- [ ] PostLocationMap extracts coordinates from PostDto

### 12.2 Integration Tests
- [ ] Geocoding API returns valid coordinates
- [ ] Reverse geocoding returns address
- [ ] Profile locations endpoint returns BusinessLocation posts

### 12.3 E2E Tests
- [ ] Create BusinessLocation post with location
- [ ] View post and see map
- [ ] Click "Get Directions" opens external app
- [ ] View profile locations tab

---

## 13. Future Enhancements

1. **Clustering** - Group nearby markers on zoom out
2. **Heatmaps** - Show activity density
3. **Route planning** - Multiple location itinerary
4. **Offline maps** - Cache tiles for mobile
5. **Custom map styles** - Dark mode, branded tiles
6. **Place posts** - New PostType for points of interest (non-business)

---

## 14. Files to Create/Modify

### New Files
```
Sivar.Os/
├── wwwroot/js/leaflet-interop.js
└── Components/App.razor (modify - add Leaflet CDN)

Sivar.Os.Client/
├── Components/
│   ├── Maps/
│   │   ├── MapComponent.razor
│   │   ├── MapComponent.razor.css
│   │   ├── LocationPicker.razor
│   │   └── LocationPicker.razor.css
│   ├── Feed/
│   │   ├── PostLocationMap.razor
│   │   └── PostLocationMap.razor.css
│   └── Profile/
│       └── ProfileLocationsMap.razor
└── Resources/
    └── Components/Maps/
        ├── MapComponent.resx
        └── MapComponent.es.resx

Sivar.Os/
└── Controllers/
    └── LocationController.cs
```

### Modify Files
```
Sivar.Os.Client/
├── Components/Feed/PostCard.razor  (add map integration)
└── Components/Feed/PostEditModal.razor (add location picker)

Sivar.Os/
└── Controllers/PostsController.cs (add locations endpoint)
```

---

## 15. Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Leaflet conflicts with Blazor rendering | Use unique mapId per instance, proper cleanup |
| Nominatim rate limits | Cache results, implement request throttling |
| Mobile performance | Lazy load maps, reduce tile quality on mobile |
| Map not displaying | Fallback to static image or address text |

---

## Ready to Start?

Begin with **Sprint 1: Foundation** - adding Leaflet.js and creating the base MapComponent.
