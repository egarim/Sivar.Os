# Sivar.Os AI Assistant Demo Data Plan

## System Audit Results ✅

### Entities & Capabilities Verified

| Component | Status | Notes |
|-----------|--------|-------|
| **PostType.BusinessLocation** | ✅ Ready | For restaurants, shops, government offices |
| **PostType.Service** | ✅ Ready | For government procedures, professional services |
| **PostType.Event** | ✅ Ready | For activities, entertainment, happenings |
| **Location (City, State, Country, Lat/Long)** | ✅ Ready | Value object with all needed fields |
| **GeoLocation (PostGIS POINT)** | ✅ Ready | Spatial queries working |
| **BusinessLocationMetadata** | ✅ Ready | Hours, contact, walk-ins, appointments |
| **ServiceMetadata** | ✅ Ready | Duration, requirements, booking instructions |
| **BusinessHours / DaySchedule** | ✅ Ready | Full weekly schedule with breaks |
| **Tags array** | ✅ Ready | For categorization (cuisine, service type) |
| **PricingInfo** | ✅ Ready | Amount, currency (USD/SVC/EUR), negotiable |
| **Profile.Location** | ✅ Ready | Profiles can have locations too |
| **Profile.Tags** | ✅ Ready | Business profiles can be tagged |

### AI Functions Available (13 total)

| Function | Use Case |
|----------|----------|
| `SearchNearbyPosts` | "Restaurants near me" |
| `SearchNearbyProfiles` | "Businesses near me" |
| `SearchPosts` | "Find pupusa restaurants" |
| `SearchProfiles` | "Find government offices" |
| `FindBusinesses` | "Find banks in Escalón" |
| `GetPostDetails` | Full info on a specific post |
| `GetAddressFromCoordinates` | Reverse geocoding |
| `GetCoordinatesFromAddress` | Geocoding addresses |
| `CalculateDistance` | Distance between locations |
| `SearchNearMe` | User's current location search |

### Data Structures for Demo Data

**BusinessLocationMetadata** (for restaurants, offices):
```json
{
  "LocationType": "RetailStore",
  "Description": "Traditional Salvadoran cuisine",
  "ContactPhone": "+503 2222-3333",
  "ContactEmail": "info@example.com",
  "AcceptsWalkIns": true,
  "RequiresAppointment": false,
  "WorkingHours": {
    "Monday": { "OpenTime": "08:00", "CloseTime": "20:00" },
    ...
  },
  "SpecialInstructions": "Parking available"
}
```

**ServiceMetadata** (for government procedures):
```json
{
  "Category": "Government",
  "DurationMinutes": 60,
  "Requirements": "DUI, birth certificate, proof of address",
  "IncludedFeatures": ["Photo", "Fingerprints", "Card printing"],
  "BookingInstructions": "Take a number, wait in line",
  "RequiresConsultation": false
}
```

### Gaps Identified: NONE

All required entities and fields exist. The system is fully capable of supporting:
- ✅ Restaurant/food discovery with hours and cuisine tags
- ✅ Government office locations with hours
- ✅ Government procedure guides with requirements
- ✅ Event listings with dates
- ✅ Spatial "near me" queries
- ✅ Service provider discovery

---

## Vision

Transform Sivar.Os into an **AI-powered local directory and assistant** for El Salvador (and Central America). The chatbot helps users:

1. **Find things to DO** - Events, activities, entertainment
2. **Find places to EAT** - Restaurants, cafes, food trucks, street food
3. **Find places to GO** - Tourist spots, parks, beaches, landmarks
4. **Run ERRANDS** - Government procedures, banks, services, utilities

## Use Cases

### 1. 🍽️ FOOD & DINING
**User queries:**
- "Where can I eat pupusas near me?"
- "Find a good Italian restaurant in Zona Rosa"
- "What's open for breakfast in Santa Tecla?"
- "Best coffee shops in San Salvador"

**Data needed:**
- Restaurants with cuisine type, price range, hours
- Cafes and coffee shops
- Street food vendors
- Food trucks with schedules

### 2. 🎉 THINGS TO DO / EVENTS
**User queries:**
- "What events are happening this weekend?"
- "Find live music venues in San Salvador"
- "Where can I go hiking near the city?"
- "Family activities for kids"

