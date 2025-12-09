# Sivar.Os Demo Data

This folder contains demo data for the AI-powered local directory assistant.

## Structure

```
DemoData/
├── README.md                 (this file)
├── Restaurants/              (50 entries)
│   ├── README.md
│   └── restaurants.json
├── Entertainment/            (planned)
│   ├── README.md
│   └── entertainment.json
├── Tourism/                  (planned)
│   ├── README.md
│   └── tourism.json
├── Government/               (planned)
│   ├── README.md
│   └── government.json
└── Services/                 (planned)
    ├── README.md
    └── services.json
```

## Categories

| Category | Entries | Profiles | Posts | Status |
|----------|---------|----------|-------|--------|
| Restaurants | 50 | 50 Business | 50 BusinessLocation | 🔄 In Progress |
| Entertainment | TBD | TBD | TBD | ⏳ Planned |
| Tourism | TBD | TBD | TBD | ⏳ Planned |
| Government | TBD | TBD | TBD | ⏳ Planned |
| Services | TBD | TBD | TBD | ⏳ Planned |

## Import Instructions

To import demo data into the database:

```bash
cd Sivar.Os.DataSeeder
dotnet run -- --demo-data
```

Or import a specific category:

```bash
dotnet run -- --demo-data --category restaurants
```

## JSON Schema

Each category follows the same JSON structure:

```json
{
  "metadata": {
    "version": "1.0",
    "generatedAt": "2024-12-09",
    "category": "restaurants",
    "totalProfiles": 50,
    "totalPosts": 50
  },
  "profiles": [...],
  "posts": [...]
}
```

## Profile Types Used

| Type | GUID | Used For |
|------|------|----------|
| Business | `22222222-2222-2222-2222-222222222222` | Restaurants, shops, services |
| Organization | `33333333-3333-3333-3333-333333333333` | Government, NGOs, tourism boards |

## Post Types Used

| Type | Value | Used For |
|------|-------|----------|
| BusinessLocation | 2 | Physical locations |
| Service | 4 | Procedures, services offered |
| Event | 5 | Activities, happenings |
