# Services & Utilities Demo Data

## Overview

| Field | Value |
|-------|-------|
| **Status** | ✅ Complete |
| **Total Entries** | 20 |
| **Profiles Created** | 20 Service/Business Profiles |
| **Posts Created** | 20 BusinessLocation Posts |

## Categories Implemented

### Financial Services (4)
1. **Banco Agrícola Centro** - Largest bank, full banking services
2. **BAC Credomatic Escalón** - Regional bank, cards & loans
3. **Davivienda Paseo General Escalón** - Banking & mortgage services
4. **Western Union Centro** - International money transfers/remittances

### Telecommunications (2)
5. **Claro El Salvador Centro** - Mobile, internet, cable TV
6. **Tigo Money Escalón** - Mobile, fiber internet, mobile payments

### Healthcare (4)
7. **Farmacia San Nicolás 24 Horas** - 24/7 pharmacy
8. **Farmacias Económicas Centro** - Affordable medications
9. **Hospital de Diagnóstico Escalón** - Private hospital, 24/7 ER
10. **Laboratorio Clínico Max Bloch** - Clinical lab, diagnostics

### Professional Services (4)
11. **Notaría Lic. Roberto García** - Notary public services
12. **Bufete Jurídico Martínez & Asociados** - Law firm
18. **Contadores Públicos Hernández** - Accounting & auditing
20. **Seguros del Pacífico** - Insurance (life, auto, home, health)

### Retail (4)
13. **Super Selectos Escalón** - Supermarket chain
14. **Walmart Soyapango** - Hypermarket
15. **Freund Centro** - Hardware store & construction materials
19. **Ópticas La Curacao** - Optical store, eye exams

### Gas Stations (2)
16. **Gasolinera Puma Escalón** - Gas station & convenience
17. **Shell Santa Elena** - Gas station with V-Power fuel

## ID Patterns

| Type | Pattern |
|------|---------|
| Profile IDs | `i0000001-0001-0001-0001-000000000001` through `i0000020-...` |
| Post IDs | `j0000001-0001-0001-0001-000000000001` through `j0000020-...` |
| CreatedById | `dddddddd-dddd-dddd-dddd-dddddddddddd` (Seeder User) |

## Sample AI Queries Supported

### Banking & Money
- "¿Dónde hay un banco cerca?"
- "Horario del Banco Agrícola"
- "Where can I receive remittances?"
- "BAC Credomatic location"

### Healthcare
- "Farmacias abiertas 24 horas"
- "¿Dónde puedo hacerme un examen de sangre?"
- "Hospital de emergencia más cercano"
- "24 hour pharmacy near me"

### Utilities & Telecom
- "¿Dónde puedo pagar mi teléfono Claro?"
- "Tigo store near Escalón"
- "Internet providers in San Salvador"

### Professional Services
- "Necesito un notario"
- "Law firm for business matters"
- "Accountant for tax declaration"
- "Insurance companies in El Salvador"

### Retail & Gas
- "Supermercado más cercano"
- "Hardware store for construction materials"
- "Gas stations open 24 hours"
- "Where to buy groceries"

## Implementation Notes

- All profiles set to `IsActive = true`
- BusinessLocationType varies by category:
  - Banks: `CustomerBranch` (2)
  - Telecom: `ServiceCenter` (6)
  - Pharmacies/Retail: `RetailStore` (4)
  - Hospitals/Labs: `MainOffice` (1)
  - Professional: `AdministrativeOffice` (3)
- Working hours included in SpecialInstructions field
- GPS coordinates for map integration
- Contact info (phone, email, website) for each location

## JSON File

`services.json` - Contains 20 profiles and 20 posts