**Data needed:**
- Event venues with schedules
- Entertainment locations
- Outdoor activities
- Family-friendly spots

### 3. 📍 PLACES TO VISIT
**User queries:**
- "Tourist attractions near San Salvador"
- "Best beaches in El Salvador"
- "Historical sites to visit"
- "Where can I see the volcanoes?"

**Data needed:**
- Tourist attractions
- Natural landmarks
- Historical sites
- Beaches and parks

### 4. 🏛️ GOVERNMENT ERRANDS
**User queries:**
- "How do I get a DUI (ID card)?"
- "Where is the nearest DGT office?"
- "What documents do I need for a passport?"
- "How do I register a vehicle?"

**Data needed:**
- Government offices with addresses
- Required documents lists
- Procedures step-by-step
- Hours of operation

### 5. 💼 SERVICES & UTILITIES
**User queries:**
- "Where is the nearest bank?"
- "Find a notary in San Salvador"
- "Where can I pay my electricity bill?"
- "Pharmacies open 24 hours"

**Data needed:**
- Banks and ATMs
- Professional services
- Utility payment centers
- Healthcare facilities

---

## Demo Data Structure

### Profile Types to Use:
- **BusinessProfile** (type ID: 22222222-2222-2222-2222-222222222222)
- **OrganizationProfile** (type ID: 33333333-3333-3333-3333-333333333333)

### Post Types to Use:
- `BusinessLocation` (2) - For physical business locations
- `Service` (4) - For services offered
- `Event` (5) - For events and activities
- `General` (1) - For informational posts

### Location Data (PostGIS):
All entries will have accurate GPS coordinates for spatial queries.

---

## Demo Data Categories

### A. Restaurants & Food (50 entries)

#### Distribution by Cuisine (5 each)

| # | Cuisine | Count | Primary Areas |
|---|---------|-------|---------------|
| 1 | Salvadoran/Típico | 5 | SS Centro, Santa Tecla, Santa Ana, La Libertad |
| 2 | Mexican | 5 | Zona Rosa, Escalón, Santa Ana |
| 3 | American | 5 | Multiplaza, Galerías, Gran Vía |
| 4 | Italian | 5 | San Benito, Escalón, Santa Elena |
| 5 | Asian (Sushi/Chinese/Thai) | 5 | Multiplaza, Zona Rosa, Gran Vía |
| 6 | Seafood/Mariscos | 5 | La Libertad, Zona Rosa |
| 7 | Steakhouse/Parrillada | 5 | Zona Rosa, Escalón, Santa Ana |
| 8 | Fast Food | 5 | Multiple locations |
| 9 | Café/Bakery | 5 | All areas |
| 10 | Vegetarian/Vegan | 5 | San Benito, Escalón |
| | **TOTAL** | **50** | |

#### Geographic Coverage

| Area | Department | Approx. Restaurants |
|------|------------|---------------------|
| San Salvador Centro | San Salvador | 8 |
| Zona Rosa / San Benito | San Salvador | 8 |
| Colonia Escalón | San Salvador | 6 |
| Santa Tecla | La Libertad | 5 |
| Antiguo Cuscatlán | La Libertad | 5 |
| Multiplaza / Galerías | San Salvador | 5 |
| La Libertad (beaches) | La Libertad | 6 |
| Santa Ana | Santa Ana | 7 |
| **TOTAL** | | **50** |

#### Price Ranges

| Symbol | Range (USD) | Example | Tag |
|--------|-------------|---------|-----|
| $ | Under $5 | Street food, pupuserías | `price-budget` |
| $$ | $5-15 | Casual dining | `price-moderate` |
| $$$ | $15-30 | Nice restaurants | `price-upscale` |
| $$$$ | $30+ | Fine dining | `price-fine-dining` |

Currency: **USD** (El Salvador uses USD)

#### Business Hours Patterns

| Type | Hours | Days |
|------|-------|------|
| Breakfast spots | 6:00 AM - 2:00 PM | Daily |
| Lunch restaurants | 11:00 AM - 3:00 PM | Mon-Sat |
| Dinner restaurants | 5:00 PM - 10:00 PM | Daily |
| All-day dining | 7:00 AM - 10:00 PM | Daily |
| 24-hour | Always open | Daily |
| Bars/Late night | 6:00 PM - 2:00 AM | Wed-Sun |

