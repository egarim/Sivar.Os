# Sivar.Os - Business Services Module

Part of the Sivar.Os platform - an operating system for modern countries.

This module provides booking infrastructure for service businesses (photo studios, salons, restaurants) with WhatsApp AI assistant integration.

## Architecture

- **PhotoBooking.Web** - Blazor Server admin UI (MudBlazor)
- **PhotoBooking.Api** - ASP.NET Core Web API
- **PhotoBooking.Data** - EF Core data layer (Postgres)
- **PhotoBooking.Shared** - Shared models and DTOs

## Tech Stack

- .NET 9
- Blazor Server with MudBlazor
- PostgreSQL with EF Core
- Keycloak (OIDC authentication)
- Cloudinary (image storage)
- WhatsApp Business API (via OpenClaw)

## Localization

Supports English and Spanish. Spanish is the primary language for the admin UI.

## Setup

1. Install .NET 9 SDK
2. Set up PostgreSQL database
3. Configure Keycloak realm
4. Set up Cloudinary account
5. Update `appsettings.json` with your credentials

```bash
dotnet restore
dotnet ef database update --project PhotoBooking.Data
dotnet run --project PhotoBooking.Web
```

## Environment Variables

```
ConnectionStrings__DefaultConnection=Host=localhost;Database=photobooking;Username=postgres;Password=yourpassword
Keycloak__Authority=https://your-keycloak.com/realms/photobooking
Keycloak__ClientId=photobooking-web
Keycloak__ClientSecret=your-secret
Cloudinary__CloudName=your-cloud
Cloudinary__ApiKey=your-key
Cloudinary__ApiSecret=your-secret
```

## Development

Created: 2026-02-17
MVP Target: 3-4 weeks
First Customer: Brother's photo studio (El Salvador)
