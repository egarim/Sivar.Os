# Restaurants Demo Data

## Overview

| Field | Value |
|-------|-------|
| **Total Entries** | 50 |
| **Profiles Created** | 50 Business Profiles |
| **Posts Created** | 50 BusinessLocation Posts |
| **Coverage** | San Salvador, Santa Ana, La Libertad |

## Cuisine Distribution

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

## Geographic Coverage

| Area | Department | Count |
|------|------------|-------|
| San Salvador Centro | San Salvador | 8 |
| Zona Rosa / San Benito | San Salvador | 8 |
| Colonia Escalón | San Salvador | 6 |
| Santa Tecla | La Libertad | 5 |
| Antiguo Cuscatlán | La Libertad | 5 |
| Multiplaza / Galerías | San Salvador | 5 |
| La Libertad (beaches) | La Libertad | 6 |
| Santa Ana | Santa Ana | 7 |

## Price Ranges

| Symbol | Range (USD) | Tag |
|--------|-------------|-----|
| $ | Under $5 | `price-budget` |
| $$ | $5-15 | `price-moderate` |
| $$$ | $15-30 | `price-upscale` |
| $$$$ | $30+ | `price-fine-dining` |

## Images

Using placeholder service with cuisine-colored backgrounds:

| Cuisine | Color | Hex |
|---------|-------|-----|
| Salvadoran | Red | `#e63946` |
| Mexican | Orange | `#f4a261` |
| American | Teal | `#2a9d8f` |
| Italian | Yellow | `#e9c46a` |
| Asian | Dark Red | `#9b2226` |
| Seafood | Blue | `#0077b6` |
| Steakhouse | Brown | `#6d4c41` |
| Fast Food | Coral | `#ff6b6b` |
| Café | Coffee | `#8d6e63` |
| Vegetarian | Green | `#52b788` |

Format: `https://placehold.co/800x600/{color}/white?text={Name}`

## Tag Taxonomy

### Cuisine Tags
`salvadoran`, `mexican`, `american`, `italian`, `asian`, `seafood`, `steakhouse`, `fast-food`, `cafe`, `vegetarian`, `vegan`

### Price Tags
`price-budget`, `price-moderate`, `price-upscale`, `price-fine-dining`

### Area Tags
`centro`, `zona-rosa`, `san-benito`, `escalon`, `santa-tecla`, `antiguo-cuscatlan`, `multiplaza`, `galerias`, `gran-via`, `la-libertad`, `santa-ana`

### Feature Tags
`family-friendly`, `romantic`, `outdoor-seating`, `parking`, `delivery`, `breakfast`, `lunch`, `dinner`, `late-night`, `24-hours`, `wifi`, `live-music`, `sports-bar`, `pet-friendly`

## Sample AI Queries

These queries should work after importing:

- "Find restaurants near me"
- "Where can I eat pupusas in Centro?"
- "Best Italian restaurant in Zona Rosa"
- "Seafood restaurants in La Libertad"
- "Vegetarian options in San Benito"
- "What's open late night?"
- "Cheap eats near Multiplaza"
- "Fine dining in Escalón"

## JSON Structure

See `restaurants.json` for the full data. Structure:

```json
{
  "metadata": { ... },
  "profiles": [
    {
      "id": "GUID",
      "profileTypeId": "22222222-2222-2222-2222-222222222222",
      "displayName": "Restaurant Name",
      "handle": "restaurant-name",
      "bio": "Description...",
      "location": { "city", "state", "country", "latitude", "longitude" },
      "tags": ["cuisine", "price", "area"],
      "contactPhone": "+503 XXXX-XXXX",
      "contactEmail": "email@example.com",
      "website": "https://...",
      "avatar": "https://placehold.co/..."
    }
  ],
  "posts": [
    {
      "id": "GUID",
      "profileId": "profile-GUID",
      "postType": 2,
      "title": "Restaurant Name",
      "content": "Full description...",
      "location": { ... },
      "tags": [...],
      "imageUrl": "https://placehold.co/...",
      "pricingInfo": { "amount", "currency", "description" },
      "businessMetadata": {
        "locationType": "RetailStore",
        "contactPhone": "...",
        "acceptsWalkIns": true,
        "workingHours": { "monday": {...}, ... }
      }
    }
  ]
}
```