#### Images Strategy

Using placeholder service: `https://placehold.co/800x600/[color]/white?text=[Name]`

Colors by cuisine:
- Salvadoran: `e63946` (red)
- Mexican: `f4a261` (orange)
- American: `2a9d8f` (teal)
- Italian: `e9c46a` (yellow)
- Asian: `9b2226` (dark red)
- Seafood: `0077b6` (blue)
- Steakhouse: `6d4c41` (brown)
- Fast Food: `ff6b6b` (coral)
- Café: `8d6e63` (coffee)
- Vegetarian: `52b788` (green)

---

### Complete Restaurant List (50 entries)

#### 1. Salvadoran/Típico (5)

| Name | Area | Price | Hours | Coordinates |
|------|------|-------|-------|-------------|
| Pupusería El Comalito | SS Centro | $ | 6am-9pm | 13.6989, -89.1914 |
| Típicos Doña Maria | Santa Tecla | $ | 7am-8pm | 13.6714, -89.2897 |
| La Casita del Maíz | Santa Ana | $ | 6am-9pm | 13.9940, -89.5590 |
| Pupusería La Bendición | La Libertad | $ | 7am-10pm | 13.4883, -89.3228 |
| Restaurante El Volcán | Escalón | $$ | 11am-10pm | 13.7052, -89.2456 |

#### 2. Mexican (5)

| Name | Area | Price | Hours | Coordinates |
|------|------|-------|-------|-------------|
| Tacos El Charro | Zona Rosa | $$ | 12pm-11pm | 13.6834, -89.2234 |
| La Cantina Mexicana | Escalón | $$ | 11am-10pm | 13.7023, -89.2412 |
| Burritos & Más | Multiplaza | $$ | 10am-9pm | 13.6823, -89.2367 |
| El Mariachi Loco | Santa Ana | $$ | 12pm-11pm | 13.9920, -89.5610 |
| Taquería Don Pancho | San Benito | $$ | 11am-11pm | 13.6856, -89.2298 |

#### 3. American (5)

| Name | Area | Price | Hours | Coordinates |
|------|------|-------|-------|-------------|
| The Burger Joint | Multiplaza | $$ | 11am-10pm | 13.6819, -89.2371 |
| Wings & Things | Galerías | $$ | 12pm-10pm | 13.7034, -89.2234 |
| BBQ Smokehouse | Gran Vía | $$$ | 12pm-10pm | 13.6712, -89.2534 |
| Diner Americano | Escalón | $$ | 7am-10pm | 13.7045, -89.2445 |
| Steak & Shake | Santa Elena | $$ | 10am-11pm | 13.6745, -89.2556 |

#### 4. Italian (5)

| Name | Area | Price | Hours | Coordinates |
|------|------|-------|-------|-------------|
| Trattoria Bella Italia | San Benito | $$$ | 12pm-10pm | 13.6845, -89.2310 |
| Pizzería Napoli | Escalón | $$ | 11am-10pm | 13.7030, -89.2420 |
| Pasta Fresca | Santa Elena | $$$ | 12pm-10pm | 13.6750, -89.2550 |
| Il Forno | Zona Rosa | $$$ | 6pm-11pm | 13.6798, -89.2345 |
| Ristorante Milano | Antiguo Cuscatlán | $$$$ | 7pm-11pm | 13.6720, -89.2540 |

#### 5. Asian (5)

| Name | Area | Price | Hours | Coordinates |
|------|------|-------|-------|-------------|
| Sushi House | Multiplaza | $$$ | 12pm-10pm | 13.6825, -89.2370 |
| Dragon Palace | Zona Rosa | $$ | 11am-10pm | 13.6790, -89.2350 |
| Thai Garden | Gran Vía | $$$ | 12pm-10pm | 13.6715, -89.2530 |
| Noodle Bar | San Benito | $$ | 11am-9pm | 13.6860, -89.2295 |
| Ramen House | Escalón | $$ | 12pm-9pm | 13.7040, -89.2430 |

#### 6. Seafood/Mariscos (5)

