# Profile Switcher - Visual Design & Mockup

## 📐 Component Dimensions

```
┌─────────────────────────────────────────┐
│  Profile Switcher Container             │ Height: Auto
│  Width: 100% (Parent Container)         │
│                                         │
│  ┌─────────────────────────────────────┐│ Card: 64px height
│  │ [Avatar] Profile Name        [▼]  ││ Padding: 12px 16px
│  │          Profile Type             ││ Border: 1px solid
│  └─────────────────────────────────────┘│ Border-radius: 8px
│                                         │
│  (Dropdown below when expanded)         │
│                                         │
└─────────────────────────────────────────┘
```

---

## 🎨 Color Scheme

```
Primary Elements:
- Background: var(--mud-palette-surface) [White/Light Gray]
- Border: var(--mud-palette-lines-default) [Light Gray #E1E8ED]
- Text Primary: var(--mud-palette-text-primary) [Dark Gray #14171A]
- Text Secondary: var(--mud-palette-text-secondary) [Medium Gray #657786]
- Primary Color: var(--mud-palette-primary) [Blue #1DA1F2]

Hover States:
- Background Hover: var(--mud-palette-action-default-hover)
- Border on Hover: Primary Color (#1DA1F2)

Active States:
- Background: rgba(25, 118, 210, 0.05) [Light Blue]
- Border: Primary Color (#1DA1F2)

Shadows:
- Card Shadow: 0 4px 12px rgba(0, 0, 0, 0.1)
- Hover Shadow: 0 6px 16px rgba(0, 0, 0, 0.15)
```

---

## 🖼️ Component States

### State 1: Closed (Default)
```
┌─────────────────────────────────────────┐
│ [JO] Personal Profile           ▼       │
│      Personal                           │
└─────────────────────────────────────────┘

Avatar: 40x40px, Blue background, white text
Icon: Material Design chevron-down (20x20px)
Font: 
  - Name: 14px, 600 weight
  - Type: 12px, 400 weight, secondary color
```

### State 2: Opened (Expanded)
```
┌─────────────────────────────────────────┐
│ [JO] Personal Profile           ▲       │
│      Personal                           │
├─────────────────────────────────────────┤
│ [JO] ✓ Personal Profile                 │ ← Active
│      Personal                           │
├─────────────────────────────────────────┤
│ [BS]   Business Profile                 │ ← Can select
│      Business                           │
├─────────────────────────────────────────┤
│ [BR]   Brand Profile                    │ ← Can select
│      Brand                              │
├─────────────────────────────────────────┤
│ [CR]   Creator Profile                  │ ← Can select
│      Creator                            │
├─────────────────────────────────────────┤
│ + Create New Profile                    │ ← Button
└─────────────────────────────────────────┘

Profile Item: 60px height, padding 12px 16px
Checkmark: Green color, positioned right
Hover: Light blue background
Active: Light blue background + checkmark
```

### State 3: Modal (Creating Profile)
```
╔═════════════════════════════════════╗
║  Create New Profile         [✕]    ║ Modal Header
╠═════════════════════════════════════╣
║                                    ║
║  Profile Type:                     ║
║  ┌────────┬──────────┬──────┬────┐║
║  │👤 Pers │💼 Busi  │🏢 Bra│🎬Cr││ Type Grid
║  │Personal│Business │Brand │eato││
║  └────────┴──────────┴──────┴────┘║
║                                    ║
║  Profile Name *:                   ║
║  ┌──────────────────────────────┐ ║
║  │ e.g., My Business        [✓] │ ║ Input field
║  └──────────────────────────────┘ ║
║                                    ║
║  Description (Optional):           ║
║  ┌──────────────────────────────┐ ║
║  │ Tell us about this profile   │ ║ Textarea
║  │ _________________ (150/500)  │ ║ Char counter
║  └──────────────────────────────┘ ║
║                                    ║
║  Visibility:                       ║
║  ⦿ 🌍 Public - Everyone      ║ ║ Radio buttons
║  ○ 👥 Connections Only       ║ ║
║  ○ 🔒 Private - Only me      ║ ║
║                                    ║
║  ☑ Set as active profile          ║ ║ Checkbox
║                                    ║
╠═════════════════════════════════════╣
║  [Cancel]              [Create]    ║ ║ Footer buttons
╚═════════════════════════════════════╝
```

