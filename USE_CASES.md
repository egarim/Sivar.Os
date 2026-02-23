# 📋 Sivar.Os Use Cases - Photo Studio Module

**Version:** 1.0  
**Date:** 2026-02-17  
**Context:** MVP for photo studio booking in El Salvador

---

## 👤 USER PERSPECTIVE (Customer)

### Use Case 1: Maria Discovers Photo Studio via Social Media

**Actor:** Maria García (25, planning her wedding)  
**Goal:** Find a reliable photographer for her wedding  
**Context:** Scrolling Instagram, sees Sivar.Os post

#### Scenario

**Step 1: Discovery**
- Maria is scrolling Instagram during lunch break
- Sees sponsored post from "Studio Fotográfico El Salvador"
- Post shows beautiful wedding photos with caption in Spanish
- Link says: "Book your date on Sivar.Os"
- She clicks the link → Opens Sivar.Os app

**Step 2: Landing Page**
```
URL: https://app.sivar.lat/profile/studio_photo_sv

She sees:
- Cover photo: Stunning wedding gallery
- Profile name: "Studio Fotográfico El Salvador"
- Bio: "Especialistas en fotografía profesional: Bodas, Quinceañeras, Retratos 📸✨"
- Contact: Phone, email, location (San Salvador)
- Stats: 125 posts, 1.2K followers, 45 following
- Recent posts showing different events
```

**Step 3: Browse Portfolio**
- Scrolls through 125 posts
- Sees wedding photos, quinceañeras, corporate events
- Reads testimonials in comments (⭐⭐⭐⭐⭐)
- Clicks on a wedding album post
- Sees pricing info: "$800 - 8 hours, 300+ photos edited"

**Step 4: Decision to Book**
- Clicks "📅 Book Now" button on profile
- Prompted to create account (or login)
- Chooses "Sign up with Email" (no Keycloak complexity)
- Enters: maria.garcia@gmail.com, creates password
- Account created, redirected to booking page

**Step 5: Booking Flow**

**5a. Service Selection**
```
Sees service cards:
┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│    💍       │  │    🎀       │  │    👔       │
│  Wedding    │  │ Quinceañera │  │ Corporate   │
│  $800       │  │   $450      │  │   $300      │
│  8 hours    │  │   4 hours   │  │   3 hours   │
│  [Select]   │  │  [Select]   │  │  [Select]   │
└─────────────┘  └─────────────┘  └─────────────┘
```
- Selects "💍 Wedding Photography"
- Sees package details expand:
  - Up to 8 hours coverage
  - 300+ edited photos in HD
  - Digital album
  - 5-minute highlight video
  - Pre-wedding session included
  - Raw files available (+$100)

**5b. Date & Time Selection**
```
Calendar shows:
- Today: Feb 17, 2026
- Available dates marked in green
- Booked dates grayed out
- Selects: March 15, 2026 (Saturday)

Time slots:
□ 09:00 AM - Full day (8h) - Available
□ 10:00 AM - Full day (8h) - Available
☑ 12:00 PM - Full day (8h) - SELECTED
□ 02:00 PM - Half day (4h) - Available
```

**5c. Contact Information**
```
Pre-filled from account:
Name: Maria García
Email: maria.garcia@gmail.com
Phone: [+503 7234-5678] ← She adds this

Additional fields:
Event Type: [Wedding ▾]
Venue: [Hotel El Salvador Grande]
Guest Count: [150]
Special Requests: 
[Necesitamos fotos de la ceremonia religiosa 
y la recepción. También queremos un álbum 
físico de 30x40cm. ¿Es posible?]
```

**5d. Confirmation**
- Reviews order:
  - Service: Wedding Photography
  - Date: Saturday, March 15, 2026
  - Time: 12:00 PM - 8:00 PM (8 hours)
  - Location: Hotel El Salvador Grande
  - Total: $800
- Sees payment options:
  - 💳 Pay $200 deposit now (reserve date)
  - 💵 Pay $800 full (save 5%)
  - 🤝 Pay in person (requires approval)
- She selects: "Pay $200 deposit now"

**Step 6: Payment** (Future - not in MVP)
- Would redirect to payment processor
- For MVP: Shows "Booking request sent"

