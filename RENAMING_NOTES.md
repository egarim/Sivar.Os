# Project Renaming Guide

The project has been renamed from PhotoBooking to Sivar.Os to reflect the platform vision.

## What Changed

- Solution name: `PhotoBooking.sln` → `Sivar.Os.sln`
- Database name: `photobooking` → `sivaros`
- Keycloak realm references updated
- App title in UI updated to "Sivar.Os"

## What You Need to Do

The folder structure still has the old "PhotoBooking.*" folder names. When you open the project on your machine, you should rename:

```
PhotoBooking.Web/     → Sivar.Os.Web/
PhotoBooking.Api/     → Sivar.Os.Api/
PhotoBooking.Data/    → Sivar.Os.Data/
PhotoBooking.Shared/  → Sivar.Os.Shared/
```

Then update all namespace references in the .cs files from `PhotoBooking.*` to `Sivar.Os.*`

**Or** just use it as-is (folder names don't matter much) and rename when you refactor later.

The important thing: the platform identity is **Sivar.Os**, not "PhotoBooking."
