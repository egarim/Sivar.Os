# Government Demo Data

## Overview

| Field | Value |
|-------|-------|
| **Status** | ✅ Complete |
| **Total Entries** | 15 |
| **Profiles Created** | 15 Government Organization Profiles |
| **Posts Created** | 15 BusinessLocation Posts |

## Categories Implemented

### Municipal Offices (3)
1. **Alcaldía de San Salvador** - Main municipal office for capital city
2. **Alcaldía de Santa Ana** - Municipal services for Santa Ana
3. **Alcaldía de San Miguel** - Municipal services for San Miguel

### Central Government (4)
4. **Casa Presidencial** - Presidential house and executive branch
5. **Ministerio de Hacienda** - Treasury/Finance ministry, tax services
6. **Ministerio de Educación** - Education ministry
7. **Ministerio de Salud** - Health ministry

### Justice System (2)
8. **Corte Suprema de Justicia** - Supreme Court
9. **Fiscalía General de la República** - Attorney General's office

### Civil Registry & ID (2)
10. **Centro DUI San Salvador** - National ID card center
11. **Registro Civil Central** - Civil registry for birth/marriage/death certificates

### Social Services (2)
12. **ISSS Central** - Social Security Institute (main office)
13. **AFP Crecer** - Pension fund administrator

### Utilities (2)
14. **ANDA Oficinas Centrales** - National water authority
15. **CAESS Centro de Servicio** - Electricity company service center

## ID Patterns

| Type | Pattern |
|------|---------|
| Profile IDs | `g0000001-0001-0001-0001-000000000001` through `g0000015-...` |
| Post IDs | `h0000001-0001-0001-0001-000000000001` through `h0000015-...` |
| CreatedById | `dddddddd-dddd-dddd-dddd-dddddddddddd` (Seeder User) |

## Sample AI Queries Supported

- "¿Cómo obtengo mi DUI?"
- "¿Dónde está la alcaldía más cercana?"
- "Horario del Ministerio de Hacienda"
- "¿Dónde puedo sacar un certificado de nacimiento?"
- "Oficinas del ISSS en San Salvador"
- "¿Dónde pago la luz?"
- "Government offices near Centro"
- "How do I get a birth certificate?"

## Implementation Notes

- All profiles set to `IsActive = true`
- BusinessLocationType defaults to `MainOffice` for government entities
- Working hours reflect typical government schedule (8am-4pm Mon-Fri)
- Phone numbers use 2XXX format (landlines) for government offices
- All locations include GPS coordinates for map integration

## Procedure Post Structure

Service posts will include:
- **Requirements**: Documents needed
- **Process**: Step-by-step instructions
- **Duration**: Estimated time
- **Fees**: Cost in USD
- **Tips**: Best times to go, what to expect

## JSON File

`government.json` - To be generated
