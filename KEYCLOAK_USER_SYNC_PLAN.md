# Keycloak User Sync Plan

> **Goal**: Create Keycloak users for all seeded demo profiles so users can log in as any business/profile and see booking information.

---

## Overview

Currently we have:
- **116 profiles** in the database (business, personal, organization)
- **4 Keycloak users** in the setup script (roberto.guzman, jaime.macias, joche.ojeda, oscar.ojeda)
- **1 Barbershop** with 3 barbers and bookable resources

The goal is to enable:
1. Log in as a **customer** → Make bookings at any business
2. Log in as a **business owner** (e.g., Barbería El Caballero) → See incoming bookings

---

## Key Decisions

1. ✅ **Keep `User` entity as-is** - No need to rename to `SivarUser`. The entity is already in `Sivar.Os.Shared.Entities` namespace and uses `Sivar_Users` table.

2. ✅ **XAF Actions for Seeding** - Instead of external scripts or Updater.cs:
   - **Action 1**: Sync Keycloak Users (create in Keycloak, save locally)
   - **Action 2**: Seed Demo Data (create profiles linked to users)

3. ✅ **Singleton SeederLog** - XAF singleton object with large text field for logging seeder operations, accessible from navigation.

---

## XAF Implementation Architecture

### SeederLog Singleton (Business Object) ✅ IMPLEMENTED

**File**: `Xaf.Sivar.Os.Module/BusinessObjects/SeederLog.cs`

A persistent singleton object that:
- Stores a large `LogText` property (unlimited text)
- Shows timestamp of last seeding operation
- Is accessible directly from navigation as a DetailView
- Cannot be deleted (validation rules)
- Has helper methods: `AppendLog()`, `AppendException()`, `StartOperation()`, `EndOperation()`, `ClearLog()`

**Properties**:
- `LogText` - Large text field for logging (RichText editor)
- `LastOperationAt` - Timestamp of last operation
- `LastOperationSummary` - Summary text
- `KeycloakUsersSynced` - Counter
- `ProfilesSeeded` - Counter
- `ProfilesLinked` - Counter

### SeedDataController ✅ IMPLEMENTED

**File**: `Xaf.Sivar.Os.Module/Controllers/SeedDataController.cs`

An `ObjectViewController<DetailView, SeederLog>` that provides actions when viewing the SeederLog:
1. **Sync Keycloak Users** - Creates users in Keycloak via Admin API (TODO: implement Keycloak API)
2. **Seed Demo Profiles** - Creates profiles from DemoData JSON (TODO: implement)
3. **Link Profiles to Users** ✅ - Updates Profile.UserId based on handle↔email matching
4. **Clear Log** ✅ - Clears the log text and resets counters

### SeederLogNavigationController ✅ IMPLEMENTED

**File**: `Xaf.Sivar.Os.Module/Controllers/SeederLogNavigationController.cs`

Handles singleton navigation - ensures clicking "SeederLog" nav item loads the singleton DetailView properly.

### Database Updates ✅ IMPLEMENTED

- **OsDbContext.cs**: Added `DbSet<SeederLog>` and table mapping to `Xaf_SeederLogs`
- **Module.cs**: Registered `SeederLog` in `AdditionalExportedTypes`
- **Updater.cs**: Seeds singleton instance on database update

---

## Navigation Setup (Manual Step Required)

To add the SeederLog to navigation, use the **Model Editor**:

1. Open `Model.DesignedDiffs.xafml` in the XAF Blazor Server project
2. Navigate to: **NavigationItems** → **Items** → **System** → **Items**
3. Right-click → **Add** → **NavigationItem**
4. Set properties:
   - **Id**: `SeederLog`
   - **Caption**: `Seeder Log`
   - **View**: `SeederLog_DetailView`
   - **ImageName**: `Action_Log` (or similar)

---

## User Naming Convention

| Field | Format | Example |
|-------|--------|---------|
| **Username** | `{handle}` | `barberia-el-caballero` |
| **Email** | `{handle}@sivar.lat` | `barberia-el-caballero@sivar.lat` |
| **Password** | `SivarOs123!` (same for all) | |
| **First Name** | Extracted from DisplayName | `Barbería` |
| **Last Name** | Extracted from DisplayName | `El Caballero` |

---

## Profile Categories & Counts

| Category | Profiles | Has Bookable Resources? |
|----------|----------|------------------------|
| **Restaurants** | 50 | 🔜 Future (table reservations) |
| **Services** | 20 | ✅ Yes (barbershop has 3 barbers) |
| **Entertainment** | 15 | 🔜 Future (event tickets) |
| **Government** | 15 | ❌ No |
| **Tourism** | 10 | 🔜 Future (tours) |
| **Personal** | 6 | ❌ No |
| **TOTAL** | ~116 | |

---

## Implementation Plan

### Phase 1: Extend Keycloak Setup Script ✅ Priority High

