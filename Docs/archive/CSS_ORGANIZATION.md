# CSS Organization Documentation

## Overview

This document describes the CSS architecture for Sivar.Os to prevent future CSS cleanup accidents and improve maintainability.

## Problem Addressed

Previously, `Home.razor` contained **1,586 lines** of inline CSS within a `<style>` block. This caused several issues:

- **Hard to maintain**: CSS scattered across markup made it difficult to track changes
- **Accidental deletions**: Removing "unused" CSS from component files broke the UI because inline styles took precedence
- **No reusability**: CSS couldn't be shared across pages
- **Poor performance**: Inline CSS isn't cached by browsers
- **Confusion**: Mixing scoped component CSS (`.razor.css`) with inline page styles created ambiguity

## New Architecture

All CSS has been extracted into modular files located in `wwwroot/css/`:

### File Structure

```
wwwroot/css/
├── app.css                      # Main entry point (imports all other CSS)
├── wireframe-theme.css          # CSS variables and theme configuration
├── wireframe-layout.css         # Grid layouts and responsive breakpoints
├── wireframe-components.css     # All UI component styles
└── wireframe-animations.css     # Keyframe animations and transitions
```

### File Responsibilities

#### 1. **app.css** - Main Entry Point
- Imports all other CSS files using `@import`
- Contains MudBlazor overrides (`.mud-main-content`, `.mud-scroll-to-top`)
- **Purpose**: Single file to include in `App.razor`

#### 2. **wireframe-theme.css** - Theme Variables
- Defines CSS custom properties (variables) for the wireframe theme
- Maps to MudBlazor's CSS variables for automatic theme adaptation
- Variables include:
  - `--wire-bg`: Background color
  - `--wire-surface`: Surface/card background
  - `--wire-border`: Border colors
  - `--wire-text-primary`: Primary text color
  - `--wire-text-secondary`: Secondary text color
  - `--wire-primary`: Primary brand color
  - `--wire-hover`: Hover state background

#### 3. **wireframe-layout.css** - Layout System
- Grid and flexbox layouts
- Container styles
- Responsive breakpoints (`@media` queries)
- Mobile-specific adjustments

#### 4. **wireframe-components.css** - Component Styles
- All component-specific styles (1,400+ lines)
- Organized by component:
  - Header
  - Sidebar & Stats Panel
  - User Cards
  - Buttons
  - Feed
  - Post Composer
  - Post Cards
  - Reactions
  - Comments
  - Pagination
  - AI Chat (panel, history, messages, FAB)
  - Empty States

#### 5. **wireframe-animations.css** - Animations
- `@keyframes` definitions
- Animation configurations
- Includes:
  - `slideIn`: Message slide-in effect
  - `typing-dot`: Typing indicator animation

## Usage

### Including CSS in Your App

CSS is automatically loaded via `Components/App.razor`:

```html
<link href="css/app.css" rel="stylesheet" />
```

This single line loads all modular CSS files through `@import` statements in `app.css`.

### Component-Scoped CSS (.razor.css)

Component-scoped `.razor.css` files should **only** be used for:
- Styles truly unique to a single component instance
- Component-specific overrides that don't fit the global theme

**Example**: If a component needs a unique hover effect that no other component uses, it belongs in the component's `.razor.css` file.

### When to Add New CSS

| Scenario | File to Update |
|----------|----------------|
| New theme color or variable | `wireframe-theme.css` |
| New grid layout or responsive breakpoint | `wireframe-layout.css` |
| New component styling | `wireframe-components.css` |
| New animation or transition | `wireframe-animations.css` |
| MudBlazor override | `app.css` |
| Component-specific unique style | `ComponentName.razor.css` |

## Benefits

✅ **Separation of Concerns**: CSS is organized by responsibility  
✅ **Reusability**: CSS can be shared across multiple pages  
✅ **Maintainability**: Easy to locate and modify specific styles  
✅ **Performance**: CSS files are cached by browsers  
✅ **Prevention**: Can't accidentally delete "unused" CSS anymore  
✅ **Clarity**: Clear distinction between global and scoped styles  

## Migration Summary

**Before:**
- `Home.razor`: 3,127 lines (including 1,586 lines of inline CSS)
- CSS scattered in `<style>` blocks and component `.razor.css` files
- No clear organization

**After:**
- `Home.razor`: 1,541 lines (markup only, no inline CSS)
- `app.css`: 40 lines (main entry point)
- `wireframe-theme.css`: 17 lines (theme variables)
- `wireframe-layout.css`: 73 lines (layouts and responsive design)
- `wireframe-components.css`: 1,411 lines (all component styles)
- `wireframe-animations.css`: 60 lines (animations)

**Total CSS**: 1,601 lines (organized across 5 files)

## Best Practices

1. **Never add inline `<style>` blocks** in `.razor` files
2. **Use CSS variables** from `wireframe-theme.css` for colors and spacing
3. **Keep component styles together** - if you add a new component, add its styles to `wireframe-components.css` in the appropriate section
4. **Add comments** to mark component sections in `wireframe-components.css`
5. **Test responsive** changes using browser dev tools at different breakpoints
6. **Use semantic class names** that describe purpose, not appearance

## Troubleshooting

### Styles not applying?
1. Check browser console for 404 errors on CSS files
2. Verify `<link href="css/app.css" rel="stylesheet" />` exists in `App.razor`
3. Hard refresh browser (Ctrl+F5) to clear cached CSS

### Responsive design broken?
1. Check `wireframe-layout.css` for media query breakpoints
2. Verify viewport meta tag in `App.razor`: `<meta name="viewport" content="width=device-width, initial-scale=1.0" />`

### Theme colors wrong?
1. Check CSS variable definitions in `wireframe-theme.css`
2. Verify MudBlazor theme is configured in `Program.cs`

## Future Enhancements

- Consider using CSS preprocessor (SASS/LESS) for variables and nesting
- Add CSS minification in production builds
- Implement CSS purging to remove unused styles
- Create theme switcher for light/dark modes using CSS variables

---

**Last Updated**: October 29, 2025  
**Version**: 1.0  
**Maintained by**: Development Team
