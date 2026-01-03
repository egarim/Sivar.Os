# Issue: Explore Page Sidebar Auth Navigation Mismatch

## Summary

On the Explore page (`/app/explore`), the **Sign In/Sign Up buttons in the sidebar** navigate to different destinations than the **buttons in the "Join Sivar.Os Today" banner**. They should both navigate to the same authentication endpoints.

## Current Behavior

### Sidebar (NavMenu.razor)
- **Login** → `/` (home page, force load)
- **Sign Up** → `/?action=signup` (home page with action parameter)

Location: [NavMenu.razor#L655-L657](../Sivar.Os.Client/Layout/NavMenu.razor)
```csharp
private void Login() => Nav.NavigateTo("/", forceLoad: true);
private void SignUp() => Nav.NavigateTo("/?action=signup", forceLoad: true);
```

### Banner (JoinCta.razor)
- **Login** → `/authentication/login?returnUrl={currentUrl}`
- **Sign Up** → `/authentication/register?returnUrl={currentUrl}`

Location: [JoinCta.razor#L168-L177](../Sivar.Os.Client/Components/Shared/JoinCta.razor)
```csharp
private void NavigateToSignUp()
{
    var returnUrl = Uri.EscapeDataString(GetReturnUrl());
    Navigation.NavigateTo($"/authentication/register?returnUrl={returnUrl}", forceLoad: true);
}

private void NavigateToLogin()
{
    var returnUrl = Uri.EscapeDataString(GetReturnUrl());
    Navigation.NavigateTo($"/authentication/login?returnUrl={returnUrl}", forceLoad: true);
}
```

## Expected Behavior

Both the sidebar buttons and the banner buttons should navigate to the same authentication endpoints:
- **Login** → `/authentication/login?returnUrl={currentUrl}`
- **Sign Up** → `/authentication/register?returnUrl={currentUrl}`

This ensures:
1. Consistent user experience regardless of which button is clicked
2. Users are returned to the Explore page after authentication
3. Proper Keycloak authentication flow is triggered

## Files to Modify

1. **[NavMenu.razor](../Sivar.Os.Client/Layout/NavMenu.razor)** - Update `Login()` and `SignUp()` methods to use the same navigation pattern as `JoinCta.razor`

## Proposed Solution

Update `NavMenu.razor` (around line 655-657):

```csharp
private void Login()
{
    var returnUrl = Uri.EscapeDataString(GetCurrentPath());
    Nav.NavigateTo($"/authentication/login?returnUrl={returnUrl}", forceLoad: true);
}

private void SignUp()
{
    var returnUrl = Uri.EscapeDataString(GetCurrentPath());
    Nav.NavigateTo($"/authentication/register?returnUrl={returnUrl}", forceLoad: true);
}

private string GetCurrentPath()
{
    var uri = new Uri(Nav.Uri);
    return uri.PathAndQuery;
}
```

## Priority

Low - UX consistency improvement

## Labels

- `enhancement`
- `ui`
- `explore-page`
- `authentication`