---

## 📏 Spacing & Layout

```
Modal Dimensions:
- Width: 500px (desktop)
- Max Width: 90% (mobile)
- Max Height: 90vh
- Border Radius: 12px

Internal Spacing:
- Header Padding: 24px
- Body Padding: 24px
- Footer Padding: 20px 24px
- Form Group Margin: 24px

Input Fields:
- Height: 40px (inputs)
- Padding: 10px 12px
- Border Radius: 6px
- Border Width: 1px
- Font Size: 14px

Avatar Elements:
- Active Profile Avatar: 40x40px
- Dropdown Item Avatar: 36x36px
- Font: Monospace, 600 weight, 12px

Buttons:
- Height: 40px
- Padding: 10px 24px
- Border Radius: 6px
- Font Weight: 600
- Font Size: 14px
- Minimum Width: 100px
```

---

## 🔤 Typography

```
Component:           Font            Weight  Size   Color
─────────────────────────────────────────────────────────
Active Profile       Roboto/System   600     14px   Primary
Profile Type Badge   Roboto/System   400     12px   Secondary
Modal Header         Roboto/System   700     20px   Primary
Form Labels          Roboto/System   600     14px   Primary
Input Fields         Roboto/System   400     14px   Primary
Form Helper Text     Roboto/System   400     12px   Secondary
Button Text          Roboto/System   600     14px   White/Primary
Character Counter    Roboto/System   400     12px   Secondary
```

---

## ✨ Animations & Transitions

```
Component               Animation              Duration   Easing
──────────────────────────────────────────────────────────────────
Dropdown Open/Close     Height fade-in/out    200ms      ease
Modal Overlay           Fade in                200ms      ease
Modal Content           Slide up + fade       300ms      ease
Hover States            Background shift      200ms      ease
Button Press            Scale + shadow        200ms      ease
Form Input Focus        Border + shadow color 200ms      ease
Dropdown Items Hover    Background fade      200ms      ease
Icon Rotation           Rotate transform      200ms      ease

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
```

---

## 📱 Responsive Breakpoints

### Desktop (>1200px)
```
┌──────────────────────────────────────────────┐
│ Header                                       │
├────────────┬────────────────┬────────────────┤
│  Sidebar   │  Main Feed     │ Profile Panel  │ ← Here
│  260px     │  1fr           │  300px         │
│            │                │ ┌─────────────┐│
│            │                │ │ProfileSwitch││
│            │                │ └─────────────┘│
│            │                │                │
│            │                │ ┌─────────────┐│
│            │                │ │WhoToFollow  ││
│            │                │ └─────────────┘│
└────────────┴────────────────┴────────────────┘
```

### Tablet (768px - 1200px)
```
┌──────────────────────────────┐
│ Header                       │
├────────────────┬─────────────┤
│  Main Feed     │ Profile Pan │ ← Sidebar hidden
│  1fr           │ 250px       │
│                │ ┌─────────┐ │
│                │ │ProfileSw│ │
│                │ └─────────┘ │
└────────────────┴─────────────┘
```

### Mobile (<768px)
```
┌──────────────────┐
│ Header           │
├──────────────────┤
│ Profile Switch   │ ← Full width
│ (collapsed)      │
├──────────────────┤
│  Main Feed       │
│  (full width)    │
└──────────────────┘
```

---

## 🎯 Interactive Elements

### Clickable Areas
```
Profile Card:
├─ Avatar (40x40px) - Clickable
├─ Profile Info - Clickable
└─ Dropdown Icon (20x20px) - Clickable

Dropdown Items:
├─ Profile Avatar (36x36px) - Clickable
├─ Profile Info - Clickable
└─ Checkmark Icon - Visual indicator only

Modal Buttons:
├─ Cancel - Minimum 44x44px (mobile touch target)
├─ Create - Minimum 44x44px
└─ Close (X) - 24x24px icon

Form Inputs:
├─ All inputs - Minimum 44px height
├─ Radio buttons - 20x20px
└─ Checkboxes - 20x20px
```

