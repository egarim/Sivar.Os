# Sivar.Os Architecture

## 🏗️ System Overview

Sivar.Os is a modular social network and service platform designed for El Salvador. Module 1 (MVP) focuses on a photo studio booking system with WhatsApp integration.

```
┌─────────────────────────────────────────────────────────────┐
│                        User Interface                        │
├──────────────────────┬──────────────────────────────────────┤
│   Web App (Blazor)   │   WhatsApp Bot (Future)             │
│   - Admin Dashboard  │   - Customer Bookings                │
│   - Content Creation │   - Notifications                    │
│   - Analytics        │   - Rich Media Responses             │
└──────────────────────┴──────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                     ASP.NET Core API                         │
├─────────────────────────────────────────────────────────────┤
│  Controllers:                                                │
│  • Posts, Comments, Reactions (Social)                       │
│  • ResourceBookings (Photo Studio)                           │
│  • Users, Profiles, Followers                                │
│  • Search, Analytics, Notifications                          │
│  • DevAuth (Development Only)                                │
└─────────────────────────────────────────────────────────────┘
                               │
                ┌──────────────┼──────────────┐
                ▼              ▼              ▼
┌──────────────────┐ ┌─────────────────┐ ┌──────────────────┐
│   PostgreSQL     │ │   Keycloak      │ │  Azure Blob      │
│   (Database)     │ │   (Auth)        │ │  (Storage)       │
│                  │ │                 │ │                  │
│  • 22 Tables     │ │  • OAuth2/OIDC  │ │  • Images        │
│  • Users         │ │  • JWT Tokens   │ │  • Documents     │
│  • Profiles      │ │  • SSO          │ │  • Media         │
│  • Posts         │ │                 │ │                  │
│  • Bookings      │ │                 │ │  (Azurite dev)   │
└──────────────────┘ └─────────────────┘ └──────────────────┘
```

---

## 🗂️ Project Structure

```
SivarOs.Prototype/
├── Sivar.Os/                    # Main web application (Blazor Server)
│   ├── Components/              # Blazor components
│   │   ├── Pages/              # Page components
│   │   ├── Layout/             # Layout components
│   │   └── Shared/             # Shared components
│   ├── Controllers/            # API controllers
│   ├── Services/               # Business logic
│   ├── wwwroot/                # Static files
│   └── Program.cs              # App configuration
│
├── Sivar.Os.Client/            # Blazor WebAssembly client
│   ├── Components/             # Client-side components
│   ├── Services/               # Client services
│   └── Program.cs
│
├── Sivar.Os.Shared/            # Shared code (DTOs, entities)
│   ├── Entities/               # Database entities
│   ├── DTOs/                   # Data transfer objects
│   └── Enums/                  # Enumerations
│
├── Sivar.Os.Data/              # Data access layer
│   ├── Context/                # DbContext
│   ├── Migrations/             # EF Core migrations
│   └── Repositories/           # Data repositories
│
└── Xaf.Sivar.Os/               # DevExpress XAF admin (optional)
```

---

## 🔐 Authentication Flow

### Development Mode (Current)

```
User enters email
       │
       ▼
DevAuthController
       │
       ├─> Check if user exists
       │   ├─ Yes: Login existing
       │   └─ No: Create new user + profile
       │
       ▼
Set authentication cookie
       │
       ▼
Redirect to app
```

### Production Mode (Future)

```
User clicks "Login"
       │
       ▼
Redirect to Keycloak
       │
       ▼
User authenticates
       │
       ▼
Keycloak returns JWT
       │
       ▼
App validates token
       │
       ├─> User exists: Login
       └─> New user: Create profile
       │
       ▼
Set authentication cookie
       │
       ▼
App dashboard
```

---

## 🗄️ Database Schema (Key Tables)

### Users & Profiles
- **Sivar_Users** - Core user accounts (from Keycloak)
- **Sivar_Profiles** - User profiles (personal/business/etc.)
- **Sivar_ProfileTypes** - Profile type definitions
- **Sivar_ProfileFollowers** - Social connections

### Content
- **Sivar_Posts** - User posts/content
- **Sivar_PostAttachments** - Images, videos, documents
- **Sivar_Comments** - Post comments
- **Sivar_Reactions** - Likes, loves, etc.

### Booking System (Photo Studio)
- **Sivar_BookableResources** - Photo studios, equipment
- **Sivar_ResourceBookings** - Customer reservations
- **Sivar_BusinessContactInfos** - Business contact details

### Communication
- **Sivar_Conversations** - Message threads
- **Sivar_ChatMessages** - Direct messages
- **Sivar_Notifications** - System notifications

### Analytics
- **Sivar_Activities** - User activity tracking
- **Sivar_SearchResults** - Search history
- **Sivar_SavedResults** - Saved searches

---

## 🔄 Request Flow Example

### Creating a Post

