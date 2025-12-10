# Entertainment Demo Data

## Overview

| Field | Value |
|-------|-------|
| **Status** | ✅ Complete |
| **Total Entries** | 20 profiles, 40 posts |
| **Profiles Created** | 20 |
| **Posts Created** | 20 BusinessLocation + 20 Events = 40 |
| **Profile Type** | Business (22222222-2222-2222-2222-222222222222) |
| **Post Types** | BusinessLocation (2), Event (5) |

## Categories Distribution (20 venues)

| Category | Count | Examples |
|----------|-------|----------|
| Shopping Malls | 4 | Metrocentro, Multiplaza, La Gran Vía, Plaza Mundo |
| Museums & Galleries | 4 | MARTE, MUNA, Centro Histórico, Joya de Cerén |
| Theaters & Cultural | 3 | Teatro Nacional, Centro Cultural de España, Ballet Nacional |
| Cinemas | 3 | Cinemark Multiplaza, Cinépolis Gran Vía, Cinemark Metrocentro |
| Parks & Nature | 3 | Parque Cuscatlán, Jardín Botánico, Parque Bicentenario |
| Bars & Nightlife | 2 | Cascadas Beer House, La Luna Casa y Arte |
| Sports Venues | 2 | Estadio Cuscatlán, Estadio Mágico González |
| **TOTAL** | **20** | |

## Posts Breakdown

| Type | ID Range | Count | Description |
|------|----------|-------|-------------|
| BusinessLocation | d0000001-...-01 to 20 | 20 | Venue profiles with hours, location, pricing |
| Event | d0000001-...-21 to 40 | 20 | Events at each venue (one per venue) |
| **TOTAL** | | **40** | |

## Geographic Coverage

| Area | Count |
|------|-------|
| San Salvador Centro | 6 |
| Zona Rosa / San Benito | 3 |
| Antiguo Cuscatlán | 5 |
| Santa Tecla | 2 |
| Soyapango | 2 |
| Ilopango | 2 |
| **TOTAL** | **20** |

## Sample AI Queries

- "What events are happening this weekend?"
- "Find live music venues in San Salvador"
- "Where can I go bowling?"
- "Family activities for kids"
- "Best bars in Zona Rosa"
- "Museums near me"
- "Shopping malls open late"
- "Parks for jogging"

## Tag Taxonomy

```
Venue Type Tags:
- mall, museum, theater, cinema, park, bar, nightclub, sports-venue, cultural

Activity Tags:
- shopping, art, culture, movies, nature, nightlife, sports, family-friendly, live-music

Area Tags:
- centro, zona-rosa, san-benito, antiguo-cuscatlan, santa-tecla, santa-ana, la-libertad

Feature Tags:
- parking, wifi, outdoor, indoor, food-court, vip-area, wheelchair-accessible
```

## JSON File

`entertainment.json` - 20 profiles + 20 BusinessLocation posts