### Hover States
```
Profile Card:           Light gray background, blue border
Profile Item:           Light gray background
Type Selection:         Blue border, light blue background
Button (Primary):       Darker blue background
Button (Secondary):     Gray background
Checkbox/Radio:         Slightly larger scale
Input Focus:            Blue border, subtle shadow
```

---

## 🎨 Theme Support

The component uses CSS variables for theme support:

```css
:root {
  --mud-palette-surface: #ffffff;
  --mud-palette-background-gray: #f5f7fa;
  --mud-palette-lines-default: #e1e8ed;
  --mud-palette-text-primary: #14171a;
  --mud-palette-text-secondary: #657786;
  --mud-palette-primary: #1da1f2;
  --mud-palette-action-default-hover: #f0f4f8;
}

/* Dark mode would override these values */
@media (prefers-color-scheme: dark) {
  :root {
    --mud-palette-surface: #192734;
    --mud-palette-text-primary: #ffffff;
    /* ... etc ... */
  }
}
```

---

## 📊 Visual Hierarchy

```
Level 1 (Highest):  Modal Header (20px, 700 weight)
                    Form Labels (14px, 600 weight)
                    Active Profile (14px, 600 weight)

Level 2 (Medium):   Profile Type Labels (12px, 600 weight)
                    Input Text (14px, 400 weight)
                    Button Text (14px, 600 weight)

Level 3 (Lower):    Form Helper Text (12px, 400 weight)
                    Profile Type Description (12px, 400 weight)
                    Character Counter (12px, 400 weight)

Level 4 (Lowest):   Disabled Text (opacity 50%)
                    Placeholders (opacity 60%)
```

---

## 🔄 Dropdown Animation Flow

```
Closed State:
[Profile] ▼
  ↓ (Click)
Opening Animation (200ms):
  Opacity: 0 → 1
  Height: 0 → auto
  ↓
Open State:
[Profile] ▲
├─ [Item 1] ✓
├─ [Item 2]
└─ [Item 3]
  ↓ (Click Item)
Closing Animation (200ms):
  Opacity: 1 → 0
  Height: auto → 0
  ↓
Closed State (New Profile Selected)
[Profile] ▼
```

---

## 📋 Accessibility Features

```
Color Contrast:
✓ Text on Background: 4.5:1+ (WCAG AA)
✓ Buttons: 4.5:1+ contrast
✓ Not relying on color alone for info

Keyboard Navigation:
✓ Tab key cycles through items
✓ Enter to select/submit
✓ Escape to close dropdown/modal
✓ Focus visible on all interactive elements

Screen Reader:
✓ ARIA labels on buttons
✓ Form labels associated with inputs
✓ Modal role="dialog"
✓ Semantic HTML structure
```

---

## 🎯 Error States

### Validation Errors
```
Profile Name:
┌──────────────────────────────┐
│ [Error: must be 3+ chars]    │ ← Red border
│                              │ ← Red text below
└──────────────────────────────┘
Font: 12px, Color: #d32f2f, Weight: 400
```

### Loading States
```
Create Button (while submitting):
[Creating...] (disabled, opacity 0.5)

Profiles Loading:
Loading spinner in dropdown area
Gray out create button temporarily
```

---

## 🌈 Visual Consistency

All components follow these design principles:

1. **Rounded Corners:** 8px for cards, 6px for inputs, 20px for pills
2. **Shadows:** Used for depth and elevation
3. **Spacing:** Multiples of 4px (8px, 12px, 16px, 20px, 24px)
4. **Font Family:** System fonts (Roboto, -apple-system)
5. **Icons:** Material Design icons (24px, 20px, 16px)
6. **Colors:** Theme-based CSS variables

---

**Design System Version:** 1.0
**Last Updated:** October 28, 2025
**Status:** ✅ Complete & Documented