```
User submits post form
       │
       ▼
Blazor component validates input
       │
       ▼
POST /api/Posts
       │
       ▼
PostsController.CreatePost()
       │
       ▼
IPostService.CreatePostAsync()
       │
       ├─> Validate user permissions
       ├─> Create Post entity
       ├─> Process attachments (Azure Blob)
       ├─> Save to database
       └─> Trigger notifications
       │
       ▼
Return PostDto
       │
       ▼
Update UI
```

---

## 🛠️ Technology Stack

### Backend
- **.NET 8 LTS** - Runtime
- **ASP.NET Core** - Web framework
- **Entity Framework Core** - ORM
- **Npgsql** - PostgreSQL provider
- **MudBlazor** - UI components

### Frontend
- **Blazor Server** - Main app
- **Blazor WebAssembly** - Client components
- **MudBlazor** - Material Design UI
- **JavaScript Interop** - Browser APIs

### Database
- **PostgreSQL 15+** - Primary database
- **Redis** - Cache (optional, fallback to memory)

### Authentication
- **Keycloak** - OAuth2/OIDC provider
- **ASP.NET Core Identity** - Cookie authentication

### Storage
- **Azure Blob Storage** - File storage
- **Azurite** - Local development emulator

### Infrastructure
- **Systemd** - Service management
- **Nginx** - Reverse proxy (future)
- **Let's Encrypt** - SSL certificates (future)

---

## 📊 Key Services

### IPostService
- Post CRUD operations
- Feed generation
- Visibility filtering
- Sentiment analysis

### IUserAuthenticationService
- User registration
- Profile management
- Role assignment

### IBookingService
- Resource availability
- Booking creation/cancellation
- Conflict detection

### INotificationService
- Push notifications
- Email notifications (future)
- WhatsApp notifications (future)

### ISearchService
- Full-text search
- Profile search
- Content search

---

## 🔌 API Design

### RESTful Principles
- Resource-based URLs (`/api/Posts/{id}`)
- HTTP verbs (GET, POST, PUT, DELETE)
- Status codes (200, 201, 400, 404, 500)
- JSON payloads

### Authentication
- Cookie-based sessions
- JWT tokens (Keycloak integration)
- CORS configured

### Pagination
```
GET /api/Posts/activity-feed?page=1&pageSize=10
```

### Filtering
```
GET /api/Search?query=photographer&type=profiles&page=1
```

---

## 🚀 Deployment Architecture

### Current Setup
```
Internet
    │
    ▼
[Port 5001] ──> Sivar.Os (Kestrel)
                    │
                    ├──> PostgreSQL (86.48.30.121)
                    ├──> Keycloak (86.48.30.137)
                    └──> Azure Blob (Azure)
```

### Production Setup (Planned)
```
Internet
    │
    ▼
[Port 80/443] ──> Nginx (SSL Termination)
                    │
                    ▼
                Sivar.Os (Kestrel :5001)
                    │
                    ├──> PostgreSQL (86.48.30.121)
                    ├──> Keycloak (86.48.30.137)
                    ├──> Redis Cache
                    └──> Azure Blob
```

---

## 📈 Scalability Considerations

### Current Limits
- Single instance (systemd service)
- In-memory cache (no Redis)
- Direct database connections

### Future Scaling
1. **Horizontal Scaling**
   - Multiple app instances behind load balancer
   - Redis for distributed cache
   - CDN for static assets

2. **Database Optimization**
   - Connection pooling (configured)
   - Read replicas
   - Indexed queries

3. **Caching Strategy**
   - Redis cache layer
   - CDN for media files
   - Application-level caching

4. **Background Jobs**
   - Hangfire for scheduled tasks
   - Message queue for async processing

---

## 🔒 Security Architecture

### Current Security
- ✅ Cookie-based authentication
- ✅ HTTPS redirects (configured)
- ✅ SQL injection protection (EF Core)
- ⚠️ Dev auth enabled (remove for production)

### Production Security
- [ ] WAF (Web Application Firewall)
- [ ] Rate limiting
- [ ] DDoS protection
- [ ] Security headers
- [ ] Content Security Policy
- [ ] Regular security audits

---

## 🧪 Testing Strategy

### Unit Tests
- Service layer logic
- Business rules validation
- Entity behavior

### Integration Tests
- API endpoints
- Database operations
- Authentication flows

### E2E Tests
- User workflows
- Booking process
- Payment integration (future)

---

## 📦 Module System (Future)

Sivar.Os is designed as a modular platform:

```
Module 1: Photo Studio Booking
Module 2: Social Network
Module 3: Business Directory
Module 4: Job Board
Module 5: Government Services
Module N: [Your Module Here]
```

Each module:
- Self-contained functionality
- Shared infrastructure
- Pluggable architecture
- Independent deployment

---

**Version:** 1.0.0-prototype  
**Last Updated:** 2026-02-17  
**Status:** MVP Development