**File**: `Keycloak/Setup-SivarOsKeycloak.ps1`

1. Add function `New-ProfileUsers` that:
   - Reads all profile handles from the database OR from JSON files
   - Creates Keycloak users for each profile
   - Uses consistent naming: `{handle}@sivar.lat`
   - Assigns appropriate roles (user, business-owner)

2. Add parameter `-SeedAllProfiles` to script

3. User list to create:

```powershell
# Priority 1: Booking-Enabled Businesses (test booking flow)
@(
    "barberia-el-caballero"  # Has 3 barbers with bookable slots
)

# Priority 2: Restaurants (50 profiles)
@(
    "pupuseria-el-comalito"
    "tipicos-dona-maria"
    "la-casita-del-maiz"
    "pupuseria-la-bendicion"
    "restaurante-el-volcan"
    "tacos-el-charro"
    "la-cantina-mexicana"
    "burritos-y-mas"
    "el-mariachi-loco"
    "taqueria-don-pancho"
    "the-burger-joint"
    "wings-and-things"
    "bbq-smokehouse"
    "diner-americano"
    "steak-and-shake"
    "trattoria-bella-italia"
    "pizzeria-napoli"
    "pasta-fresca"
    "il-forno"
    "ristorante-milano"
    "sushi-house"
    "dragon-palace"
    "thai-garden"
    "noodle-bar"
    "ramen-house"
    "mariscos-el-puerto"
    "cevicheria-la-ola"
    "el-pescador"
    "marisqueria-costanera"
    "lobster-house"
    "la-parrilla-argentina"
    "el-gaucho"
    "rancho-grande"
    "carnes-y-brasas"
    "asador-el-toro"
    "pollo-campestre-centro"
    "burger-palace"
    "pizza-rapida"
    "hot-dogs-express"
    "tacos-express"
    "cafe-del-centro"
    "la-panaderia-francesa"
    "coffee-and-art"
    "dulce-tentacion"
    "cafe-vista-al-mar"
    "green-garden"
    "vida-natural"
    "el-jardin-vegano"
    "roots-and-leaves"
    "semillas-cafe"
)

# Priority 3: Services (20 profiles - includes barbershop)
@(
    "farmaciasannicolas"
    "farmaciaseconomicascentro"
    "labmaxbloch"
    "hospitaldiagnostico"
    "opticascuracao"
    "bufetemrtinez"
    "notariagarcia"
    "cpaghernandez"
    "segurospacifico"
    "bancoagricolacentro"
    "daviviendaescalon"
    "westernunioncentro"
    "clarocentro"
    "tigoescalon"
    "freundcentro"
    "shellsantaelena"
    "pumascalon"
    "superselectosescalon"
    "walmartesoyapango"
)

# Priority 4: Entertainment (15 profiles)
@(
    "cinemark-metrocentro"
    "cinemark-multiplaza"
    "cinepolis-gran-via"
    "cascadas-beer-house"
    "la-luna-casa-arte"
    "teatro-nacional"
    "teatro-presidente"
    "estadio-cuscatlan"
    "estadio-magico-gonzalez"
    "museo-marte"
    "museo-arte-popular"
    "museo-nacional-antropologia"
    "centro-cultural-espana"
    "jardin-botanico"
    "parque-cuscatlan"
)

# Priority 5: Government (15 profiles)
@(
    "alcaldia-san-salvador"
    "dui-centro-gobierno"
    "registro-civil-ss"
    "cnr-registros"
    "migracion-extranjeria"
    "dgt-transito"
    "pnc-policia"
    "isss-seguro-social"
    "defensoria-consumidor"
    "ministerio-hacienda"
)

# Priority 6: Tourism (10 profiles)
@(
    "volcan-san-salvador"
    "parque-el-boqueron"
    "puerta-del-diablo"
    "parque-bicentenario"
    "joya-de-ceren"
    "ruinas-tazumal"
    "ruta-de-las-flores"
    "lago-coatepeque"
    "playa-el-tunco"
    "playa-el-sunzal"
    "playa-el-zonte"
)

# Priority 7: Shopping Malls
@(
    "metrocentro-san-salvador"
    "multiplaza-san-salvador"
    "galerias-escalon"
    "la-gran-via"
)

# Existing Personal Users (already in script)
@(
    "roberto-guzman"
    "jaime-macias"
    "joche-ojeda"
    "oscar-ojeda"
)
```

### Phase 2: Update Updater.cs to Link Users to Profiles

**File**: `Xaf.Sivar.Os/Xaf.Sivar.Os.Module/DatabaseUpdate/Updater.cs`

1. When seeding profiles, ensure `UserId` is set:
   - Look up User by `KeycloakId` matching the profile handle pattern
   - If no user exists, skip linking (user will be created when they first log in)

