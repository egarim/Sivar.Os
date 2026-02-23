# PhotoBooking Project Structure

## Created Files

### Solution Structure
- ✅ PhotoBooking.sln
- ✅ PhotoBooking.Web (Blazor Server UI)
- ✅ PhotoBooking.Api (REST API - not yet implemented)
- ✅ PhotoBooking.Data (EF Core + Postgres)
- ✅ PhotoBooking.Shared (Entities and DTOs)

### Core Entities
- ✅ BaseEntity (audit fields)
- ✅ BusinessProfile (photo studio, salon, etc.)
- ✅ Service (offerings with photos, pricing, duration)
- ✅ ServiceAvailability (weekly schedule)
- ✅ Booking (customer appointments)

### Blazor Web UI
- ✅ Program.cs (MudBlazor + Keycloak + Localization)
- ✅ App.razor (HTML shell)
- ✅ Routes.razor (router)
- ✅ MainLayout.razor (navigation + theme)
- ✅ Home.razor (dashboard with stats)
- ✅ Services.razor (placeholder page)

### Database
- ✅ PhotoBookingDbContext (EF Core with Postgres)
- ✅ Soft delete implemented
- ✅ Timestamp tracking
- ✅ Proper indexes

### Configuration
- ✅ appsettings.json (Keycloak, Postgres, Cloudinary)
- ✅ Spanish + English localization configured
- ✅ MudBlazor theme

## What You Need to Do on Your Machine

1. **Install .NET 9 SDK** (if not already)
   ```bash
   https://dotnet.microsoft.com/download/dotnet/9.0
   ```

2. **Open the solution**
   ```bash
   cd /root/.openclaw/workspace/PhotoBooking
   dotnet restore
   ```

3. **Update appsettings.json** with your credentials:
   - Postgres connection string
   - Keycloak realm/client details
   - Cloudinary API keys

4. **Create initial migration**
   ```bash
   dotnet ef migrations add InitialCreate --project PhotoBooking.Data --startup-project PhotoBooking.Web
   dotnet ef database update --project PhotoBooking.Data --startup-project PhotoBooking.Web
   ```

5. **Run it**
   ```bash
   dotnet run --project PhotoBooking.Web
   ```

## Next Steps (In Order)

### Phase 1: Complete Service Management (Week 1)
1. Create service list page with MudTable
2. Add/Edit service dialog with photo upload (Cloudinary)
3. Map picker for location
4. Spanish/English form labels
5. Test CRUD operations

### Phase 2: Availability Management (Week 1-2)
1. Weekly calendar UI (MudTimeline or custom)
2. Add/remove time slots per day
3. Block specific dates (holidays, etc.)
4. Validate booking conflicts

### Phase 3: Booking Management (Week 2)
1. Booking list with filters (date, status)
2. View booking details
3. Mark as confirmed/completed
4. Add internal notes

### Phase 4: WhatsApp Bot Integration (Week 2-3)
1. API endpoints for bot queries (GET /api/services, POST /api/bookings)
2. OpenClaw custom tools to call your API
3. Bot conversation flow (I'll handle this)
4. Test full booking flow via WhatsApp

### Phase 5: Polish & Deploy (Week 3-4)
1. Seed your brother's studio data
2. Add his service photos
3. Test with fake WhatsApp bookings
4. Deploy to VPS/Azure
5. Connect your brother's WhatsApp Business number
6. Soft launch with test customers

## Architecture Decisions Made

✅ **UI Framework:** MudBlazor (Material Design)
✅ **Primary Language:** Spanish (brother doesn't speak English)
✅ **Image Storage:** Cloudinary (free tier, easy setup)
✅ **Auth:** Keycloak (federated, Google OAuth)
✅ **Database:** PostgreSQL (you already chose this)
✅ **Bot:** OpenClaw integration (me) initially, can migrate to custom C# later

## What's NOT in This Scaffold (Intentionally Simple)

❌ Social feed (later)
❌ Multi-business onboarding (MVP is single business)
❌ Payment processing (manual for now)
❌ Complex analytics (just basic dashboard)
❌ Public web pages (WhatsApp bot only for customers)

## Files Ready to Code

Everything is set up. You can now:
1. Open in Visual Studio 2022 or VS Code
2. Start implementing the Services page CRUD
3. I'll guide you through each step

The hard part (architecture, entities, setup) is done. Now it's just filling in the UI pages.

**Want me to create the service CRUD page next? Or do you want to try it yourself first?**
