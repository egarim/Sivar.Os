# Booking System Testing Guide

This guide explains how to test the booking flow from different perspectives: **Customer**, **Business Owner**, and **Resource Management**.

## Prerequisites

- ✅ Keycloak users synced (115 profiles linked)
- ✅ Sivar.Os application running
- ✅ All demo profiles have users with password: `SivarOs123!`

---

## Test Accounts

### Customer Accounts (Personal Profiles)
These are regular users who make bookings:

| Handle | Email | Display Name | Use Case |
|--------|-------|--------------|----------|
| `joche-ojeda` | joche-ojeda@sivar.lat | Joche Ojeda | Primary test customer |
| `oscar-ojeda` | oscar-ojeda@sivar.lat | Oscar Ojeda | Secondary test customer |
| `jaime-macias` | jaime-macias@sivar.lat | Jaime Macias | Alternative customer |
| `roberto-guzman` | roberto-guzman@sivar.lat | Roberto Guzman | Alternative customer |

### Business Accounts (Business Profiles)
These are businesses that offer bookable services/resources:

| Handle | Email | Category | Good For Testing |
|--------|-------|----------|------------------|
| `barberia-el-caballero` | barberia-el-caballero@sivar.lat | Barber Shop | Appointment bookings |
| `pupuseria-el-comalito` | pupuseria-el-comalito@sivar.lat | Restaurant | Table reservations |
| `sushi-house` | sushi-house@sivar.lat | Restaurant | Table reservations |
| `hospitaldiagnostico` | hospitaldiagnostico@sivar.lat | Healthcare | Medical appointments |
| `farmaciasannicolas` | farmaciasannicolas@sivar.lat | Pharmacy | Service bookings |
| `labmaxbloch` | labmaxbloch@sivar.lat | Laboratory | Lab appointments |
| `notariagarcia` | notariagarcia@sivar.lat | Legal | Notary appointments |

**Default Password for all accounts:** `SivarOs123!`

---

## Test Flow 1: Customer Makes a Booking

### Step 1: Login as Customer
1. Open Sivar.Os application
2. Login with: `joche-ojeda@sivar.lat` / `SivarOs123!`
3. You should see the main feed/chat interface

### Step 2: Search for Bookable Services (via Chat)
In the chat, try natural language queries:
```
"Quiero hacer una reservación en un restaurante"
"I want to book an appointment at a barber shop"
"Buscar servicios de laboratorio"
"Find restaurants that accept reservations"
```

### Step 3: Check Available Slots
```
"What times are available at Sushi House tomorrow?"
"¿Qué horarios tiene disponible la barbería?"
"Show me availability for next week"
```

### Step 4: Create a Booking
```
"Book a table at Sushi House for tomorrow at 7 PM for 2 people"
"Reservar cita en la barbería para el sábado a las 10 AM"
"Make an appointment at Lab Max Bloch for Monday morning"
```

### Step 5: View My Bookings
```
"Show my upcoming bookings"
"Ver mis reservaciones"
"What appointments do I have?"
```

### Expected Results:
- ✅ Search returns bookable resources
- ✅ Availability shows time slots
- ✅ Booking creates with confirmation code
- ✅ My bookings shows the new reservation

---

## Test Flow 2: Business Owner Manages Bookings

### Step 1: Login as Business Owner
1. Logout from customer account
2. Login with: `sushi-house@sivar.lat` / `SivarOs123!`

### Step 2: Create a Bookable Resource (First Time Setup)
Before receiving bookings, the business must create resources:

**Option A: Via UI (if available)**
- Navigate to Business Settings → Resources
- Create resource: "Mesa Principal" (Table)
- Set availability schedule

**Option B: Via Chat**
```
"Create a bookable table resource"
"Configurar recurso para reservaciones"
```

### Step 3: Set Availability
```
"Set my availability Monday to Friday 11 AM to 9 PM"
"Configurar horario de atención"
```

### Step 4: View Incoming Bookings
```
"Show today's bookings"
"Ver reservaciones de hoy"
"What appointments do I have this week?"
```

### Step 5: Manage Bookings
```
"Confirm booking ABC123"
"Cancel the 3 PM appointment"
"Reschedule booking XYZ to tomorrow at 5 PM"
```

### Expected Results:
- ✅ Can create bookable resources
- ✅ Can set availability schedules
- ✅ Can view all business bookings
- ✅ Can confirm/cancel/reschedule bookings

---

## Test Flow 3: Resource Management

### Resource Types
The system supports different resource types:

| Type | Example | Typical Use |
|------|---------|-------------|
| `Table` | Restaurant table | Dining reservations |
| `Room` | Meeting room, hotel room | Space booking |
| `Person` | Stylist, doctor, lawyer | Appointment with specific person |
| `Equipment` | Medical equipment, tools | Equipment rental |
| `Vehicle` | Car, van | Vehicle rental |
| `Generic` | General service | Any other bookable |

### Creating Resources (as Business)
Login as a business (e.g., `barberia-el-caballero@sivar.lat`):