2. Add `KeycloakId` to User entities using handle-based pattern:
   ```csharp
   // Pattern: {handle} as KeycloakId
   user.KeycloakId = profileHandle;
   ```

### Phase 3: Sync Script (Python or PowerShell)

Create a script that:
1. Queries the database for all profiles
2. Calls Keycloak Admin API to create matching users
3. Maps users to profiles by updating `UserId` foreign key

---

## Test Scenarios

### Scenario 1: Customer Books at Barbershop
1. Log in as `joche.ojeda@sivar.lat` (customer)
2. Chat: "Busco una barbería para cortarme el pelo"
3. Book with Carlos for tomorrow 10:00
4. Verify booking in database

### Scenario 2: Business Owner Views Bookings
1. Log in as `barberia-el-caballero@sivar.lat` (business owner)
2. Navigate to business dashboard
3. See the booking made by joche.ojeda
4. Confirm/modify/cancel the booking

### Scenario 3: Barber Views Their Schedule
1. Log in as `barberia-el-caballero@sivar.lat` (or create separate barber users)
2. View schedule for Carlos
3. See all bookings for the day

---

## Database Changes Required

### Option A: Link via Profile Handle → User KeycloakId
```sql
-- Each profile's handle becomes a user's KeycloakId
-- User login: {handle}@sivar.lat
-- Profile.UserId → User.Id (linked by matching KeycloakId pattern)
```

### Option B: Generate Stable GUIDs per Profile
```csharp
// Generate deterministic GUID from profile handle
var keycloakId = GenerateStableGuid($"{handle}@sivar.lat");
```

---

## Files to Modify

| File | Changes |
|------|---------|
| `Keycloak/Setup-SivarOsKeycloak.ps1` | Add `New-ProfileUsers` function with all 116 profiles |
| `Xaf.Sivar.Os.Module/DatabaseUpdate/Updater.cs` | Ensure User↔Profile linking during seeding |
| `DemoData/*.json` (optional) | Add `keycloakId` field to each profile |

---

## Password Policy

All demo users will have:
- **Password**: `SivarOs123!`
- **Email Verified**: `true`
- **Enabled**: `true`
- **Temporary Password**: `false`

---

## Roles Mapping

| Profile Type | Keycloak Role | Capabilities |
|--------------|---------------|--------------|
| Personal | `user` | Book appointments, make posts |
| Business | `user`, `business-owner` | Manage bookings, view dashboard |
| Organization | `user`, `organization-admin` | Manage org content |

---

## Implementation Checklist

### XAF Admin Interface ✅ DONE
- [x] **SeederLog.cs** - Singleton business object with log text and counters
- [x] **SeedDataController.cs** - Actions for seeding operations
- [x] **SeederLogNavigationController.cs** - Navigation handler for singleton
- [x] **OsDbContext.cs** - DbSet and table mapping (`Xaf_SeederLogs`)
- [x] **Module.cs** - Registered in `AdditionalExportedTypes`
- [x] **Updater.cs** - Seeds singleton on database update

### XAF Model Editor (Manual) ⏳ TODO
- [ ] Add navigation item for SeederLog (Id: `SeederLog`, View: `SeederLog_DetailView`)

### Seeding Actions Implementation ⏳ TODO
- [ ] **Sync Keycloak Users** - Implement Keycloak Admin API integration
- [ ] **Seed Demo Profiles** - Implement JSON file reading and profile creation
- [x] **Link Profiles to Users** - Implemented (matches handle to email pattern)
- [x] **Clear Log** - Implemented

### Testing ⏳ TODO
- [ ] **Phase 4**: Test booking flow as customer
- [ ] **Phase 5**: Test dashboard as business owner

---

## Quick Start After Implementation

```powershell
# 1. Run XAF Admin to access SeederLog
dotnet run --project Xaf.Sivar.Os/Xaf.Sivar.Os.Blazor.Server

# 2. Navigate to System → Seeder Log
# 3. Use actions:
#    - "Sync Keycloak Users" to create users in Keycloak
#    - "Seed Demo Profiles" to create profiles
#    - "Link Profiles to Users" to connect profiles to their users

# 4. Test login
# Customer: joche.ojeda@sivar.lat / SivarOs123!
# Business: barberia-el-caballero@sivar.lat / SivarOs123!
```

---

## Notes

1. **Keycloak User IDs**: When users are created in Keycloak, they get a random UUID. We need to capture this and store it in the User.KeycloakId field in the database.

2. **First Login Sync**: Currently, when a Keycloak user logs in for the first time, `GetOrCreateUserFromKeycloakAsync` creates a User record. We should update this to also link to an existing Profile if the handle matches.

3. **Profile Ownership**: Each Profile has a `UserId` field that indicates ownership. This is what determines if a logged-in user can manage the profile's bookings.

4. **SeederLog Singleton**: The SeederLog is a singleton (only one instance). It's created automatically during database update and provides a visual log of all seeding operations.