| Name | Area | Price | Hours | Coordinates |
|------|------|-------|-------|-------------|
| Mariscos El Puerto | La Libertad | $$ | 10am-8pm | 13.4890, -89.3220 |
| Cevichería La Ola | La Libertad | $$ | 9am-6pm | 13.4950, -89.3890 |
| El Pescador | Zona Rosa | $$$ | 12pm-10pm | 13.6795, -89.2340 |
| Marisquería Costanera | La Libertad | $$ | 8am-7pm | 13.4870, -89.3210 |
| Lobster House | San Benito | $$$$ | 6pm-11pm | 13.6840, -89.2305 |

#### 7. Steakhouse/Parrillada (5)

| Name | Area | Price | Hours | Coordinates |
|------|------|-------|-------|-------------|
| La Parrilla Argentina | Zona Rosa | $$$ | 12pm-11pm | 13.6787, -89.2365 |
| El Gaucho | Escalón | $$$$ | 6pm-11pm | 13.7050, -89.2450 |
| Rancho Grande | Santa Ana | $$$ | 12pm-10pm | 13.9935, -89.5605 |
| Carnes y Brasas | San Benito | $$$ | 12pm-10pm | 13.6850, -89.2300 |
| Asador El Toro | Santa Tecla | $$$ | 12pm-10pm | 13.6720, -89.2890 |

#### 8. Fast Food (5)

| Name | Area | Price | Hours | Coordinates |
|------|------|-------|-------|-------------|
| Pollo Campestre Centro | SS Centro | $ | 7am-10pm | 13.6976, -89.1923 |
| Burger Palace | Multiplaza | $ | 10am-10pm | 13.6820, -89.2368 |
| Pizza Rápida | Galerías | $ | 10am-10pm | 13.7032, -89.2230 |
| Hot Dogs Express | Santa Tecla | $ | 10am-9pm | 13.6710, -89.2895 |
| Tacos Express | Santa Ana | $ | 11am-10pm | 13.9945, -89.5595 |

#### 9. Café/Bakery (5)

| Name | Area | Price | Hours | Coordinates |
|------|------|-------|-------|-------------|
| Café del Centro | SS Centro | $ | 6am-6pm | 13.6985, -89.1910 |
| La Panadería Francesa | Escalón | $$ | 7am-7pm | 13.7048, -89.2448 |
| Coffee & Art | San Benito | $$ | 7am-8pm | 13.6858, -89.2300 |
| Dulce Tentación | Santa Ana | $ | 6am-8pm | 13.9938, -89.5602 |
| Café Vista al Mar | La Libertad | $$ | 7am-6pm | 13.4885, -89.3225 |

#### 10. Vegetarian/Vegan (5)

| Name | Area | Price | Hours | Coordinates |
|------|------|-------|-------|-------------|
| Green Garden | San Benito | $$ | 8am-8pm | 13.6855, -89.2295 |
| Vida Natural | Escalón | $$ | 7am-7pm | 13.7042, -89.2438 |
| El Jardín Vegano | Santa Tecla | $$ | 9am-6pm | 13.6718, -89.2892 |
| Roots & Leaves | Zona Rosa | $$$ | 11am-9pm | 13.6792, -89.2348 |
| Semillas Café | Antiguo Cuscatlán | $$ | 7am-5pm | 13.6708, -89.2538 |

---

## JSON Import Structure

### File: `demo-data-restaurants.json`