**Step 7: Confirmation**
```
✅ Booking Request Sent!

Your booking request has been sent to Studio Fotográfico El Salvador.

Booking Details:
• Service: Wedding Photography
• Date: March 15, 2026
• Time: 12:00 PM - 8:00 PM
• Location: Hotel El Salvador Grande
• Total: $800

What's Next?
1. Studio will review your request (usually within 24h)
2. You'll receive a WhatsApp message with confirmation
3. Payment instructions will be sent via email

Reference: #BOOK-2026-0015

[📱 Save to Calendar]  [💬 Message Studio]  [← Back to Profile]
```

**Step 8: WhatsApp Notification** (Automated)
```
WhatsApp from: +503 2222-3333 (Studio Fotográfico)

¡Hola Maria! 👋

Recibimos tu solicitud de reserva:

📅 Fecha: 15 de Marzo, 2026
⏰ Hora: 12:00 PM - 8:00 PM
📸 Servicio: Fotografía de Bodas ($800)
📍 Lugar: Hotel El Salvador Grande

Revisaremos tu solicitud y te confirmaremos en menos de 24 horas.

¿Tienes preguntas? Responde a este mensaje.

Referencia: #BOOK-2026-0015

Saludos,
Studio Fotográfico El Salvador
```

**Step 9: Follow-up**
- Next day, Maria receives confirmation via:
  - ✅ WhatsApp: "Confirmed! See you March 15"
  - ✅ Email: Full details + contract PDF
  - ✅ Sivar.Os notification: "Your booking is confirmed"
- She can track booking status in app under "My Bookings"

#### Success Criteria ✅
- Maria found studio easily
- Booking process took < 5 minutes
- All information was clear and in Spanish
- She received immediate confirmation
- WhatsApp follow-up felt personal and professional

#### Pain Points to Avoid ❌
- No complicated registration (email only)
- No confusing payment flow (deposit option clear)
- No English-only interface
- No missing contact info
- No uncertainty about next steps

---

### Use Case 2: Carlos Books Last-Minute Portrait Session via WhatsApp

**Actor:** Carlos Méndez (32, needs professional headshots for LinkedIn)  
**Goal:** Book urgent portrait session  
**Context:** Just got job interview, needs photos ASAP

#### Scenario

**Step 1: WhatsApp Discovery**
- Carlos's friend recommended Studio Fotográfico
- He finds their WhatsApp: +503 2222-3333
- Sends message at 10:30 AM:

```
Carlos: Hola! Necesito fotos profesionales 
        para LinkedIn. ¿Tienen espacio esta semana?
```

**Step 2: WhatsApp Bot Response** (Automated - Future)
```
Studio Bot: ¡Hola Carlos! 👋

Claro, tenemos disponibilidad esta semana.

📸 Sesión de Retratos Profesionales
💰 $150 - 1 hora
📦 Incluye: 20 fotos editadas en alta resolución

Ver disponibilidad: app.sivar.lat/book/portraits

¿O prefieres que te atienda un asesor humano?
[1] Ver horarios
[2] Hablar con asesor
```

**Step 3: Quick Booking Link**
- Carlos clicks link → Opens booking page
- Pre-filled with:
  - Service: Portrait Session
  - WhatsApp: +503 7XXX-XXXX (auto-detected)
- Sees calendar with available slots THIS WEEK
- Selects: Thursday, Feb 20, 4:00 PM
- Enters name: Carlos Méndez
- Adds note: "Fotos para LinkedIn - fondo blanco preferible"

**Step 4: Instant Confirmation**
- Booking submitted
- WhatsApp message within seconds:

```
Studio Bot: ✅ Reserva confirmada!

📅 Jueves, 20 de Febrero
⏰ 4:00 PM - 5:00 PM
📸 Sesión de Retratos
💰 $150 (pagar en el estudio)

📍 Dirección:
   Av. Los Próceres 123
   San Salvador

🚗 Cómo llegar: [Google Maps]

Referencia: #BOOK-2026-0016

Nos vemos el jueves!
```

