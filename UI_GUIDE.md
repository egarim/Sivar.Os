# 📱 Sivar.Os UI Overview - Visual Guide

**Date:** 2026-02-17  
**Status:** Running in Development mode on http://127.0.0.1:5001

---

## 🎨 Design System

**Framework:** MudBlazor (Material Design)  
**Color Scheme:** Purple gradient (primary), Dark theme support  
**Fonts:** Roboto, Material icons  
**Responsive:** Mobile-first design

---

## 🏠 Landing Page (/)

### Layout
The landing page uses a **split-screen design**:

**Left Side (Branding):**
- Purple gradient background (135deg, #667eea → #764ba2)
- Large "Sivar.Os" logo (72px, bold)
- Tagline: "Operating System for Modern Countries"
- Description text
- Animated entrance (fadeInLeft)

**Right Side (Authentication):**
- Clean white background
- Login/Register form
- Modern card-based UI
- Shadow effects for depth
- Animated entrance (fadeInRight)

### Visual Style
```
┌─────────────────────────────────────────────────────────────┐
│                                                              │
│  ╔═══════════════════════╗  ║                              │
│  ║                       ║  ║   🔐 Login / Register        │
│  ║  Sivar.Os             ║  ║                              │
│  ║                       ║  ║   [Email Input]              │
│  ║  Operating System     ║  ║                              │
│  ║  for Modern Countries ║  ║   [Password Input]           │
│  ║                       ║  ║                              │
│  ║  Description text...  ║  ║   [Login Button]             │
│  ║                       ║  ║                              │
│  ╚═══════════════════════╝  ║   [Register Link]            │
│   Purple Gradient           ║   White Background           │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎯 Main App Structure

### Navigation Components
The app includes:
- **Top App Bar** - Logo, search, notifications, profile
- **Side Navigation** - Main menu (collapsible)
- **Content Area** - Dynamic page content
- **Bottom Bar** - Mobile navigation (responsive)

### Main Sections (Based on Routes)

#### 1. **Feed / Home** (`/feed`)
- Activity feed with posts
- Infinite scroll
- Post cards with:
  - Author profile pic + name
  - Post content (text, images, videos)
  - Reactions (like, love, etc.)
  - Comments section
  - Share button
  - Timestamp

#### 2. **Profile** (`/profile`)
- Profile header:
  - Cover photo
  - Profile picture
  - Display name, handle
  - Bio
  - Stats (followers, following, posts)
  - Edit button (own profile)
  - Follow button (other profiles)
- Profile tabs:
  - Posts
  - Photos
  - About
  - Followers/Following

#### 3. **Booking System** (`/bookings`)
- **For Photo Studio Module 1:**
  - Calendar view
  - Available time slots
  - Service selection
  - Date/time picker
  - Customer information form
  - Confirmation dialog

#### 4. **Search** (`/search`)
- Search bar (persistent)
- Filter chips (People, Posts, Services)
- Results grid
- Quick filters

#### 5. **Notifications** (`/notifications`)
- Notification list
- Grouped by type:
  - Social (likes, comments, follows)
  - Bookings (confirmations, reminders)
  - System messages
- Mark as read functionality

#### 6. **Messages** (`/messages`)
- Conversation list (left sidebar)
- Chat window (right panel)
- Message composer
- File attachments
- Emoji picker

---

## 🎨 Component Library (MudBlazor)

The app uses MudBlazor components:

- **MudCard** - Post cards, profile cards
- **MudButton** - Primary, secondary, outlined
- **MudTextField** - Input fields
- **MudSelect** - Dropdowns
- **MudDialog** - Modals (post creation, confirmations)
- **MudDrawer** - Side navigation
- **MudAppBar** - Top bar
- **MudAvatar** - Profile pictures
- **MudChip** - Tags, categories
- **MudDatePicker** - Calendar selection
- **MudTimePicker** - Time selection
- **MudTable** - Data tables
- **MudPagination** - Page navigation

---

## 📸 Photo Studio UI (Module 1)

### Booking Flow

```
Step 1: Service Selection
┌─────────────────────────────────────┐
│  Choose Your Service                │
│  ┌─────────┐  ┌─────────┐          │
│  │ 💍      │  │ 🎀      │          │
│  │ Wedding │  │ Quincea │          │
│  │         │  │ ñera    │          │
│  └─────────┘  └─────────┘          │
│  ┌─────────┐  ┌─────────┐          │
│  │ 👔      │  │ 📷      │          │
│  │ Corporate│ │ Portrait│          │
│  └─────────┘  └─────────┘          │
└─────────────────────────────────────┘

Step 2: Date & Time
┌─────────────────────────────────────┐
│  📅 Select Date                     │
│  [Calendar Widget]                  │
│                                      │
│  ⏰ Select Time                     │
│  [Available Slots]                  │
│  □ 09:00 AM  ☑ 10:00 AM  □ 11:00   │
└─────────────────────────────────────┘

Step 3: Contact Info
┌─────────────────────────────────────┐
│  Your Information                   │
│  [Name]                             │
│  [Email]                            │
│  [Phone]                            │
│  [Notes/Special Requests]           │
│                                      │
│  [← Back]  [Confirm Booking →]     │
└─────────────────────────────────────┘

Step 4: Confirmation
┌─────────────────────────────────────┐
│  ✅ Booking Confirmed!              │
│                                      │
│  Service: Wedding Photography       │
│  Date: March 15, 2026               │
│  Time: 10:00 AM - 6:00 PM           │
│                                      │
│  Check your email for details       │
│  WhatsApp confirmation sent         │
│                                      │
│  [View My Bookings]  [Done]         │
└─────────────────────────────────────┘
```

---

## 🎨 Color Palette

```css
Primary: #667eea (Purple)
Secondary: #764ba2 (Deep Purple)
Success: #4caf50 (Green)
Error: #f44336 (Red)
Warning: #ff9800 (Orange)
Info: #2196f3 (Blue)

Background (Light): #f5f5f5
Background (Dark): #1a1a27
Surface: #ffffff
Text Primary: #1a1a27
Text Secondary: #666666
```

---

## 📱 Responsive Breakpoints

```
Mobile:  < 600px  (Single column, bottom nav)
Tablet:  600-960px (Two columns, collapsible nav)
Desktop: > 960px  (Three columns, permanent nav)
```

---

## 🎭 Animations & Effects

**Page Transitions:**
- Fade in/out
- Slide up/down
- Scale

**Component Animations:**
- Button ripple
- Card hover elevation
- Skeleton loaders
- Shimmer effects

**Loading States:**
- Spinner (MudProgressCircular)
- Linear progress bar
- Skeleton screens

---

## 🖼️ Example: Post Card Component

```
┌─────────────────────────────────────────────┐
│  👤 Photo Studio SV    ⋮                    │
│     @studio_photo_sv   • 2d ago             │
├─────────────────────────────────────────────┤
│                                              │
│  ¡Capturamos los momentos más especiales    │
│  de tu boda! 💍✨                           │
│                                              │
│  Nuestro equipo de fotógrafos...            │
│                                              │
│  [Image Gallery - Wedding Photo]            │
│                                              │
├─────────────────────────────────────────────┤
│  ❤️ 45  💬 12  🔄 8                         │
│                                              │
│  #BodaSV #FotografíaProfesional             │
└─────────────────────────────────────────────┘
```

---

## 🔧 Admin Dashboard (Future)

**For business owners:**
- Booking calendar (monthly/weekly/daily views)
- Customer management
- Service configuration
- Analytics dashboard
- Revenue reports
- Customer reviews

---

## 📊 Current Implementation Status

### ✅ Implemented Components
- Landing page with split design
- Authentication UI (dev mode)
- Navigation structure
- Post cards
- Profile pages
- Basic layouts

### 🚧 In Progress
- Booking calendar UI
- Payment integration
- WhatsApp notifications
- Image upload interface

### 📝 Planned
- Advanced analytics
- Multi-profile switching
- Dark mode toggle
- Push notifications
- Mobile app (PWA)

---

## 🎯 Design Philosophy

**Key Principles:**
1. **Mobile First** - Optimized for phones (El Salvador's primary device)
2. **Spanish Primary** - UI text in Spanish, English secondary
3. **WhatsApp Native** - Customers use WhatsApp, admins use web
4. **Fast & Light** - Works on slow connections
5. **Familiar** - Looks like apps users already know (Instagram, Facebook)
6. **Professional** - Credible for businesses

---

## 🖼️ Visual References

The UI design is inspired by:
- **Instagram** - Feed and profile layouts
- **Facebook** - Post interactions
- **LinkedIn** - Professional profiles
- **Calendly** - Booking interface
- **WhatsApp** - Chat and messaging

---

**To See It Live:**
Once you configure the domain, you can access:
- Landing page: `https://your-domain.com/`
- App feed: `https://your-domain.com/feed`
- Profile: `https://your-domain.com/profile/[handle]`
- Bookings: `https://your-domain.com/bookings`

**Or Test Locally:**
```bash
# With Development environment (current)
curl http://127.0.0.1:5001/

# Or use browser (not available on server)
# Wait for domain setup
```

---

**Note:** Full screenshots would require a browser, which isn't available on this server. Once you access via domain, you'll see the complete visual design! 🎨