```json
{
  "metadata": {
    "version": "1.0",
    "generatedAt": "2024-12-09",
    "category": "restaurants",
    "totalProfiles": 50,
    "totalPosts": 50
  },
  "profiles": [
    {
      "id": "uuid-here",
      "profileTypeId": "22222222-2222-2222-2222-222222222222",
      "displayName": "Pupusería El Comalito",
      "handle": "pupuseria-el-comalito",
      "bio": "Auténticas pupusas salvadoreñas desde 1985. Especialidad en pupusas de chicharrón, queso y loroco.",
      "location": {
        "city": "San Salvador",
        "state": "San Salvador",
        "country": "El Salvador",
        "latitude": 13.6989,
        "longitude": -89.1914
      },
      "tags": ["restaurant", "salvadoran", "pupusas", "tipico", "budget"],
      "contactPhone": "+503 2222-0001",
      "contactEmail": "info@elcomalito.sv",
      "website": "https://elcomalito.sv",
      "avatar": "https://placehold.co/400x400/e63946/white?text=El+Comalito"
    }
  ],
  "posts": [
    {
      "id": "uuid-here",
      "profileId": "profile-uuid-here",
      "postType": 2,
      "title": "Pupusería El Comalito",
      "content": "Auténticas pupusas salvadoreñas desde 1985. Nuestra especialidad son las pupusas de chicharrón, queso con loroco, y revueltas. Ambiente familiar, precios accesibles. ¡Venga a probar las mejores pupusas del centro!",
      "location": {
        "city": "San Salvador",
        "state": "San Salvador",
        "country": "El Salvador",
        "latitude": 13.6989,
        "longitude": -89.1914
      },
      "tags": ["restaurant", "salvadoran", "pupusas", "tipico", "budget", "centro", "family-friendly"],
      "imageUrl": "https://placehold.co/800x600/e63946/white?text=Pupuseria+El+Comalito",
      "pricingInfo": {
        "amount": 3.50,
        "currency": "USD",
        "isNegotiable": false,
        "description": "Average meal price"
      },
      "businessMetadata": {
        "locationType": "RetailStore",
        "description": "Traditional Salvadoran pupusería",
        "contactPhone": "+503 2222-0001",
        "contactEmail": "info@elcomalito.sv",
        "acceptsWalkIns": true,
        "requiresAppointment": false,
        "workingHours": {
          "monday": { "isClosed": false, "openTime": "06:00", "closeTime": "21:00" },
          "tuesday": { "isClosed": false, "openTime": "06:00", "closeTime": "21:00" },
          "wednesday": { "isClosed": false, "openTime": "06:00", "closeTime": "21:00" },
          "thursday": { "isClosed": false, "openTime": "06:00", "closeTime": "21:00" },
          "friday": { "isClosed": false, "openTime": "06:00", "closeTime": "21:00" },
          "saturday": { "isClosed": false, "openTime": "06:00", "closeTime": "21:00" },
          "sunday": { "isClosed": false, "openTime": "06:00", "closeTime": "21:00" }
        },
        "specialInstructions": "Estacionamiento disponible. Servicio para llevar."
      }
    }
  ]
}
```

### JSON Schema Details

#### Profile Object
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | GUID | Yes | Unique identifier |
| `profileTypeId` | GUID | Yes | Business = `22222222-2222-2222-2222-222222222222` |
| `displayName` | string | Yes | Restaurant name |
| `handle` | string | Yes | URL-friendly slug |
| `bio` | string | Yes | Description (max 2000 chars) |
| `location` | object | Yes | City, State, Country, Lat, Long |
| `tags` | string[] | Yes | Cuisine, price tier, area |
| `contactPhone` | string | No | Phone number |
| `contactEmail` | string | No | Email address |
| `website` | string | No | Website URL |
| `avatar` | string | Yes | Placeholder image URL |

#### Post Object
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | GUID | Yes | Unique identifier |
| `profileId` | GUID | Yes | Links to profile |
| `postType` | int | Yes | 2 = BusinessLocation |
| `title` | string | Yes | Restaurant name |
| `content` | string | Yes | Full description |
| `location` | object | Yes | Same as profile |
| `tags` | string[] | Yes | Extended tags |
| `imageUrl` | string | Yes | Placeholder image |
| `pricingInfo` | object | No | Average price |
| `businessMetadata` | object | Yes | Hours, contact, etc. |

### Tag Taxonomy for Restaurants

```
Cuisine Tags:
- salvadoran, mexican, american, italian, asian, seafood, steakhouse, fast-food, cafe, vegetarian, vegan

Price Tags:
- price-budget ($)
- price-moderate ($$)
- price-upscale ($$$)
- price-fine-dining ($$$$)

Area Tags:
- centro, zona-rosa, san-benito, escalon, santa-tecla, antiguo-cuscatlan
- multiplaza, galerias, gran-via, la-libertad, santa-ana

Feature Tags:
- family-friendly, romantic, outdoor-seating, parking, delivery
- breakfast, lunch, dinner, late-night, 24-hours
- wifi, live-music, sports-bar, pet-friendly
```

### B. Entertainment & Activities (10 entries)