```
"Create a new barber resource named 'Carlos - Barber'"
"Add a service: Haircut, 30 minutes, $10"
"Add a service: Beard trim, 15 minutes, $5"
"Set availability: Mon-Sat 9 AM to 6 PM"
```

### Resource Services (for Person-type resources)
A single resource (e.g., a barber) can offer multiple services:

| Service | Duration | Price |
|---------|----------|-------|
| Corte de cabello | 30 min | $8.00 |
| Recorte de barba | 15 min | $5.00 |
| Corte + Barba | 45 min | $12.00 |
| Afeitado clásico | 20 min | $7.00 |

---

## Test Flow 4: Complete Booking Lifecycle

### Booking Status Flow:
```
Pending → Confirmed → CheckedIn → Completed
                ↓           ↓
             Cancelled   NoShow
```

### Test Each Status Transition:

1. **Create Booking (Customer)**
   - Login: `joche-ojeda@sivar.lat`
   - Create booking → Status: `Pending`

2. **Confirm Booking (Business)**
   - Login: `barberia-el-caballero@sivar.lat`
   - Confirm the booking → Status: `Confirmed`

3. **Check-In (Business)**
   - When customer arrives
   - Check in the booking → Status: `CheckedIn`

4. **Complete (Business)**
   - After service is done
   - Complete the booking → Status: `Completed`

### Alternative Flows:

**Cancel (by Customer):**
```
"Cancel my appointment at barberia"
```

**Cancel (by Business):**
```
"Cancel booking ABC123, reason: Closed for holiday"
```

**Reschedule (either party):**
```
"Reschedule booking ABC123 to tomorrow at 3 PM"
```

**No-Show (Business marks):**
```
"Mark booking ABC123 as no-show"
```

---

## API Endpoints Reference

### Customer APIs
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/booking/resources/search` | POST | Search bookable resources |
| `/api/booking/resources/{id}` | GET | Get resource details |
| `/api/booking/resources/{id}/slots` | GET | Get available slots |
| `/api/booking/bookings` | POST | Create booking |
| `/api/booking/bookings/my` | GET | My upcoming bookings |
| `/api/booking/bookings/my/history` | GET | My booking history |

### Business APIs
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/booking/resources` | POST | Create resource |
| `/api/booking/resources/{id}` | PUT | Update resource |
| `/api/booking/business/bookings` | GET | All business bookings |
| `/api/booking/business/today` | GET | Today's bookings |
| `/api/booking/bookings/{id}/confirm` | PUT | Confirm booking |
| `/api/booking/bookings/{id}/cancel` | PUT | Cancel booking |
| `/api/booking/bookings/{id}/checkin` | PUT | Check-in customer |
| `/api/booking/bookings/{id}/complete` | PUT | Complete booking |

---

## Chat Commands Quick Reference

### Customer Commands
```
"Search for bookable [type] near me"
"Find restaurants with reservations"
"Show availability for [business] on [date]"
"Book [service] at [business] for [date] at [time]"
"Show my bookings"
"Cancel my booking at [business]"
"Reschedule my booking to [new date/time]"
```

### Business Commands
```
"Create a bookable resource"
"Set my availability"
"Show today's appointments"
"Show this week's bookings"
"Confirm booking [code]"
"Cancel booking [code]"
"Check in customer [code]"
"View business stats"
```

---

## Troubleshooting

### No Bookable Resources Found
- Business hasn't created resources yet
- Login as business and create resources first

### Cannot Create Booking
- Check if user is logged in
- Verify resource has availability
- Check minimum advance booking time

### Business Can't See Bookings
- Verify business profile type (must be "Business" or "Organization")
- Check that resources belong to the business

### Authentication Issues
- Default password: `SivarOs123!`
- Check Keycloak realm: `sivar-os`
- Verify user was synced (check XAF SeederLog)

---

## Database Verification Queries

### Check Bookable Resources
```sql
SELECT r."Name", r."ResourceType", r."Category", p."Handle" as business
FROM "BookableResources" r
JOIN "Profiles" p ON r."ProfileId" = p."Id"
WHERE r."IsActive" = true;
```

### Check Bookings
```sql
SELECT b."ConfirmationCode", b."Status", b."StartTime",
       r."Name" as resource, cp."Handle" as customer
FROM "ResourceBookings" b
JOIN "BookableResources" r ON b."ResourceId" = r."Id"
JOIN "Profiles" cp ON b."CustomerProfileId" = cp."Id"
ORDER BY b."StartTime" DESC
LIMIT 20;
```

### Check User-Profile Links
```sql
SELECT p."Handle", u."Email", u."KeycloakId"
FROM "Profiles" p
JOIN "Users" u ON p."UserId" = u."Id"
WHERE u."KeycloakId" IS NOT NULL
LIMIT 20;
```

---

## Next Steps After Testing

1. ✅ Verify all booking flows work
2. 📝 Document any issues found
3. 🔧 Fix edge cases
4. 🚀 Test with production-like data volume
5. 📊 Add analytics/reporting for bookings