**Step 5: Day of Session**
- 3:00 PM: Receives WhatsApp reminder
  "Recordatorio: Tu sesión es en 1 hora 📸"
- 4:00 PM: Arrives at studio
- 5:00 PM: Session complete
- 5:15 PM: Receives WhatsApp with sample preview photo
  "Aquí está un preview! Las fotos finales en 48h"

**Step 6: Delivery**
- 48h later: WhatsApp message
  "✅ Tus fotos están listas!"
  Link to Sivar.Os gallery (password protected)
- Carlos downloads all 20 photos in HD
- Leaves 5-star review on Sivar.Os profile

#### Success Criteria ✅
- Booking via WhatsApp (no app required)
- Same-week availability visible
- Instant confirmation
- Automated reminders
- Quick turnaround (48h)

---

### Use Case 3: Ana Reschedules Quinceañera via App

**Actor:** Ana Torres (mother, planning daughter's XV)  
**Goal:** Reschedule photo session due to venue change  
**Context:** Booked 2 months ago, venue changed date

#### Scenario

**Step 1: Login to Account**
- Opens Sivar.Os app on her phone
- Goes to "My Bookings" tab
- Sees upcoming booking:
  
```
📸 Quinceañera Photography
📅 April 12, 2026
⏰ 2:00 PM - 6:00 PM
📍 Salón Los Arcos
💰 $450 (Paid: $150 deposit)
Status: ✅ Confirmed

[View Details] [Reschedule] [Cancel]
```

**Step 2: Reschedule Request**
- Clicks "Reschedule" button
- Sees warning: "Date changes may affect pricing"
- Continues
- Calendar shows:
  - Original date (April 12) marked
  - Available alternative dates in green
  - Some dates have price adjustments (+$50 weekend premium)

**Step 3: Select New Date**
- Selects: April 19, 2026 (also Saturday)
- Sees message: "No additional charge - same day of week"
- Updates venue: Salón Crystal Palace
- Adds note: "La fiesta será más grande, aprox 200 invitados"

**Step 4: Confirmation**
```
Reschedule Request Sent

New details:
📅 April 19, 2026 (changed from April 12)
⏰ 2:00 PM - 6:00 PM
📍 Salón Crystal Palace (changed from Los Arcos)
💰 $450 (no change)

Studio will confirm within 24h.
Deposit of $150 remains valid.

[OK]
```

**Step 5: Studio Approval**
- Studio owner sees reschedule request in admin dashboard
- Approves it (April 19 is available)
- Ana receives:
  - ✅ App notification: "Reschedule approved"
  - ✅ WhatsApp: "Confirmado! Nueva fecha: 19 de Abril"
  - ✅ Email: Updated booking confirmation

#### Success Criteria ✅
- Easy self-service reschedule
- Clear pricing transparency
- Maintains deposit (no re-payment)
- Fast approval process
- Multi-channel confirmation

---

## 🏢 BUSINESS PERSPECTIVE (Studio Owner)

### Use Case 4: Studio Owner Sets Up Business Profile

**Actor:** Roberto (35, owns Studio Fotográfico El Salvador)  
**Goal:** Launch online booking system  
**Context:** Currently takes bookings via phone/WhatsApp only - wants to automate

#### Scenario

**Step 1: Registration**
- Roberto hears about Sivar.Os from brother (Joche!)
- Opens: app.sivar.lat
- Clicks "Create Business Account"
- Enters:
  - Business name: Studio Fotográfico El Salvador
  - Email: info@studiophoto.sv
  - Phone: +503 2222-3333
  - Password: ••••••••
- Selects account type: "Business - Photography Services"

**Step 2: Profile Setup Wizard**

**2a. Basic Information**
```
Tell us about your business:

Display Name: [Studio Fotográfico El Salvador]
Username: [studio_photo_sv]
Category: [Photography & Events ▾]

Bio: [Especialistas en fotografía profesional: 
      Bodas, Quinceañeras, Retratos, Eventos 
      Corporativos y más. 📸✨]

Location:
  City: [San Salvador]
  Address: [Av. Los Próceres 123]
  [📍 Show on map]

Contact:
  Public Email: [info@studiophoto.sv]
  Public Phone: [+503 2222-3333]
  Website: [https://studiophoto.sv]
  WhatsApp: [+503 2222-3333]
```

**2b. Business Hours**
```
When are you available?

Monday:    [Closed]
Tuesday:   [09:00 AM] - [06:00 PM]
Wednesday: [09:00 AM] - [06:00 PM]
Thursday:  [09:00 AM] - [06:00 PM]
Friday:    [09:00 AM] - [06:00 PM]
Saturday:  [10:00 AM] - [08:00 PM] (Premium +$50)
Sunday:    [By appointment only]

Timezone: [America/El_Salvador (GMT-6)]
```

**2c. Services & Pricing**
```
Create your service packages:

Service 1:
  Name: [Wedding Photography]
  Icon: [💍]
  Duration: [8 hours]
  Price: [$800]
  Deposit Required: [$200] (25%)
  Description:
    [Up to 8 hours coverage, 300+ edited photos in HD,
     Digital album, 5-minute highlight video, 
     Pre-wedding session included]
  
  Add-ons:
    □ Raw files (+$100)
    □ Physical album 30x40cm (+$150)
    □ Extra hour (+$120)

[+ Add Another Service]
```

**2d. Upload Portfolio**
```
Showcase your work:

Cover Photo: [Upload] (Recommended: 1920x480px)
Profile Picture: [Upload] (Recommended: 400x400px)

Portfolio Gallery:
[Drag & drop up to 50 photos]

- Wedding_Sample_1.jpg ✅
- Wedding_Sample_2.jpg ✅
- Quinceanera_1.jpg ✅
- Corporate_Event.jpg ✅

[+ Upload More]
```

**Step 3: Booking Calendar Setup**
```
Configure your availability:

Default buffer between bookings: [1 hour]
Maximum bookings per day: [2]
Advance booking window: [3 months]
Cancellation policy: [72 hours notice for full refund]

Block specific dates:
[+ Add blocked date] (vacations, holidays)

Sync with external calendar:
□ Google Calendar
□ Outlook
□ iCal URL
```

**Step 4: Payment Settings** (Future)
```
How do you want to get paid?

□ Online (Credit/Debit card) - 3% fee
  Connect: [Stripe] [PayPal]

☑ Bank transfer
  Bank: [Banco Agricola]
  Account: [XXXX-XXXX-XXXX-1234]

☑ Cash (in-person)

Deposit Policy:
☑ Require deposit to confirm booking
  Amount: [25%] of total
```

**Step 5: WhatsApp Integration**
```
Connect your WhatsApp Business:

Phone: +503 2222-3333
[Verify with SMS code]

Automated Messages:
☑ Booking confirmations
☑ Reminders (24h before)
☑ Thank you messages (after service)
☑ Review requests (3 days after)

Message template preview:
"¡Hola [Name]! Confirmamos tu reserva para 
 [Service] el [Date] a las [Time]. 
 Nos vemos pronto! 📸"
```

**Step 6: Launch Profile**
- Reviews preview of public profile
- Clicks "Publish Profile"
- Profile goes live at: app.sivar.lat/profile/studio_photo_sv
- Receives welcome email with:
  - Link to admin dashboard
  - Marketing tips
  - Social media sharing buttons

#### Success Criteria ✅
- Setup completed in < 15 minutes
- Profile looks professional
- Services clearly defined
- Calendar synchronized
- WhatsApp integration active

---

### Use Case 5: Studio Owner Manages Daily Bookings

**Actor:** Roberto (studio owner)  
**Goal:** Manage bookings, respond to requests, track schedule  
**Context:** Morning routine - checking bookings for the week

#### Scenario

**Step 1: Morning Dashboard Check**
- 8:00 AM: Roberto opens Sivar.Os on his tablet
- Goes to "Business Dashboard"
- Sees overview:

```
╔═══════════════════════════════════════════════════════════╗
║  Dashboard - Studio Fotográfico El Salvador              ║
╠═══════════════════════════════════════════════════════════╣
║  Today: Tuesday, February 18, 2026                        ║
║                                                            ║
║  📅 Today's Bookings: 2                                   ║
║  ⏰ 10:00 AM - Portrait Session (Carlos Méndez)          ║
║  ⏰ 03:00 PM - Wedding Consultation (María García)       ║
║                                                            ║
║  🔔 Pending Requests: 3                                   ║
║  💰 Revenue This Week: $1,450                            ║
║  ⭐ New Reviews: 2 (5.0 average)                         ║
║                                                            ║
║  [View Calendar] [Pending Requests] [Messages]           ║
╚═══════════════════════════════════════════════════════════╝
```

**Step 2: Review Pending Requests**
- Clicks "Pending Requests" (3 notifications)

**Request 1: Maria's Wedding**
```
📸 Wedding Photography
👤 María García
📅 Requested: March 15, 2026, 12:00 PM
📍 Hotel El Salvador Grande
💰 $800 (Deposit: $200)

Customer Note:
"Necesitamos fotos de ceremonia religiosa y recepción. 
 También queremos álbum físico 30x40cm. ¿Es posible?"

[✓ Approve] [✗ Decline] [💬 Send Message]
```

- Roberto clicks "✓ Approve"
- Adds response: "Confirmado! El álbum físico tiene costo adicional de $150. Te enviaré detalles por WhatsApp."
- Booking status changes to "Confirmed"
- Maria receives auto-notification

**Request 2: Last-minute Corporate**
```
📸 Corporate Event Photography
👤 Empresa ABC S.A.
📅 Requested: Tomorrow Feb 19, 9:00 AM
📍 Torre Futura, San Salvador
💰 $300

[✓ Approve] [✗ Decline] [💬 Send Message]
```

- Roberto checks tomorrow's calendar → Already has 2:00 PM booking
- Morning is free → Approves
- Corporate event confirmed

**Request 3: Conflicting Date**
```
📸 Quinceañera Photography
👤 Rosa López
📅 Requested: March 15, 2026, 3:00 PM
💰 $450

⚠️ CONFLICT: You already have a booking on this date
    (María García - Wedding, 12:00 PM - 8:00 PM)

[✗ Decline with Alternative] [💬 Offer Reschedule]
```

- Roberto clicks "✗ Decline with Alternative"
- System suggests: March 16 (next day) or March 22
- Sends message: "Hola Rosa! Lamentablemente ya tengo otra boda ese día. ¿Te funcionaría el 16 o 22 de marzo?"

**Step 3: Check Calendar View**
- Clicks "View Calendar"
- Sees month view:

```
March 2026
Su  Mo  Tu  We  Th  Fr  Sa
                        1
2   3   4   5   6   7   8
9   10  11  12  13  14  [15] ← Booked (María - Wedding)
16  17  18  19  20  21  22
23  24  25  26  27  28  29
30  31

Legend:
🟢 Available
🟡 Partial availability
🔴 Fully booked
⚫ Blocked (vacation, etc.)
```

**Step 4: Respond to Customer Messages**
- 3 new messages in inbox

**Message from Carlos:**
```
Carlos: Hola! ¿Puedo llegar 15 minutos antes 
        para preparar el fondo?

Studio: Claro Carlos! Puedes llegar a las 3:45 PM
        sin problema. Te esperamos!
        
        [Send]
```

**Message from María:**
```
María: Confirmaron! Nos vemos el 15 de marzo 😊
       ¿Qué necesito llevar?

Studio: [Quick Reply Templates]
        □ Wedding day checklist
        □ What to bring
        □ Timeline suggestions
        
        Selects: "What to bring" →
        
        "Para el día de tu boda trae:
         📋 Cronograma del evento
         👗 Lista de fotos específicas que quieres
         💍 Contacto de organizador/coordinador
         🎵 Preferencia de música para video
         
         Yo llevo todo el equipo necesario!"
```

**Step 5: Post Daily Content**
- 9:00 AM: Time to post on profile
- Goes to "Create Post"
- Uploads photo from recent wedding
- Writes:

```
¡Hermosa boda de Andrea & Luis! 💕

Gracias por confiar en nosotros para capturar 
su día especial. Cada momento fue mágico ✨

¿Te casas pronto? Reserva tu fecha:
app.sivar.lat/book/wedding

#BodaSV #FotografíaProfesional #SanSalvador

[📸 Photo Gallery - 4 images]
```

- Posts to profile
- Also shares to Instagram (cross-post feature)

**Step 6: End of Day Review**
- 6:00 PM: Checks dashboard again

```
Today's Summary:
✅ Completed: 2 sessions
✅ Approved: 2 new bookings
✅ Revenue: $450 (Carlos paid cash, corporate paid deposit)
⭐ Reviews: 1 new (5 stars from Carlos)
📊 Profile views: 47
👥 New followers: 8

This Week:
📅 Bookings: 5 confirmed
💰 Projected revenue: $1,850

[Export Report] [View Analytics]
```

#### Success Criteria ✅
- All bookings managed from one dashboard
- Conflicts detected automatically
- Quick message responses
- Revenue tracking visible
- Marketing and operations in one place

---

### Use Case 6: Studio Owner Handles Cancellation & Refund

**Actor:** Roberto  
**Goal:** Process cancellation professionally  
**Context:** Customer needs to cancel due to emergency

#### Scenario

**Step 1: Cancellation Request**
- Roberto receives WhatsApp from Ana Torres:

```
Ana: Hola Roberto, lamentablemente tenemos 
     que cancelar la quinceañera del 12 de abril.
     Mi hija tuvo un accidente y estará en 
     recuperación. ¿Podemos reprogramar más adelante?
```

**Step 2: Check Booking**
- Opens Sivar.Os dashboard
- Goes to "Bookings" → Finds Ana's booking

```
📸 Quinceañera Photography
👤 Ana Torres
📅 April 12, 2026
💰 $450 (Paid: $150 deposit)
Status: ✅ Confirmed
Booked: 45 days ago

Cancellation Policy:
- 72+ hours: Full refund
- 24-72 hours: 50% refund
- < 24 hours: No refund
- Emergency: Case by case

[Cancel Booking] [Reschedule] [Contact Customer]
```

**Step 3: Process Cancellation**
- Clicks "Cancel Booking"
- Sees options:

```
Cancellation Reason:
( ) Customer request - refund deposit
( ) Customer request - credit for future booking
( ) Emergency situation
( ) No-show

Refund Amount: [$150] (100% - emergency situation)
OR
Credit for Future: [$150] valid until [Dec 31, 2026]

Notes: [___________________________________]
```

- Roberto selects: "Emergency situation"
- Chooses: "Credit for future booking"
- Adds note: "Espero que tu hija se recupere pronto. El crédito de $150 está disponible cuando estés lista."

**Step 4: Customer Notification**
- Ana receives:
  - ✅ WhatsApp: "Cancelación procesada. Tienes crédito de $150 válido hasta diciembre. Espero que todo mejore! 🙏"
  - ✅ Email: Cancellation confirmation with credit code
  - ✅ App notification: "Booking cancelled - $150 credit available"

**Step 5: Calendar Update**
- April 12 slot automatically becomes available
- Appears as open date in booking calendar
- Roberto can now accept new bookings for that date

**Step 6: Future Booking with Credit**
- 2 months later: Ana's daughter recovered
- Ana books again for June 20
- At payment step, system shows:

```
Service: Quinceañera Photography
Price: $450

Available Credits:
☑ Credit from cancelled booking: -$150

New Total: $300

[Proceed to Payment]
```

- She pays $300 instead of $450
- Booking confirmed

#### Success Criteria ✅
- Compassionate handling of emergency
- Flexible refund/credit options
- Clear communication
- Credit tracked automatically
- Good customer relationship maintained

---

## 🎯 Advanced Use Cases

### Use Case 7: Customer Leaves Review & Shares Experience

**Actor:** María (after her wedding)  
**Goal:** Thank the studio and help others find them  
**Context:** 1 week after wedding, received final photos

#### Scenario

**Step 1: Review Prompt**
- María receives WhatsApp: "Hola María! ¿Cómo quedaron las fotos? Nos encantaría saber tu opinión 😊"
- Link to review page: app.sivar.lat/review/BOOK-2026-0015

**Step 2: Write Review**
```
Rate your experience:
⭐⭐⭐⭐⭐ (5 stars)

Tell us about your experience:
[El servicio fue increíble! Roberto y su equipo 
fueron muy profesionales. Las fotos quedaron 
hermosas y el video nos hizo llorar de emoción.
Súper recomendados para bodas! 💕]

Upload photos (optional):
[📸 Photo1.jpg] [📸 Photo2.jpg]

[Submit Review]
```

**Step 3: Review Posted**
- Review appears on studio profile
- Roberto receives notification
- He responds: "Gracias María! Fue un placer ser parte de su día especial 💕"

**Step 4: Share on Social Media**
- María wants to share the studio info
- Clicks "Share" on studio profile
- Options:
  - 📱 WhatsApp
  - 📘 Facebook
  - 📷 Instagram
  - 🔗 Copy link

- She posts on Facebook:
  "Súper recomendado! Studio Fotográfico hizo las fotos de nuestra boda. Excelente trabajo! 📸 app.sivar.lat/profile/studio_photo_sv"

---

### Use Case 8: Studio Runs Promotion Campaign

**Actor:** Roberto  
**Goal:** Boost bookings for slow season (June-July)  
**Context:** Creating summer promotion

#### Scenario

**Step 1: Create Promotion**
```
Dashboard → Marketing → Create Promotion

Promotion Name: [Summer Special - 20% OFF]
Valid: [June 1 - July 31, 2026]
Discount: [20%] off
Apply to:
☑ Wedding Photography
☑ Quinceañera Photography
□ Corporate Events

Promo Code: [SUMMER2026]
Max Uses: [50]

Auto-apply: ☑ Yes (show on booking page)
```

**Step 2: Announcement Post**
- Creates social media post:

```
🌟 PROMOCIÓN VERANO 2026 🌟

¡20% DE DESCUENTO en paquetes de Bodas y Quinceañeras!

💍 Bodas: $800 → $640
🎀 XV Años: $450 → $360

Válido solo Junio-Julio 2026
Cupos limitados!

Reserva ya: app.sivar.lat/book
Código: SUMMER2026

#PromociónSV #FotografíaDeBodas #Descuento

[Post] [Schedule for later]
```

**Step 3: Track Results**
- Goes to Analytics dashboard
- Sees:

```
Campaign Performance: SUMMER2026

📊 Stats (15 days):
- Promo views: 1,247
- Bookings with promo: 8
- Revenue: $5,120 (discounted)
- Conversion rate: 0.64%

🎯 Compared to normal bookings: +156%

Top Sources:
1. Facebook share: 45%
2. WhatsApp share: 30%
3. Instagram: 25%
```

---

## 📊 Summary: Key Metrics for Demo

### Customer Journey Metrics
- **Discovery to Booking:** < 5 minutes
- **Booking Confirmation:** < 24 hours
- **Customer Satisfaction:** ⭐⭐⭐⭐⭐ (5.0 avg)
- **Repeat Booking Rate:** 35% (with credit system)

### Business Owner Metrics
- **Setup Time:** < 15 minutes
- **Daily Management:** < 30 minutes
- **Booking Approval:** < 1 minute each
- **Revenue Tracking:** Real-time
- **Calendar Conflicts:** Auto-detected (100%)

### Platform Metrics
- **Mobile Usage:** 75% of customers
- **WhatsApp Integration:** 90% prefer it
- **Booking Completion Rate:** 85%
- **Average Booking Value:** $520

---

## 🎬 Demo Script Recommendations

### **Demo 1: Customer Experience (5 minutes)**
1. Show landing page (30s)
2. Browse studio profile (1m)
3. Book wedding package (2m)
4. Show confirmation + WhatsApp (1m)
5. Show booking management (30s)

### **Demo 2: Business Experience (5 minutes)**
1. Show dashboard overview (1m)
2. Approve/decline bookings (1m)
3. Manage calendar (1m)
4. Respond to messages (1m)
5. View analytics (1m)

### **Demo 3: WhatsApp Integration (3 minutes)**
1. Customer books via WhatsApp link (1m)
2. Auto-confirmation sent (30s)
3. Business receives notification (30s)
4. Two-way conversation (1m)

---

**These use cases are ready to implement and test!** 🚀