| Name | Type | Location | Description |
|------|------|----------|-------------|
| Teatro Nacional | Theater | Centro Histórico | Historic national theater |
| Estadio Cuscatlán | Sports Venue | San Salvador | National stadium |
| Museo MARTE | Museum | San Benito | Modern art museum |
| La Gran Vía | Shopping/Entertainment | Antiguo Cuscatlán | Mall with entertainment |
| Multiplaza | Shopping Mall | San Salvador | Major shopping center |
| Parque Cuscatlán | Park | San Salvador | Urban park |
| Jardín Botánico | Nature | Antiguo Cuscatlán | Botanical garden |
| Boliche Bol | Bowling | Multiplaza | Bowling alley |
| Cinemark | Cinema | Multiplaza | Movie theater |
| Cascadas Beer House | Bar/Live Music | Zona Rosa | Live music venue |

### C. Tourist Attractions (10 entries)

| Name | Type | Location | Description |
|------|------|----------|-------------|
| Volcán de San Salvador | Natural Landmark | San Salvador | Iconic volcano |
| Puerta del Diablo | Viewpoint | Panchimalco | Famous rock formation |
| Joya de Cerén | Archaeological | San Juan Opico | UNESCO World Heritage |
| Playa El Tunco | Beach | La Libertad | Popular surf beach |
| Playa El Sunzal | Beach | La Libertad | Surf destination |
| Lago de Coatepeque | Lake | Santa Ana | Crater lake |
| Ruta de las Flores | Tourism Route | Ahuachapán | Scenic flower route |
| Catedral Metropolitana | Religious Site | San Salvador | Main cathedral |
| Monumento al Salvador del Mundo | Landmark | San Salvador | National monument |
| Parque Nacional El Boquerón | National Park | San Salvador | Volcano crater hike |

### D. Government Offices & Procedures (10 entries)

| Name | Type | Location | Services |
|------|------|----------|----------|
| DUI Centro de Gobierno | Government | Centro de Gobierno | ID card services |
| Migración y Extranjería | Government | San Salvador | Passports, visas |
| Alcaldía de San Salvador | Government | Centro | Municipal services |
| CNR (Registro) | Government | Centro de Gobierno | Property registry |
| Ministerio de Hacienda | Government | Centro de Gobierno | Tax services |
| ISSS Hospital | Healthcare | San Salvador | Social security hospital |
| DGT (Tránsito) | Government | San Salvador | Vehicle registration, licenses |
| Corte Suprema de Justicia | Government | Centro | Legal services |
| Defensoría del Consumidor | Government | San Salvador | Consumer protection |
| Banco Central de Reserva | Financial | San Salvador | Central bank services |

### E. Services & Utilities (10 entries)

| Name | Type | Location | Services |
|------|------|----------|----------|
| Banco Agrícola Centro | Bank | Centro | Banking services |
| Banco de América Central | Bank | Escalón | Banking services |
| CAESS Pago | Utility | Multiple | Electricity payments |
| ANDA Oficina | Utility | Centro | Water services |
| Farmacia San Nicolás | Pharmacy | Multiple | 24hr pharmacy |
| Notaría García | Professional | San Salvador | Notary services |
| Claro Centro | Telecom | Centro | Phone/internet |
| Tigo Escalón | Telecom | Escalón | Phone/internet |
| Western Union Centro | Financial | Centro | Money transfers |
| Super Selectos | Supermarket | Multiple | Grocery shopping |

---

## Government Procedures Guide (Service Posts)

### 1. Getting a DUI (National ID)
**Requirements:**
- Birth certificate (certified)
- Previous DUI (if renewal)
- Proof of address
- $10 USD fee

**Process:**
1. Go to nearest DUI center
2. Take number and wait
3. Provide documents
4. Fingerprint and photo
5. Receive DUI in 7-15 days

### 2. Getting a Passport
**Requirements:**
- Original DUI
- Birth certificate
- Previous passport (if renewal)
- $25 USD fee (regular), $35 USD (express)

**Process:**
1. Schedule appointment online at migracion.gob.sv
2. Go to Migración office
3. Submit documents and payment
4. Biometric data capture
5. Pick up in 5-10 business days

### 3. Vehicle Registration
**Requirements:**
- Bill of sale
- Previous owner's tarjeta de circulación
- Solvencia de multas
- Insurance policy
- $15-50 USD depending on vehicle type

