# Booking UI Implementation Plan

> **Created**: December 22, 2025  
> **Branch**: `BookingUI`  
> **Status**: 📋 Planning  
> **Priority**: High - Core feature for business profiles

---

## Table of Contents

1. [Overview](#overview)
2. [MVP vs Full Implementation](#mvp-vs-full-implementation)
3. [User Perspectives](#user-perspectives)
4. [Data Model Gap Analysis](#data-model-gap-analysis)
5. [Existing Components Audit](#existing-components-audit)
6. [MVP Implementation](#mvp-implementation)
7. [Full Implementation](#full-implementation)
8. [Component Wireframes](#component-wireframes)
9. [Checklist](#checklist)

---

## Overview

The booking system needs UI for three distinct user perspectives:

| Perspective | User Type | Primary Actions |
|-------------|-----------|-----------------|
| **Customer** | Any profile | View my bookings, cancel, reschedule, review |
| **Business Owner** | Business/Organization profile | Manage resources, view all bookings, approve/reject, analytics |
| **Staff/Resource** | Individual (Barber, Doctor, etc.) | View MY schedule, mark complete, see today's appointments |

---

## MVP vs Full Implementation

### 🎯 MVP Goal
> **Get all 3 perspectives working with minimal new code**

| Perspective | MVP Scope | What Exists | What's Needed |
|-------------|-----------|-------------|---------------|
| **Customer** | View & cancel bookings | ✅ `MyBookings.razor` | Nothing (already works) |
| **Business** | View all bookings, manage resources | ✅ `BusinessBookingDashboard.razor`, `ResourceManager.razor` | Nothing (already works) |
| **Staff** | View MY schedule | ✅ `StaffSchedule.razor` | ~~`AssignedProfileId` + `StaffSchedule.razor`~~ DONE! |

### MVP Components (3 items only!)

| # | Item | Type | Effort | Status |
|---|------|------|--------|--------|
| 1 | Add `AssignedProfileId` to `BookableResource` | Entity | 🟢 Low | ✅ **DONE** |
| 2 | `StaffSchedule.razor` | Component | 🟡 Medium | ✅ **DONE** |
| 3 | Staff navigation detection | Logic | 🟢 Low | ✅ **DONE** |

**MVP Status**: ✅ **COMPLETE!**

**What was built**:
- Added `AssignedProfileId` to `BookableResource` entity (links staff to their login)
- Added `AssignedProfile` navigation property with EF Core configuration
- Added repository method `GetResourcesByAssignedProfileIdAsync`
- Added repository method `GetBookingsForStaffAsync`
- Added service methods `GetMyAssignedResourcesAsync` and `GetStaffScheduleAsync`
- Added API endpoints `GET /api/resourcebookings/staff/my-resources` and `GET /api/resourcebookings/staff/schedule`
- Created `StaffSchedule.razor` component with:
  - Date navigation (prev/next day, date picker)
  - Summary cards (total, pending, confirmed)
  - Timeline view of appointments with customer info
  - Quick actions (Confirm, Check In, Complete, No Show)
  - Localization (English + Spanish)
- Created `MySchedule.razor` page at route `/my-schedule`
- Added "My Schedule" navigation link to NavMenu

### Full Implementation (After MVP)

| Phase | Components | Priority |
|-------|------------|----------|
| **Phase 2**: Customer Polish | BookingDetailsDialog, RescheduleDialog, ReviewDialog | Medium |
| **Phase 3**: Business Polish | ServiceEditor, AvailabilityEditor, ExceptionEditor, StaffAssignment UI | Medium |
| **Phase 4**: Advanced | CalendarView, Analytics | Low |

---

## User Perspectives

### 1. Customer Perspective ✅ ALREADY WORKS

**Existing Components:**
- ✅ `MyBookings.razor` - Shows upcoming & history with tabs
- ✅ `BookingCard.razor` - Display card with cancel button

**MVP Status**: ✅ Complete - No work needed

**Future Enhancements** (Phase 2):
- BookingDetailsDialog (full info, directions)
- RescheduleBookingDialog (pick new time)
- SubmitReviewDialog (leave review)

---

### 2. Business Owner Perspective ✅ ALREADY WORKS

**Existing Components:**
- ✅ `BusinessBookingDashboard.razor` - Stats, today's schedule, pending approvals
- ✅ `ResourceManager.razor` - CRUD for resources (barbers, tables, etc.)
- ✅ `ResourceCard.razor` - Display card for a resource

**MVP Status**: ✅ Complete - No work needed

**Future Enhancements** (Phase 3):
- ServiceEditor (manage services per resource)
- AvailabilityEditor (weekly schedule)
- ExceptionEditor (holidays, blocks)
- ResourceStaffAssignment UI (link to profile)
- CalendarView (week/month view)
- Analytics dashboard

---

### 3. Staff/Resource Perspective ❌ NEEDS MVP WORK

**Gap:**
> ⚠️ `BookableResource` has no `AssignedProfileId` to link staff to their login.

**MVP Deliverables:**
1. Add `AssignedProfileId` to entity
2. Create `StaffSchedule.razor` component
3. Add navigation detection for staff

**Future Enhancements** (Phase 3):
- StaffDashboard (overview with stats)
- AppointmentActionBar (check-in, complete, no-show buttons)
- Quick block functionality

---

## Data Model Gap Analysis

### Current State

```
Profile (Business) ──owns──> BookableResource ──has──> ResourceBooking
                                                         │
                                                         └── CustomerProfileId
```

### Needed State

```
Profile (Business) ──owns──> BookableResource ──has──> ResourceBooking
                                  │                      │
                                  └── AssignedProfileId  └── CustomerProfileId
                                        (Staff login)
```

### Required Entity Changes

**File**: `Sivar.Os.Shared/Entities/BookableResource.cs`

```csharp
/// <summary>
/// Optional: The staff member's profile who operates this resource.
/// For Person-type resources (barbers, doctors), this links to their login profile.
/// Allows staff to see their own schedule by logging into the platform.
/// </summary>
public virtual Guid? AssignedProfileId { get; set; }
public virtual Profile? AssignedProfile { get; set; }
```

**Migration Required**: Add `AssignedProfileId` column to `BookableResources` table.

---

## Existing Components Audit

| Component | Lines | Status | Perspective |
|-----------|-------|--------|-------------|
| `MyBookings.razor` | 340 | ✅ Working | Customer |
| `BookingCard.razor` | ~200 | ✅ Working | Customer |
| `BookingWidget.razor` | ~150 | ✅ Working | Customer |
| `BusinessBookingDashboard.razor` | 589 | ✅ Working | Business |
| `ResourceManager.razor` | 546 | ✅ Working | Business |
| `ResourceCard.razor` | ~100 | ✅ Working | Business |
| `TimeSlotPicker.razor` | ~200 | ✅ Working | Shared |

**Summary**: Customer & Business perspectives are complete. Only Staff perspective needs work.

---

## MVP Implementation

### 🎯 MVP Goal: Staff can see their schedule (2-4 hours)

---

### MVP Step 1: Add `AssignedProfileId` to Entity

**File**: `Sivar.Os.Shared/Entities/BookableResource.cs`

```csharp
/// <summary>
/// Optional: The staff member's profile who operates this resource.
/// For Person-type resources (barbers, doctors), this links to their login profile.
/// Allows staff to see their own schedule by logging into the platform.
/// </summary>
public virtual Guid? AssignedProfileId { get; set; }
public virtual Profile? AssignedProfile { get; set; }
```

**Also update:**
- `BookableResourceDto` - add `AssignedProfileId`, `AssignedProfileName`
- `CreateBookableResourceDto` - add optional `AssignedProfileId`
- `UpdateBookableResourceDto` - add optional `AssignedProfileId`
- Repository query to include `AssignedProfile`

**Migration**: Add column to `BookableResources` table

---

### MVP Step 2: Create `StaffSchedule.razor`

**Purpose**: Staff member sees their appointments for today/tomorrow

**Wireframe**:
```
┌──────────────────────────────────────────────────────────────┐
│  📅 My Schedule                              [Today ▼]       │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  Monday, December 23, 2025                    4 appointments  │
│  ─────────────────────────────────────────────────────────── │
│                                                               │
│  9:00 AM   ┌──────────────────────────────────────────────┐  │
│            │ 🟢 Juan Pérez                                │  │
│            │ Haircut • 30 min • $15.00                    │  │
│            │ Confirmed                                     │  │
│            └──────────────────────────────────────────────┘  │
│                                                               │
│  9:30 AM   ┌──────────────────────────────────────────────┐  │
│            │ 🟡 Maria López                               │  │
│            │ Beard Trim • 15 min • $10.00                 │  │
│            │ Pending confirmation                          │  │
│            └──────────────────────────────────────────────┘  │
│                                                               │
│  10:00 AM  ░░░░░░░░░░░░ Available ░░░░░░░░░░░░░░░░░░░░░░░░  │
│                                                               │
│  10:30 AM  ┌──────────────────────────────────────────────┐  │
│            │ 🔵 Roberto Gómez                             │  │
│            │ Full Package • 45 min • $30.00               │  │
│            │ Checked In                                    │  │
│            └──────────────────────────────────────────────┘  │
│                                                               │
│  Summary: $55.00 estimated today                              │
└──────────────────────────────────────────────────────────────┘
```

**Features (MVP)**:
- Date selector (today/tomorrow/pick date)
- List of appointments sorted by time
- Status color coding (🟢 Confirmed, 🟡 Pending, 🔵 Checked In)
- Basic info: customer name, service, duration, price

**NOT in MVP** (Phase 3):
- Check-in/Complete/No-show buttons
- Quick block functionality
- Stats/earnings

---

### MVP Step 3: Add API Endpoints

**New endpoints needed:**

```csharp
// Get resources where I am assigned as staff
GET /api/bookings/my-assigned-resources
Returns: List<BookableResourceSummaryDto>

// Get bookings for a resource I'm assigned to
GET /api/bookings/resources/{resourceId}/schedule?date=2025-12-23
Returns: List<ResourceBookingDto>
```

---

### MVP Step 4: Navigation Detection

**Logic**: When user logs in, check if they're assigned to any resources.
If yes, show "My Schedule" in navigation.

**In NavMenu.razor or similar:**
```razor
@if (IsStaffMember)
{
    <MudNavLink Href="/staff/schedule" Icon="@Icons.Material.Filled.CalendarToday">
        My Schedule
    </MudNavLink>
}
```

---

## Full Implementation

### Phase 2: Customer Experience Polish

| Component | Description | Effort |
|-----------|-------------|--------|
| `BookingDetailsDialog.razor` | Full booking info, contact, directions, confirmation code | Medium |
| `RescheduleBookingDialog.razor` | Pick new date/time, uses TimeSlotPicker | High |
| `SubmitReviewDialog.razor` | Star rating + text review after completion | Low |

---

### Phase 3: Business & Staff Polish

| Component | Description | Effort |
|-----------|-------------|--------|
| `ServiceEditor.razor` | CRUD services for a resource | Medium |
| `AvailabilityEditor.razor` | Weekly schedule editor | High |
| `ExceptionEditor.razor` | Block dates, holidays | Medium |
| `ResourceStaffAssignment.razor` | Search & assign profile to resource | Low |
| `StaffDashboard.razor` | Stats, quick actions for staff | Medium |
| `AppointmentActionBar.razor` | Check-in, Complete, No-show buttons | Low |

---

### Phase 4: Advanced Features

| Component | Description | Effort |
|-----------|-------------|--------|
| `BookingCalendarView.razor` | Week/month calendar for business | High |
| `BookingAnalytics.razor` | Charts, revenue, trends | High |

---

## Component Wireframes

### MVP: StaffSchedule.razor (see above in MVP Step 2)

### Phase 2: BookingDetailsDialog.razor

```
┌──────────────────────────────────────────────────────────────┐
│  ✕                    Booking Details                         │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────┐  Haircut with Carlos                            │
│  │  📷     │  ⭐ 4.8 (124 reviews)                            │
│  │ Avatar  │                                                  │
│  └─────────┘  📍 Barbería El Jefe                            │
│               📞 +503 7890-1234                               │
│                                                               │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  📅 Date & Time                                               │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  Monday, December 23, 2025                                ││
│  │  10:00 AM - 10:30 AM (30 min)                            ││
│  │  🌐 America/El_Salvador                                   ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
│  📍 Location                                                  │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  Calle Principal #123, San Salvador       [Get Directions]││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
│  💰 Price                                                     │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  $15.00 USD                    [Paid ✓] / [Pay Now]      ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
│  📝 Notes                                                     │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  "Please use scissors, not clippers"                      ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
│  Confirmation Code: AXBQ-4K7M                                 │
│                                                               │
├──────────────────────────────────────────────────────────────┤
│  [Cancel Booking]              [Reschedule]    [Close]       │
└──────────────────────────────────────────────────────────────┘
```

---

### Phase 2: RescheduleBookingDialog.razor

```
┌──────────────────────────────────────────────────────────────┐
│  ←                  Reschedule Booking                        │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  Current Appointment:                                         │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  📅 Mon, Dec 23 at 10:00 AM                              ││
│  │  ✂️ Haircut with Carlos                                  ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
│  Select New Date:                                             │
│  ┌──────────────────────────────────────────────────────────┐│
│  │      December 2025                                        ││
│  │  Su  Mo  Tu  We  Th  Fr  Sa                              ││
│  │  22  [23] 24  25  26  27  28                             ││
│  │  29  30  31   1   2   3   4                              ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
│  Available Times for Dec 24:                                  │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  [9:00]  [9:30]  [10:00]  [10:30]                        ││
│  │  [11:00] [11:30] [14:00]  [14:30]                        ││
│  │  [15:00] [15:30] [16:00]  [16:30]                        ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
├──────────────────────────────────────────────────────────────┤
│                                      [Cancel]  [Confirm]     │
└──────────────────────────────────────────────────────────────┘
```

---

### Phase 3: StaffDashboard.razor (Full Version)

```
┌──────────────────────────────────────────────────────────────┐
│  👤 Welcome, Carlos!                          Barbería El Jefe│
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────┐ │
│  │   Today     │ │   Pending   │ │  Completed  │ │  Earned │ │
│  │     4       │ │      1      │ │     12      │ │  $180   │ │
│  │ appointments│ │  to confirm │ │  this week  │ │ this wk │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────┘ │
│                                                               │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  📅 Today's Schedule                    [View Full Day]  ││
│  ├──────────────────────────────────────────────────────────┤│
│  │  9:00  Juan Pérez         Haircut        🟢 Confirmed   ││
│  │  9:30  Maria López        Beard Trim     🟡 Pending     ││
│  │  10:30 Roberto Gómez      Full Package   🟢 Confirmed   ││
│  │  14:00 Pedro Martínez     Haircut        🟢 Confirmed   ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  🚫 Quick Block                                          ││
│  │  [Block Next Hour]  [Block Rest of Day]  [Custom Block]  ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

---

### Phase 4: BookingCalendarView.razor

```
┌──────────────────────────────────────────────────────────────┐
│  ← December 2025 →                 [Day] [Week] [Month]      │
├──────────────────────────────────────────────────────────────┤
│  Filter: [All Resources ▼]  [All Staff ▼]  [All Status ▼]   │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│           Mon 23      Tue 24      Wed 25      Thu 26        │
│  ─────────────────────────────────────────────────────────── │
│  Carlos   ████░░░░    ██░░░░░░    ░░░░░░░░    ████████      │
│  Miguel   ░░████░░    ████████    ██████░░    ░░░░░░░░      │
│  Table 1  ██░░██░░    ░░░░██░░    ██░░░░░░    ░░██████      │
│  Table 2  ░░░░░░░░    ██████░░    ░░████░░    ██░░░░░░      │
│                                                               │
│  Legend: █ Booked  ░ Available  ▒ Blocked                    │
│                                                               │
├──────────────────────────────────────────────────────────────┤
│  Quick Actions:                                               │
│  [+ New Booking]  [Block Time]  [Add Holiday]  [Export]      │
└──────────────────────────────────────────────────────────────┘
```

---

### Phase 3: AvailabilityEditor.razor

```
┌──────────────────────────────────────────────────────────────┐
│  Weekly Availability - Carlos (Senior Barber)                │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  ☑ Monday                                                     │
│    Start: [09:00 ▼]    End: [18:00 ▼]    Break: [12:00-13:00]│
│                                                               │
│  ☑ Tuesday                                                    │
│    Start: [09:00 ▼]    End: [18:00 ▼]    Break: [12:00-13:00]│
│                                                               │
│  ☑ Wednesday                                                  │
│    Start: [09:00 ▼]    End: [18:00 ▼]    Break: [12:00-13:00]│
│                                                               │
│  ☑ Thursday                                                   │
│    Start: [09:00 ▼]    End: [18:00 ▼]    Break: [12:00-13:00]│
│                                                               │
│  ☑ Friday                                                     │
│    Start: [09:00 ▼]    End: [17:00 ▼]    Break: [12:00-13:00]│
│                                                               │
│  ☑ Saturday                                                   │
│    Start: [08:00 ▼]    End: [14:00 ▼]    Break: [None      ]│
│                                                               │
│  ☐ Sunday (Closed)                                            │
│                                                               │
├──────────────────────────────────────────────────────────────┤
│  [Copy to All Resources]              [Cancel]  [Save]       │
└──────────────────────────────────────────────────────────────┘
```

---

### Phase 3: ServiceEditor.razor

```
┌──────────────────────────────────────────────────────────────┐
│  Services - Carlos (Senior Barber)              [+ Add]      │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  ✂️ Haircut                                       [Edit] ││
│  │  30 min • $15.00 • ✅ Active                      [Del]  ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  🧔 Beard Trim                                    [Edit] ││
│  │  15 min • $10.00 • ✅ Active                      [Del]  ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  💈 Full Package (Haircut + Beard)                [Edit] ││
│  │  45 min • $25.00 • ✅ Active                      [Del]  ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  🪒 Hot Towel Shave                               [Edit] ││
│  │  20 min • $12.00 • ⛔ Inactive                    [Del]  ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

---

### Phase 3: ResourceStaffAssignment.razor

```
┌──────────────────────────────────────────────────────────────┐
│  Assign Staff - Carlos (Senior Barber)                       │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  This resource is a Person (Barber). You can link it to a    │
│  staff member's profile so they can log in and view their    │
│  own schedule.                                                │
│                                                               │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  🔍 Search profiles...                                   ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
│  Matching Profiles:                                           │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  ○ 👤 Carlos Martínez (@carlos_barbero)                  ││
│  │  ○ 👤 Carlos López (@carloslopez)                        ││
│  │  ● 👤 Carlos Henríquez (@carlos_h) ← Currently assigned  ││
│  └──────────────────────────────────────────────────────────┘│
│                                                               │
│  OR invite new staff member:                                  │
│  [Invite by Email]  [Generate Invite Link]                   │
│                                                               │
├──────────────────────────────────────────────────────────────┤
│                                      [Cancel]  [Assign]      │
└──────────────────────────────────────────────────────────────┘
```

---

---

## Checklist

### 🎯 MVP (Do First - 2-4 hours)

- [ ] **Step 1: Entity Update**
  - [ ] Add `AssignedProfileId` to `BookableResource.cs`
  - [ ] Add `AssignedProfile` navigation property
  - [ ] Update `BookableResourceDto` with `AssignedProfileId`, `AssignedProfileName`
  - [ ] Update `CreateBookableResourceDto` and `UpdateBookableResourceDto`
  - [ ] Update repository to include `AssignedProfile`
  - [ ] Create EF migration (or SQL script for XAF)
  
- [ ] **Step 2: API Endpoints**
  - [ ] Add `GET /api/bookings/my-assigned-resources`
  - [ ] Add `GET /api/bookings/resources/{id}/schedule`
  - [ ] Update `IResourceBookingsClient` interface
  - [ ] Implement client methods
  
- [ ] **Step 3: StaffSchedule Component**
  - [ ] Create `StaffSchedule.razor`
  - [ ] Date selector (today/tomorrow/calendar)
  - [ ] Appointment list with status colors
  - [ ] Add localization resources
  
- [ ] **Step 4: Navigation**
  - [ ] Add staff detection logic
  - [ ] Add "My Schedule" nav link when applicable
  - [ ] Create staff schedule page route

### Phase 2: Customer Polish (After MVP)

- [ ] Create `BookingDetailsDialog.razor`
- [ ] Create `RescheduleBookingDialog.razor`
- [ ] Create `SubmitReviewDialog.razor`
- [ ] Add localization resources
- [ ] Test customer flows

### Phase 3: Business & Staff Polish

- [ ] Create `ServiceEditor.razor`
- [ ] Create `AvailabilityEditor.razor`
- [ ] Create `ExceptionEditor.razor`
- [ ] Create `ResourceStaffAssignment.razor`
- [ ] Create `StaffDashboard.razor` (full version with stats)
- [ ] Create `AppointmentActionBar.razor` (check-in, complete buttons)
- [ ] Test business & staff flows

### Phase 4: Advanced

- [ ] Create `BookingCalendarView.razor`
- [ ] Create `BookingAnalytics.razor`
- [ ] Integration tests

---

## Related Documentation

- [DEVELOPMENT_RULES.md](DEVELOPMENT_RULES.md) - XAF entity rules
- [Entities/BookableResource.cs](Sivar.Os.Shared/Entities/BookableResource.cs)
- [Entities/ResourceBooking.cs](Sivar.Os.Shared/Entities/ResourceBooking.cs)
- [Components/Booking/](Sivar.Os.Client/Components/Booking/) - Existing components