**Process:**
1. Get solvencia at DGT
2. Pay fees at bank
3. Return to DGT with receipts
4. Receive new tarjeta de circulación

### 4. Driver's License
**Requirements:**
- DUI
- Blood type test
- Vision test
- Driving test (new licenses)
- $20 USD fee

**Process:**
1. Go to DGT office
2. Take medical tests
3. Take written exam
4. Take practical exam (if new)
5. Receive license

---

## Implementation Steps

### Phase 1: Generate Restaurant JSON Data
Create `demo-data-restaurants.json` with:
- 50 Business Profiles (one per restaurant)
- 50 BusinessLocation Posts (one per restaurant)
- All with placeholder images from placehold.co

### Phase 2: Create JSON Importer Service
Create `DemoDataImporterService.cs` that:
- Reads JSON file from disk or embedded resource
- Creates Profile entities with all fields
- Creates Post entities with BusinessMetadata
- Updates PostGIS GeoLocation after insert

### Phase 3: Create Organization Profiles (5 profiles)
Create profiles with `ProfileTypeId = 33333333-3333-3333-3333-333333333333` (Organization):
- 3 Government agencies (DUI Centro, Migración, DGT)
- 2 Tourism/info boards

### Phase 4: Create BusinessLocation Posts (35 posts)
For each physical location, create a post with:
- `PostType = PostType.BusinessLocation`
- `Location` with accurate GPS coordinates
- `BusinessMetadata` JSON with:
  - `WorkingHours` (BusinessHours structure)
  - `ContactPhone`, `ContactEmail`
  - `AcceptsWalkIns`, `RequiresAppointment`
  - `SpecialInstructions`
- `Tags` array (e.g., ["restaurant", "mexican", "zona-rosa"])
- `PricingInfo` where applicable ($$, $$$, etc.)

### Phase 5: Create Service Posts (10 posts)
For government procedures, create posts with:
- `PostType = PostType.Service`
- `Title` = procedure name (e.g., "Getting a DUI (National ID)")
- `Content` = step-by-step instructions
- `BusinessMetadata` JSON with ServiceMetadata:
  - `Requirements` = documents needed
  - `DurationMinutes` = estimated time
  - `BookingInstructions` = how to proceed
- `Tags` (e.g., ["government", "dui", "id-card", "tramite"])
- `PricingInfo` with fees

### Phase 6: Create Event Posts (5 posts)
For activities and recurring events:
- `PostType = PostType.Event`
- `ExpiresAt` = event date
- `Location` with coordinates
- `Tags` for discovery

### Phase 7: Update PostGIS GeoLocation
Run raw SQL to update `geolocation` column from lat/lng:
```sql
UPDATE "Posts" 
SET "GeoLocation" = ST_SetSRID(ST_MakePoint("Location_Longitude", "Location_Latitude"), 4326)::geography
WHERE "Location_Latitude" IS NOT NULL;

UPDATE "Profiles" 
SET "GeoLocation" = ST_SetSRID(ST_MakePoint("Location_Longitude", "Location_Latitude"), 4326)::geography
WHERE "Location_Latitude" IS NOT NULL;
```

### Phase 8: Test AI Queries
Test all use cases:
- "Find restaurants near me"
- "Where can I eat pupusas in Centro?"
- "Best Italian restaurant in Zona Rosa"
- "Seafood restaurants in La Libertad"
- "Vegetarian options in San Benito"
- "What's open late night?"

---

## Files to Create/Modify

1. **demo-data-restaurants.json** - JSON data file with 50 profiles + 50 posts
2. **DemoDataImporterService.cs** - Service to import JSON data
3. **Sivar.Os.DataSeeder/Program.cs** - Add demo import option
4. **ChatFunctionService.cs** - May need to enhance for cuisine/price queries

---

## Success Criteria

1. ✅ User asks "Where can I eat pupusas?" → Returns Pupusería La Ceiba with location
2. ✅ User asks "How do I get a passport?" → Returns step-by-step procedure
3. ✅ User asks "What's near me?" → Returns nearby businesses with distances
4. ✅ User asks "Events this weekend" → Returns upcoming events
5. ✅ User asks "Where is the DGT?" → Returns address and services
