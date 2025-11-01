# 🌍 Multi-Language Localization Implementation Plan

## Executive Summary

**Project**: Sivar.Os Multi-Language Support  
**Version**: 1.0  
**Date**: November 1, 2025  
**Status**: Planning Phase

### Objective
Implement comprehensive multi-language localization in Sivar.Os Blazor WebAssembly application with priority-based culture resolution.

### Culture Resolution Priority
1. **Profile Settings** - User's explicit saved preference in database
2. **Browser Language** - Auto-detected from browser settings
3. **Default Language** - en-US (English, United States)

### Target Languages
- **Phase 1**: English (en-US) - Default
- **Phase 1**: Spanish (es-ES) - Primary secondary language
- **Future**: Framework ready for additional languages

### Key Features
- ✅ Runtime language switching without logout
- ✅ Persistent preference across devices (for authenticated users)
- ✅ Browser language detection for anonymous users
- ✅ MudBlazor component localization
- ✅ Date, time, and number formatting per culture
- ✅ Centralized resource file management

### Estimated Timeline
- **Total Duration**: 8-12 weeks (4-6 sprints)
- **Total Effort**: 168-232 hours
- **Team Size**: 1-2 developers + 1 translator/reviewer

---

## 📊 Project Metrics

| Metric | Target | Status |
|--------|--------|--------|
| **Supported Languages** | 2 (en-US, es-ES) | ⬜ 0/2 |
| **Components Translated** | 30+ | ⬜ 0/30 |
| **Pages Translated** | 9 | ⬜ 0/9 |
| **Resource Files Created** | 60+ (30 components × 2 languages) | ⬜ 0/60 |
| **Startup Time Impact** | < 200ms | ⬜ Not Measured |
| **Language Switch Time** | < 3 seconds | ⬜ Not Measured |
| **Code Coverage** | > 80% for new code | ⬜ Not Measured |

---

## 🎯 Success Criteria

### Functional Requirements
- [ ] System supports English and Spanish languages
- [ ] Culture priority works: Profile > Browser > Default
- [ ] Authenticated users can set and save language preference
- [ ] Anonymous users automatically use browser language
- [ ] Language preference persists across sessions
- [ ] Language preference synchronizes across devices
- [ ] All user-facing text is localized (no hardcoded strings)
- [ ] MudBlazor components display in selected language
- [ ] Date, time, and number formatting respects culture
- [ ] Users can change language at runtime
- [ ] Page reloads automatically after language change
- [ ] Error messages are localized

### Non-Functional Requirements
- [ ] App startup time increases by less than 200ms
- [ ] Language switch completes in less than 3 seconds
- [ ] Resource files add less than 500KB to application size
- [ ] No memory leaks from localization system
- [ ] Works in all supported browsers (Chrome, Firefox, Edge, Safari)
- [ ] Works on mobile devices (iOS, Android)
- [ ] Accessible (screen readers compatible)
- [ ] Graceful degradation if culture resolution fails

### Technical Requirements
- [ ] Database migration runs without errors
- [ ] No breaking changes to existing APIs
- [ ] All code follows project conventions
- [ ] All public methods have XML documentation
- [ ] All controllers have comprehensive logging
- [ ] All exceptions handled gracefully
- [ ] No compiler warnings
- [ ] Backward compatible with existing profiles

---

## 📁 Project Structure

### New Files to Create
```
Sivar.Os.Client/
├── Services/
│   ├── ICultureService.cs
│   └── CultureService.cs
├── Resources/
│   ├── Pages/
│   │   ├── Login.resx
│   │   ├── Login.es.resx
│   │   ├── SignUp.resx
│   │   ├── SignUp.es.resx
│   │   ├── Home.resx
│   │   ├── Home.es.resx
│   │   ├── ProfilePage.resx
│   │   └── ProfilePage.es.resx
│   ├── Components/
│   │   ├── Feed/
│   │   │   ├── PostCard.resx
│   │   │   ├── PostCard.es.resx
│   │   │   ├── PostComposer.resx
│   │   │   ├── PostComposer.es.resx
│   │   │   └── [... other feed components]
│   │   ├── Profile/
│   │   │   ├── ProfileCard.resx
│   │   │   ├── ProfileCard.es.resx
│   │   │   ├── ProfileLocationEditor.resx
│   │   │   ├── ProfileLocationEditor.es.resx
│   │   │   └── [... other profile components]
│   │   ├── Layout/
│   │   │   ├── NavMenu.resx
│   │   │   └── NavMenu.es.resx
│   │   └── Shared/
│   │       ├── LanguageSelector.resx
│   │       ├── LanguageSelector.es.resx
│   │       ├── DeleteConfirmationDialog.resx
│   │       └── DeleteConfirmationDialog.es.resx
│   ├── Common.resx
│   └── Common.es.resx
└── Components/
    ├── Shared/
    │   └── LanguageSelector.razor
    └── Profile/
        └── ProfileLanguageSettings.razor

Database/
└── Scripts/
    └── 004_AddPreferredLanguageToProfile.sql

Docs/
├── LOCALIZATION_GUIDE.md
└── MULTI_LANGUAGE_LOCALIZATION_PLAN.md (this file)
```

### Files to Modify
```
Sivar.Os.Client/
├── Program.cs (add localization services)
├── Sivar.Os.Client.csproj (add NuGet packages)
└── [All .razor components for translation]

Sivar.Os.Shared/
├── Entities/Profile.cs (add PreferredLanguage property)
├── DTOs/ProfileDto.cs (add PreferredLanguage to all DTOs)
└── Clients/IProfilesClient.cs (add language methods)

Sivar.Os/
├── Controllers/ProfilesController.cs (add language endpoint)
└── Services/ProfileService.cs (add language update method)
```

---

## 🚀 PHASE 1: Database & Backend Infrastructure

**Duration**: 1-2 weeks  
**Priority**: CRITICAL (blocking for all other phases)  
**Team**: Backend Developer

### Overview
Establish the database schema and backend API support for storing and retrieving user language preferences.

---

### Task 1.1: Database Schema Update

**Assignee**: Database Administrator / Backend Developer  
**Estimated Time**: 2-4 hours  
**Priority**: P0 - Critical

#### Description
Add `PreferredLanguage` column to the `Profiles` table to store user's language preference.

#### Files to Create
- `Database/Scripts/004_AddPreferredLanguageToProfile.sql`

#### SQL Script
```sql
-- Migration: Add PreferredLanguage to Profiles table
-- Date: 2025-11-01
-- Description: Adds user language preference support

-- Add preferred language column
ALTER TABLE "Profiles" 
ADD COLUMN "PreferredLanguage" VARCHAR(10) NULL;

-- Add column comment for documentation
COMMENT ON COLUMN "Profiles"."PreferredLanguage" IS 
'User preferred UI language (e.g., en-US, es-ES). NULL falls back to browser language.';

-- Create index for filtering queries (only index non-null values)
CREATE INDEX "IX_Profiles_PreferredLanguage" 
ON "Profiles"("PreferredLanguage") 
WHERE "PreferredLanguage" IS NOT NULL;

-- Optional: Set default for testing (commented out for production)
-- UPDATE "Profiles" SET "PreferredLanguage" = 'en-US' WHERE "PreferredLanguage" IS NULL;

-- Verify changes
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_name = 'Profiles' AND column_name = 'PreferredLanguage';
```

#### Acceptance Criteria
- [x] Column `PreferredLanguage` exists in `Profiles` table
- [x] Column is VARCHAR(10) type
- [x] Column accepts NULL values
- [x] Column has descriptive comment
- [x] Index `IX_Profiles_PreferredLanguage` created successfully
- [x] Existing profile records remain unchanged (all NULL)
- [x] Migration script runs without errors on clean database
- [x] Migration script is idempotent (safe to run multiple times)
- [x] Rollback script tested and documented

#### Testing Steps
1. Run migration on local development database
2. Verify column added: `SELECT * FROM information_schema.columns WHERE table_name = 'Profiles'`
3. Test INSERT with NULL value
4. Test INSERT with valid culture code ('en-US')
5. Test INSERT with invalid value (should allow, validation in app layer)
6. Verify index exists: `SELECT * FROM pg_indexes WHERE tablename = 'Profiles'`
7. Test rollback: `ALTER TABLE "Profiles" DROP COLUMN "PreferredLanguage"`

#### Rollback Plan
```sql
-- Rollback script if needed
DROP INDEX IF EXISTS "IX_Profiles_PreferredLanguage";
ALTER TABLE "Profiles" DROP COLUMN IF EXISTS "PreferredLanguage";
```

---

### Task 1.2: Update Profile Entity

**Assignee**: Backend Developer  
**Estimated Time**: 1-2 hours  
**Priority**: P0 - Critical

#### Description
Add `PreferredLanguage` property to the Profile entity class with appropriate validation.

#### Files to Modify
- `Sivar.Os.Shared/Entities/Profile.cs`

#### Code Changes
Add the following property to the `Profile` class (around line 50, after `ContactPhone`):

```csharp
/// <summary>
/// User's preferred UI language (e.g., "en-US", "es-ES")
/// If null, the system will use browser language as fallback
/// </summary>
[StringLength(10, ErrorMessage = "Preferred language code cannot exceed 10 characters")]
[RegularExpression(@"^[a-z]{2}-[A-Z]{2}$", 
    ErrorMessage = "Preferred language must be in format 'xx-XX' (e.g., 'en-US', 'es-ES')")]
public virtual string? PreferredLanguage { get; set; }
```

#### Acceptance Criteria
- [x] Property added to `Profile` entity
- [x] Property is nullable (`string?`)
- [x] Property is virtual (for EF Core proxies)
- [x] StringLength validation set to 10 characters
- [x] RegEx validation matches culture format (e.g., "en-US")
- [x] XML documentation comments added
- [x] Entity Framework Core recognizes property in model
- [x] No breaking changes to existing functionality
- [x] Project compiles without errors or warnings

#### Testing Steps
1. Build solution and verify no compilation errors
2. Run EF Core migration generator to verify property detected
3. Create new Profile instance and set PreferredLanguage
4. Verify validation triggers for invalid formats:
   - "english" (should fail - not culture code)
   - "EN-US" (should fail - wrong case)
   - "en-us" (should fail - wrong case)
   - "en-USA" (should fail - too long)
5. Verify validation passes for valid formats:
   - "en-US" (should pass)
   - "es-ES" (should pass)
   - null (should pass)

---

### Task 1.3: Update Profile DTOs

**Assignee**: Backend Developer  
**Estimated Time**: 2-3 hours  
**Priority**: P0 - Critical

#### Description
Add `PreferredLanguage` property to all Profile-related DTOs for API communication.

#### Files to Modify
- `Sivar.Os.Shared/DTOs/ProfileDto.cs`

#### Code Changes

**1. ProfileDto** (around line 80):
```csharp
/// <summary>
/// User's preferred UI language (e.g., "en-US", "es-ES")
/// If null, falls back to browser language
/// </summary>
public string? PreferredLanguage { get; set; }
```

**2. CreateProfileDto** (around line 140):
```csharp
/// <summary>
/// User's preferred UI language (optional, can be set later)
/// </summary>
public string? PreferredLanguage { get; set; }
```

**3. UpdateProfileDto** (around line 180):
```csharp
/// <summary>
/// User's preferred UI language
/// </summary>
public string? PreferredLanguage { get; set; }
```

**4. CreateAnyProfileDto** (around line 340):
```csharp
/// <summary>
/// User's preferred UI language
/// </summary>
public string? PreferredLanguage { get; set; }
```

#### Acceptance Criteria
- [x] `PreferredLanguage` added to `ProfileDto`
- [x] `PreferredLanguage` added to `CreateProfileDto`
- [x] `PreferredLanguage` added to `UpdateProfileDto`
- [x] `PreferredLanguage` added to `CreateAnyProfileDto`
- [x] All properties are nullable (`string?`)
- [x] XML documentation added to all properties
- [x] DTOs compile without errors
- [x] Serialization/deserialization works correctly
- [x] AutoMapper mappings updated (if used)

#### Testing Steps
1. Build solution
2. Serialize ProfileDto to JSON and verify PreferredLanguage included
3. Deserialize JSON with PreferredLanguage and verify property set
4. Test with null value
5. Test AutoMapper Profile entity → ProfileDto mapping
6. Test AutoMapper UpdateProfileDto → Profile entity mapping

---

### Task 1.4: Add Profile Service Method

**Assignee**: Backend Developer  
**Estimated Time**: 2-3 hours  
**Priority**: P0 - Critical

#### Description
Add business logic method to update user's language preference in the ProfileService.

#### Files to Modify
- `Sivar.Os.Shared/Services/IProfileService.cs`
- `Sivar.Os/Services/ProfileService.cs`

#### Code Changes

**IProfileService.cs** - Add interface method:
```csharp
/// <summary>
/// Updates the preferred language for the user's current profile
/// </summary>
/// <param name="keycloakId">User's Keycloak ID</param>
/// <param name="language">Culture code (e.g., "en-US") or null to clear preference</param>
/// <exception cref="KeyNotFoundException">Profile not found</exception>
Task UpdatePreferredLanguageAsync(string keycloakId, string? language);
```

**ProfileService.cs** - Add implementation:
```csharp
public async Task UpdatePreferredLanguageAsync(string keycloakId, string? language)
{
    _logger.LogInformation(
        "Updating preferred language for user {KeycloakId} to {Language}",
        keycloakId, language ?? "NULL");

    // Get user ID from Keycloak ID
    var userId = await GetUserIdByKeycloakIdAsync(keycloakId);
    
    // Get user's active profile
    var profile = await _profileRepository.GetActiveProfileByUserIdAsync(userId);
    
    if (profile == null)
    {
        _logger.LogWarning(
            "No active profile found for user {UserId} (Keycloak: {KeycloakId})",
            userId, keycloakId);
        throw new KeyNotFoundException($"No active profile found for user {userId}");
    }
    
    // Update language preference
    profile.PreferredLanguage = language;
    profile.UpdatedAt = DateTime.UtcNow;
    
    await _profileRepository.UpdateAsync(profile);
    
    _logger.LogInformation(
        "Successfully updated language preference for profile {ProfileId} to {Language}",
        profile.Id, language ?? "NULL");
}
```

#### Acceptance Criteria
- [x] Method added to IProfileService interface
- [x] Method implemented in ProfileService
- [x] Updates `PreferredLanguage` property
- [x] Updates `UpdatedAt` timestamp
- [x] Throws `KeyNotFoundException` if profile not found
- [x] Handles NULL language (clears preference)
- [x] Saves changes to database via repository
- [x] Comprehensive logging for debugging
- [x] No side effects on other profile properties

#### Testing Steps
1. Unit test: Update language to "es-ES"
2. Unit test: Update language to null
3. Unit test: User with no profile throws exception
4. Integration test: Verify database updated
5. Integration test: Verify UpdatedAt timestamp changed
6. Verify logging output contains expected messages

---

### Task 1.5: Add Profile Controller Endpoint

**Assignee**: Backend Developer  
**Estimated Time**: 3-4 hours  
**Priority**: P0 - Critical

#### Description
Create REST API endpoint for updating user's language preference.

#### Files to Modify
- `Sivar.Os/Controllers/ProfilesController.cs`

#### Code to Add
Add this endpoint to ProfilesController (around line 1100, near end of file):

```csharp
/// <summary>
/// Updates the preferred language for the current user's profile
/// </summary>
/// <param name="request">Language update request containing the culture code</param>
/// <returns>No content on success</returns>
/// <response code="204">Language preference updated successfully</response>
/// <response code="400">Invalid culture code provided</response>
/// <response code="401">User not authenticated</response>
/// <response code="404">User profile not found</response>
/// <response code="500">Internal server error</response>
[HttpPut("current/language")]
[Authorize]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> UpdatePreferredLanguage(
    [FromBody] UpdateLanguageRequest request)
{
    var requestId = Guid.NewGuid();
    _logger.LogInformation(
        "[ProfilesController.UpdatePreferredLanguage] START - RequestId={RequestId}, Language={Language}",
        requestId, request?.Language ?? "NULL");

    try
    {
        // Get authenticated user
        var keycloakId = GetKeycloakIdFromRequest();
        if (string.IsNullOrEmpty(keycloakId))
        {
            _logger.LogWarning(
                "[ProfilesController.UpdatePreferredLanguage] UNAUTHORIZED - No KeycloakId, RequestId={RequestId}",
                requestId);
            return Unauthorized("User not authenticated");
        }

        // Validate culture code if provided (null is valid for clearing preference)
        if (!string.IsNullOrWhiteSpace(request?.Language))
        {
            if (!IsValidCultureCode(request.Language))
            {
                _logger.LogWarning(
                    "[ProfilesController.UpdatePreferredLanguage] BAD_REQUEST - Invalid culture: {Culture}, RequestId={RequestId}",
                    request.Language, requestId);
                return BadRequest(new 
                { 
                    error = "InvalidCultureCode",
                    message = $"Invalid culture code: {request.Language}. Expected format: 'xx-XX' (e.g., 'en-US', 'es-ES')"
                });
            }
        }

        // Update preference
        await _profileService.UpdatePreferredLanguageAsync(keycloakId, request?.Language);

        _logger.LogInformation(
            "[ProfilesController.UpdatePreferredLanguage] SUCCESS - RequestId={RequestId}, Language={Language}",
            requestId, request?.Language ?? "NULL");

        return NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        _logger.LogWarning(ex,
            "[ProfilesController.UpdatePreferredLanguage] NOT_FOUND - RequestId={RequestId}",
            requestId);
        return NotFound(new 
        { 
            error = "ProfileNotFound",
            message = "No active profile found for the current user"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "[ProfilesController.UpdatePreferredLanguage] ERROR - RequestId={RequestId}",
            requestId);
        return StatusCode(500, new 
        { 
            error = "InternalServerError",
            message = "An error occurred while updating language preference"
        });
    }
}

/// <summary>
/// Validates if a string is a valid culture code
/// </summary>
/// <param name="cultureCode">The culture code to validate</param>
/// <returns>True if valid, false otherwise</returns>
private bool IsValidCultureCode(string cultureCode)
{
    if (string.IsNullOrWhiteSpace(cultureCode))
        return false;

    try
    {
        // Will throw if invalid
        var culture = new System.Globalization.CultureInfo(cultureCode);
        return true;
    }
    catch (System.Globalization.CultureNotFoundException)
    {
        return false;
    }
}

/// <summary>
/// Request model for updating language preference
/// </summary>
/// <param name="Language">Culture code (e.g., "en-US", "es-ES") or null to clear preference</param>
public record UpdateLanguageRequest(string? Language);
```

#### Acceptance Criteria
- [x] Endpoint accessible at `PUT /api/profiles/current/language`
- [x] Requires authentication (`[Authorize]` attribute)
- [x] Accepts JSON body with `Language` property
- [x] Validates culture code format using CultureInfo
- [x] Returns 204 No Content on success
- [x] Returns 400 Bad Request for invalid culture code
- [x] Returns 401 Unauthorized if not authenticated
- [x] Returns 404 Not Found if profile doesn't exist
- [x] Returns 500 Internal Server Error on unexpected errors
- [x] Logs all operations with request ID for tracing
- [x] Accepts NULL to clear preference
- [x] Proper error response format (JSON with error and message)
- [x] Swagger documentation generated correctly

#### Testing Steps
1. Test with Postman/curl: Valid culture code
   ```bash
   PUT /api/profiles/current/language
   Authorization: Bearer {token}
   Body: { "language": "es-ES" }
   Expected: 204 No Content
   ```

2. Test with null (clear preference)
   ```bash
   Body: { "language": null }
   Expected: 204 No Content
   ```

3. Test with invalid culture code
   ```bash
   Body: { "language": "invalid" }
   Expected: 400 Bad Request
   ```

4. Test without authentication
   ```bash
   No Authorization header
   Expected: 401 Unauthorized
   ```

5. Test with user that has no profile
   ```bash
   Expected: 404 Not Found
   ```

6. Verify logs contain request ID and all steps

---

### Phase 1 Completion Checklist

Before moving to Phase 2, verify:

- [ ] Database migration executed successfully
- [ ] `PreferredLanguage` column exists in Profiles table
- [ ] Profile entity updated with new property
- [ ] All DTOs updated
- [ ] ProfileService method implemented
- [ ] Controller endpoint created and tested
- [ ] All unit tests passing
- [ ] All integration tests passing
- [ ] API documentation updated
- [ ] Database rollback script tested
- [ ] Code reviewed and approved
- [ ] No compiler warnings
- [ ] Logging verified in all layers

---

## 🔌 PHASE 2: Client-Side API Integration

**Duration**: 3-5 days  
**Priority**: HIGH (depends on Phase 1)  
**Team**: Frontend Developer

### Overview
Integrate the backend language preference API into the Blazor WebAssembly client application.

---

### Task 2.1: Update IProfilesClient Interface

**Assignee**: Frontend Developer  
**Estimated Time**: 1 hour  
**Priority**: P0 - Critical

#### Description
Add method signatures to the IProfilesClient interface for language preference operations.

#### Files to Modify
- `Sivar.Os.Shared/Clients/IProfilesClient.cs`

#### Code Changes
Add these methods to the interface (around line 50, after existing methods):

```csharp
/// <summary>
/// Updates the preferred language for the current user's profile
/// </summary>
/// <param name="language">Culture code (e.g., "en-US", "es-ES") or null to clear preference</param>
/// <exception cref="HttpRequestException">Request failed</exception>
Task UpdatePreferredLanguageAsync(string? language);

/// <summary>
/// Gets the current user's profile (includes PreferredLanguage)
/// </summary>
/// <returns>User's profile or null if not found</returns>
Task<ProfileDto?> GetCurrentProfileAsync();
```

#### Acceptance Criteria
- [x] Method signatures added to interface
- [x] XML documentation included for both methods
- [x] `UpdatePreferredLanguageAsync` accepts nullable string
- [x] `GetCurrentProfileAsync` returns nullable ProfileDto
- [x] Interface compiles without errors
- [x] No breaking changes to existing methods

#### Testing Steps
1. Build solution
2. Verify interface compilation
3. Verify implementing classes show compilation errors (expected until Task 2.2)

---

### Task 2.2: Implement ProfilesClient Methods

**Assignee**: Frontend Developer  
**Estimated Time**: 2-3 hours  
**Priority**: P0 - Critical

#### Description
Implement the language preference methods in the ProfilesClient class.

#### Files to Modify
- `Sivar.Os.Client/Clients/ProfilesClient.cs`

#### Code Changes
Add these methods to the ProfilesClient class:

```csharp
/// <inheritdoc/>
public async Task UpdatePreferredLanguageAsync(string? language)
{
    try
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"{_options.BaseUrl}api/profiles/current/language",
            new { Language = language }
        );
        
        response.EnsureSuccessStatusCode();
    }
    catch (HttpRequestException ex)
    {
        throw new HttpRequestException(
            $"Failed to update preferred language: {ex.Message}", 
            ex);
    }
}

/// <inheritdoc/>
public async Task<ProfileDto?> GetCurrentProfileAsync()
{
    try
    {
        var response = await _httpClient.GetAsync(
            $"{_options.BaseUrl}api/profiles/my"
        );
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ProfileDto>();
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return null;
    }
    catch (HttpRequestException ex)
    {
        // Log error and return null for graceful degradation
        Console.WriteLine($"Error getting current profile: {ex.Message}");
        return null;
    }
}
```

#### Acceptance Criteria
- [x] `UpdatePreferredLanguageAsync` calls correct endpoint
- [x] Method sends JSON payload with `Language` property
- [x] Method throws HttpRequestException on errors
- [x] `GetCurrentProfileAsync` retrieves user's profile
- [x] Returns null on 404 (profile not found)
- [x] Returns null on other errors (graceful degradation)
- [x] Uses configured BaseUrl from options
- [x] Properly deserializes ProfileDto including PreferredLanguage
- [x] No hardcoded URLs

#### Testing Steps
1. Mock HttpClient responses
2. Unit test: UpdatePreferredLanguageAsync with "es-ES"
3. Unit test: UpdatePreferredLanguageAsync with null
4. Unit test: UpdatePreferredLanguageAsync throws on 400
5. Unit test: GetCurrentProfileAsync returns profile
6. Unit test: GetCurrentProfileAsync returns null on 404
7. Integration test: Call real API endpoint
8. Verify JSON serialization of language property

---

### Task 2.3: Test API Integration

**Assignee**: Frontend Developer / QA  
**Estimated Time**: 2-3 hours  
**Priority**: P1 - High

#### Description
Create integration tests to verify client-server communication for language preferences.

#### Files to Create
- `Sivar.Os.Tests/Integration/ProfilesClientLanguageTests.cs`

#### Test Cases
```csharp
public class ProfilesClientLanguageTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task UpdatePreferredLanguage_ValidCulture_ReturnsSuccess()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        
        // Act
        await client.UpdatePreferredLanguageAsync("es-ES");
        var profile = await client.GetCurrentProfileAsync();
        
        // Assert
        Assert.NotNull(profile);
        Assert.Equal("es-ES", profile.PreferredLanguage);
    }
    
    [Fact]
    public async Task UpdatePreferredLanguage_Null_ClearsPreference()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        await client.UpdatePreferredLanguageAsync("es-ES");
        
        // Act
        await client.UpdatePreferredLanguageAsync(null);
        var profile = await client.GetCurrentProfileAsync();
        
        // Assert
        Assert.NotNull(profile);
        Assert.Null(profile.PreferredLanguage);
    }
    
    [Fact]
    public async Task UpdatePreferredLanguage_Unauthenticated_ThrowsException()
    {
        // Arrange
        var client = CreateUnauthenticatedClient();
        
        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.UpdatePreferredLanguageAsync("es-ES")
        );
    }
}
```

#### Acceptance Criteria
- [x] Integration tests created
- [x] Test valid culture code update
- [x] Test null value (clear preference)
- [x] Test unauthenticated request
- [x] All tests pass
- [x] Tests use test database (not production)
- [x] Tests clean up after themselves

---

### Phase 2 Completion Checklist

Before moving to Phase 3, verify:

- [ ] IProfilesClient interface updated
- [ ] ProfilesClient implementation complete
- [ ] All methods throw appropriate exceptions
- [ ] Integration tests created and passing
- [ ] API endpoints responding correctly
- [ ] JSON serialization/deserialization working
- [ ] Error handling implemented
- [ ] Code reviewed and approved
- [ ] No compiler warnings

---

## 🏗️ PHASE 3: Localization Infrastructure

**Duration**: 1 week  
**Priority**: CRITICAL (blocking for Phase 4 and 5)  
**Team**: Frontend Developer

### Overview
Set up the core localization infrastructure including resource files, culture service, and startup configuration.

---

### Task 3.1: Add NuGet Packages

**Assignee**: Frontend Developer  
**Estimated Time**: 30 minutes  
**Priority**: P0 - Critical

#### Description
Add required localization packages to the Blazor WebAssembly client project.

#### Files to Modify
- `Sivar.Os.Client/Sivar.Os.Client.csproj`

#### Code Changes
Add this package reference to the `<ItemGroup>` section:

```xml
<PackageReference Include="Microsoft.Extensions.Localization" Version="9.0.0" />
```

#### Acceptance Criteria
- [x] Package installed successfully
- [x] No version conflicts with existing packages
- [x] Project builds successfully
- [x] Package restore works on clean checkout
- [x] No dependency warnings

#### Testing Steps
1. Add package reference
2. Run `dotnet restore`
3. Build solution
4. Verify package in obj/project.assets.json
5. Test on clean checkout

---

### Task 3.2: Create Resource File Structure

**Assignee**: Frontend Developer  
**Estimated Time**: 4-6 hours  
**Priority**: P0 - Critical

#### Description
Create the directory structure and initial resource files for localization.

#### Directories to Create
```
Sivar.Os.Client/Resources/
├── Pages/
├── Components/
│   ├── Feed/
│   ├── Profile/
│   ├── Layout/
│   └── Shared/
└── (Common.resx files at root)
```

#### Files to Create

**1. Common.resx** (shared strings):
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  
  <!-- Common action buttons -->
  <data name="Cancel" xml:space="preserve">
    <value>Cancel</value>
  </data>
  <data name="Save" xml:space="preserve">
    <value>Save</value>
  </data>
  <data name="Delete" xml:space="preserve">
    <value>Delete</value>
  </data>
  <data name="Edit" xml:space="preserve">
    <value>Edit</value>
  </data>
  <data name="Close" xml:space="preserve">
    <value>Close</value>
  </data>
  <data name="Submit" xml:space="preserve">
    <value>Submit</value>
  </data>
  <data name="Back" xml:space="preserve">
    <value>Back</value>
  </data>
  
  <!-- Common status messages -->
  <data name="Loading" xml:space="preserve">
    <value>Loading...</value>
  </data>
  <data name="Saving" xml:space="preserve">
    <value>Saving...</value>
  </data>
  <data name="Error" xml:space="preserve">
    <value>Error</value>
  </data>
  <data name="Success" xml:space="preserve">
    <value>Success</value>
  </data>
  <data name="Warning" xml:space="preserve">
    <value>Warning</value>
  </data>
  <data name="Info" xml:space="preserve">
    <value>Information</value>
  </data>
  
  <!-- Common labels -->
  <data name="Email" xml:space="preserve">
    <value>Email</value>
  </data>
  <data name="Password" xml:space="preserve">
    <value>Password</value>
  </data>
  <data name="Username" xml:space="preserve">
    <value>Username</value>
  </data>
  <data name="Search" xml:space="preserve">
    <value>Search</value>
  </data>
  <data name="Filter" xml:space="preserve">
    <value>Filter</value>
  </data>
</root>
```

**2. Common.es.resx** (Spanish translations):
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  
  <!-- Common action buttons -->
  <data name="Cancel" xml:space="preserve">
    <value>Cancelar</value>
  </data>
  <data name="Save" xml:space="preserve">
    <value>Guardar</value>
  </data>
  <data name="Delete" xml:space="preserve">
    <value>Eliminar</value>
  </data>
  <data name="Edit" xml:space="preserve">
    <value>Editar</value>
  </data>
  <data name="Close" xml:space="preserve">
    <value>Cerrar</value>
  </data>
  <data name="Submit" xml:space="preserve">
    <value>Enviar</value>
  </data>
  <data name="Back" xml:space="preserve">
    <value>Volver</value>
  </data>
  
  <!-- Common status messages -->
  <data name="Loading" xml:space="preserve">
    <value>Cargando...</value>
  </data>
  <data name="Saving" xml:space="preserve">
    <value>Guardando...</value>
  </data>
  <data name="Error" xml:space="preserve">
    <value>Error</value>
  </data>
  <data name="Success" xml:space="preserve">
    <value>Éxito</value>
  </data>
  <data name="Warning" xml:space="preserve">
    <value>Advertencia</value>
  </data>
  <data name="Info" xml:space="preserve">
    <value>Información</value>
  </data>
  
  <!-- Common labels -->
  <data name="Email" xml:space="preserve">
    <value>Correo Electrónico</value>
  </data>
  <data name="Password" xml:space="preserve">
    <value>Contraseña</value>
  </data>
  <data name="Username" xml:space="preserve">
    <value>Nombre de Usuario</value>
  </data>
  <data name="Search" xml:space="preserve">
    <value>Buscar</value>
  </data>
  <data name="Filter" xml:space="preserve">
    <value>Filtrar</value>
  </data>
</root>
```

#### Project File Updates
Update `Sivar.Os.Client.csproj` to include resource files:

```xml
<ItemGroup>
  <EmbeddedResource Include="Resources\**\*.resx" />
</ItemGroup>
```

#### Acceptance Criteria
- [x] All directory structure created
- [x] Common.resx created with 20+ common strings
- [x] Common.es.resx created with Spanish translations
- [x] Resource files have valid XML format
- [x] Files set to "Embedded Resource" build action
- [x] Resources compile into satellite assemblies
- [x] No build errors or warnings
- [x] ResX file schema is correct

#### Testing Steps
1. Create directory structure
2. Add resource files
3. Build project
4. Verify satellite assemblies created in bin/Debug/net9.0/es
5. Use ResXManager extension to verify file structure
6. Test resource loading in code

---

### Task 3.3: Create Culture Service

**Assignee**: Frontend Developer  
**Estimated Time**: 6-8 hours  
**Priority**: P0 - Critical

#### Description
Create the CultureService that implements the priority-based culture resolution logic.

#### Files to Create
- `Sivar.Os.Client/Services/ICultureService.cs`
- `Sivar.Os.Client/Services/CultureService.cs`

#### Implementation

**ICultureService.cs**:
```csharp
namespace Sivar.Os.Client.Services;

/// <summary>
/// Service for managing application culture and language preferences
/// </summary>
public interface ICultureService
{
    /// <summary>
    /// Gets the effective culture using priority: Profile > Browser > Default
    /// </summary>
    Task<string> GetEffectiveCultureAsync();
    
    /// <summary>
    /// Gets the culture from user's profile preference (authenticated users only)
    /// </summary>
    Task<string?> GetProfileCultureAsync();
    
    /// <summary>
    /// Sets the culture preference in user's profile and applies it
    /// </summary>
    Task SetProfileCultureAsync(string? cultureName);
    
    /// <summary>
    /// Gets the browser's language preference
    /// </summary>
    Task<string> GetBrowserCultureAsync();
    
    /// <summary>
    /// Gets the list of supported culture codes
    /// </summary>
    string[] GetSupportedCultures();
    
    /// <summary>
    /// Event raised when culture changes
    /// </summary>
    event Action? CultureChanged;
}
```

**CultureService.cs**:
```csharp
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Sivar.Os.Shared.Clients;

namespace Sivar.Os.Client.Services;

/// <summary>
/// Implementation of culture management service
/// </summary>
public class CultureService : ICultureService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private readonly IProfilesClient _profilesClient;
    private readonly AuthenticationStateProvider _authStateProvider;
    
    private const string DEFAULT_CULTURE = "en-US";
    private readonly string[] _supportedCultures = { "en-US", "es-ES" };
    
    public event Action? CultureChanged;

    public CultureService(
        IJSRuntime jsRuntime,
        NavigationManager navigationManager,
        IProfilesClient profilesClient,
        AuthenticationStateProvider authStateProvider)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _profilesClient = profilesClient ?? throw new ArgumentNullException(nameof(profilesClient));
        _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
    }

    /// <inheritdoc/>
    public async Task<string> GetEffectiveCultureAsync()
    {
        try
        {
            Console.WriteLine("[CultureService] Resolving effective culture...");
            
            // Priority 1: Profile preference
            var profileCulture = await GetProfileCultureAsync();
            if (!string.IsNullOrEmpty(profileCulture) && IsSupportedCulture(profileCulture))
            {
                Console.WriteLine($"[CultureService] Using profile culture: {profileCulture}");
                return profileCulture;
            }

            // Priority 2: Browser language
            var browserCulture = await GetBrowserCultureAsync();
            if (!string.IsNullOrEmpty(browserCulture) && IsSupportedCulture(browserCulture))
            {
                Console.WriteLine($"[CultureService] Using browser culture: {browserCulture}");
                return browserCulture;
            }

            // Priority 3: Default
            Console.WriteLine($"[CultureService] Using default culture: {DEFAULT_CULTURE}");
            return DEFAULT_CULTURE;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CultureService] Error resolving culture: {ex.Message}");
            return DEFAULT_CULTURE;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetProfileCultureAsync()
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (authState?.User?.Identity?.IsAuthenticated == true)
            {
                var profile = await _profilesClient.GetCurrentProfileAsync();
                var culture = profile?.PreferredLanguage;
                
                Console.WriteLine($"[CultureService] Profile culture: {culture ?? "NULL"}");
                return culture;
            }
            
            Console.WriteLine("[CultureService] User not authenticated, no profile culture");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CultureService] Error getting profile culture: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SetProfileCultureAsync(string? cultureName)
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (authState?.User?.Identity?.IsAuthenticated != true)
            {
                Console.WriteLine("[CultureService] Cannot set profile culture: user not authenticated");
                throw new InvalidOperationException("User must be authenticated to set profile culture");
            }

            Console.WriteLine($"[CultureService] Setting profile culture to: {cultureName ?? "NULL"}");
            await _profilesClient.UpdatePreferredLanguageAsync(cultureName);
            
            // Determine effective culture after update
            var effectiveCulture = cultureName ?? await GetEffectiveCultureAsync();
            await ApplyCultureAsync(effectiveCulture);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CultureService] Error setting profile culture: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetBrowserCultureAsync()
    {
        try
        {
            var browserLang = await _jsRuntime.InvokeAsync<string>(
                "eval", "navigator.language || navigator.userLanguage"
            );
            
            var normalized = NormalizeCulture(browserLang);
            Console.WriteLine($"[CultureService] Browser language: {browserLang} => {normalized}");
            return normalized;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CultureService] Error getting browser culture: {ex.Message}");
            return DEFAULT_CULTURE;
        }
    }

    /// <inheritdoc/>
    public string[] GetSupportedCultures() => _supportedCultures;

    /// <summary>
    /// Applies the specified culture to the application
    /// </summary>
    private async Task ApplyCultureAsync(string cultureName)
    {
        Console.WriteLine($"[CultureService] Applying culture: {cultureName}");
        
        var culture = new CultureInfo(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        
        // Notify listeners
        CultureChanged?.Invoke();
        
        // Reload page to apply culture to all components
        _navigationManager.NavigateTo(_navigationManager.Uri, forceLoad: true);
    }

    /// <summary>
    /// Checks if a culture is supported
    /// </summary>
    private bool IsSupportedCulture(string culture)
    {
        return _supportedCultures.Contains(culture, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes browser language to full culture code
    /// </summary>
    private string NormalizeCulture(string? browserLang)
    {
        if (string.IsNullOrWhiteSpace(browserLang))
            return DEFAULT_CULTURE;
            
        // Browser might return "en", "es", etc. - map to full culture codes
        var langCode = browserLang.Split('-')[0].ToLower();
        
        var normalized = langCode switch
        {
            "en" => "en-US",
            "es" => "es-ES",
            _ => browserLang
        };
        
        // Return normalized if supported, otherwise default
        return IsSupportedCulture(normalized) ? normalized : DEFAULT_CULTURE;
    }
}
```

#### Acceptance Criteria
- [x] Interface defines all required methods
- [x] Service implements priority-based culture resolution
- [x] Logs culture resolution decisions to console
- [x] Handles unauthenticated users gracefully
- [x] Validates culture codes against supported list
- [x] Triggers page reload after culture change
- [x] Raises `CultureChanged` event
- [x] Handles exceptions without crashing
- [x] Browser language detection works
- [x] Normalizes culture codes correctly
- [x] All dependencies injected via constructor

#### Testing Steps
1. Unit test: GetEffectiveCultureAsync with profile preference
2. Unit test: GetEffectiveCultureAsync with browser only
3. Unit test: GetEffectiveCultureAsync falls back to default
4. Unit test: GetBrowserCultureAsync normalizes "en" to "en-US"
5. Unit test: SetProfileCultureAsync throws when not authenticated
6. Unit test: IsSupportedCulture validates correctly
7. Integration test: Full culture resolution flow

---

### Task 3.4: Configure Localization in Program.cs

**Assignee**: Frontend Developer  
**Estimated Time**: 2-3 hours  
**Priority**: P0 - Critical

#### Description
Register localization services and configure culture on application startup.

#### Files to Modify
- `Sivar.Os.Client/Program.cs`

#### Code Changes
Add the following after existing service registrations (around line 70):

```csharp
// Add localization services
builder.Services.AddLocalization();

// Configure supported cultures
var supportedCultures = new[] { "en-US", "es-ES" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("en-US")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});

// Register culture service
builder.Services.AddScoped<ICultureService, CultureService>();

// Build the host
var host = builder.Build();

// Resolve effective culture BEFORE running the app
try
{
    var cultureService = host.Services.GetRequiredService<ICultureService>();
    var effectiveCulture = await cultureService.GetEffectiveCultureAsync();

    var culture = new System.Globalization.CultureInfo(effectiveCulture);
    System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
    System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
    
    Console.WriteLine($"[Program] Application culture set to: {effectiveCulture}");
}
catch (Exception ex)
{
    Console.WriteLine($"[Program] Error setting culture: {ex.Message}. Using default.");
}

await host.RunAsync();
```

#### Acceptance Criteria
- [x] Localization services registered
- [x] Supported cultures configured (en-US, es-ES)
- [x] Default culture set to en-US
- [x] CultureService registered as scoped service
- [x] Culture resolved on app startup
- [x] Culture applied before app runs
- [x] Error handling prevents app crash on culture failure
- [x] App starts without errors
- [x] Console logging shows culture resolution

#### Testing Steps
1. Start application
2. Check browser console for culture logs
3. Verify correct culture applied
4. Test with different browser languages
5. Test with authenticated user with profile preference
6. Test with anonymous user
7. Verify fallback to default on errors

---

### Phase 3 Completion Checklist

Before moving to Phase 4, verify:

- [ ] NuGet packages installed
- [ ] Resource file structure created
- [ ] Common.resx and Common.es.resx created
- [ ] CultureService interface defined
- [ ] CultureService implemented
- [ ] Program.cs configured
- [ ] Culture resolution works on startup
- [ ] Browser language detection works
- [ ] Profile preference overrides browser
- [ ] Fallback to default works
- [ ] All unit tests passing
- [ ] No compilation errors
- [ ] No runtime errors
- [ ] Console logging working

---

## 🎨 PHASE 4: Culture Switcher Components

**Duration**: 3-4 days  
**Priority**: HIGH (depends on Phase 3)  
**Team**: Frontend Developer

### Overview
Create user interface components for language selection and management.

---

### Task 4.1: Create LanguageSelector Component

**Assignee**: Frontend Developer  
**Estimated Time**: 4-6 hours  
**Priority**: P1 - High

#### Description
Create a compact language selector component for use in navigation/header areas.

#### Files to Create
- `Sivar.Os.Client/Components/Shared/LanguageSelector.razor`
- `Sivar.Os.Client/Resources/Components/Shared/LanguageSelector.resx`
- `Sivar.Os.Client/Resources/Components/Shared/LanguageSelector.es.resx`

#### Implementation

**LanguageSelector.razor**:
```razor
@inject ICultureService CultureService
@inject ISnackbar Snackbar
@inject IStringLocalizer<LanguageSelector> Localizer

<MudSelect T="string" 
           Value="@_selectedCulture"
           ValueChanged="OnCultureChanged"
           Label="@Localizer["Language"]"
           Variant="Variant.Outlined"
           Adornment="Adornment.Start"
           AdornmentIcon="@Icons.Material.Filled.Language"
           Disabled="_isChanging"
           Dense="true"
           Class="language-selector">
    @foreach (var culture in _supportedCultures)
    {
        <MudSelectItem Value="@culture">
            @GetLanguageDisplay(culture)
        </MudSelectItem>
    }
</MudSelect>

@code {
    [Parameter]
    public EventCallback OnLanguageChanged { get; set; }

    private string _selectedCulture = "en-US";
    private string[] _supportedCultures = Array.Empty<string>();
    private bool _isChanging = false;

    protected override async Task OnInitializedAsync()
    {
        _supportedCultures = CultureService.GetSupportedCultures();
        _selectedCulture = await CultureService.GetEffectiveCultureAsync();
    }

    private async Task OnCultureChanged(string newCulture)
    {
        if (newCulture == _selectedCulture)
            return;

        _isChanging = true;
        StateHasChanged();

        try
        {
            await CultureService.SetProfileCultureAsync(newCulture);
            await OnLanguageChanged.InvokeAsync();
            // Page will reload automatically
        }
        catch (InvalidOperationException)
        {
            // User not authenticated - just reload with new culture
            var culture = new System.Globalization.CultureInfo(newCulture);
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            
            Snackbar.Add(Localizer["LanguageChanged"], Severity.Success);
            await Task.Delay(500);
            NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error changing language: {ex.Message}");
            Snackbar.Add(Localizer["ErrorChangingLanguage"], Severity.Error);
            _isChanging = false;
            StateHasChanged();
        }
    }

    private string GetLanguageDisplay(string culture) => culture switch
    {
        "en-US" => "🇺🇸 English",
        "es-ES" => "🇪🇸 Español",
        _ => culture
    };
}
```

**LanguageSelector.resx**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="Language" xml:space="preserve">
    <value>Language</value>
  </data>
  <data name="ErrorChangingLanguage" xml:space="preserve">
    <value>Error changing language. Please try again.</value>
  </data>
  <data name="LanguageChanged" xml:space="preserve">
    <value>Language changed successfully</value>
  </data>
</root>
```

**LanguageSelector.es.resx**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="Language" xml:space="preserve">
    <value>Idioma</value>
  </data>
  <data name="ErrorChangingLanguage" xml:space="preserve">
    <value>Error al cambiar el idioma. Inténtelo de nuevo.</value>
  </data>
  <data name="LanguageChanged" xml:space="preserve">
    <value>Idioma cambiado exitosamente</value>
  </data>
</root>
```

#### Acceptance Criteria
- [x] Component displays language dropdown
- [x] Shows current effective culture as selected
- [x] Lists all supported cultures with flags
- [x] Disables while changing culture
- [x] Shows success message on change
- [x] Shows error message on failure
- [x] Triggers page reload after change
- [x] Works for authenticated users (saves to profile)
- [x] Works for anonymous users (browser-only)
- [x] Resource files created for both languages
- [x] Component is reusable
- [x] Compact size for navigation bar

#### Testing Steps
1. Add component to NavMenu
2. Test switching from English to Spanish
3. Test switching from Spanish to English
4. Verify page reloads
5. Verify UI updates in new language
6. Test as authenticated user (should save to profile)
7. Test as anonymous user (should work without errors)
8. Test error handling (disconnect network)

---

### Task 4.2: Create ProfileLanguageSettings Component

**Assignee**: Frontend Developer  
**Estimated Time**: 6-8 hours  
**Priority**: P1 - High

#### Description
Create a comprehensive language settings component for the user's profile settings page.

#### Files to Create
- `Sivar.Os.Client/Components/Profile/ProfileLanguageSettings.razor`
- `Sivar.Os.Client/Resources/Components/Profile/ProfileLanguageSettings.resx`
- `Sivar.Os.Client/Resources/Components/Profile/ProfileLanguageSettings.es.resx`

#### Implementation

**ProfileLanguageSettings.razor**:
```razor
@inject ICultureService CultureService
@inject ISnackbar Snackbar
@inject IStringLocalizer<ProfileLanguageSettings> Localizer

<MudPaper Class="pa-4" Elevation="2">
    <MudStack Spacing="3">
        <MudText Typo="Typo.h6">
            <MudIcon Icon="@Icons.Material.Filled.Language" Class="mr-2" />
            @Localizer["LanguagePreferences"]
        </MudText>
        
        <MudDivider />
        
        @if (_isLoading)
        {
            <MudProgressLinear Indeterminate="true" />
        }
        else
        {
            @if (!string.IsNullOrEmpty(_profileCulture))
            {
                <MudAlert Severity="Severity.Info" Dense="true" Icon="@Icons.Material.Filled.CheckCircle">
                    @Localizer["CurrentlySaved"]: <strong>@GetLanguageName(_profileCulture)</strong>
                </MudAlert>
            }
            
            <MudAlert Severity="Severity.Normal" Dense="true" Icon="@Icons.Material.Filled.Info" Variant="Variant.Outlined">
                @Localizer["BrowserLanguage"]: <strong>@GetLanguageName(_browserCulture)</strong>
            </MudAlert>
            
            <MudText Typo="Typo.body2" Color="Color.Secondary">
                @Localizer["ExplanationText"]
            </MudText>
            
            <MudSelect T="string?"
                       Label="@Localizer["PreferredLanguage"]"
                       @bind-Value="_selectedCulture"
                       Variant="Variant.Outlined"
                       HelperText="@Localizer["HelperText"]"
                       AdornmentIcon="@Icons.Material.Filled.Translate"
                       Adornment="Adornment.Start">
                <MudSelectItem Value="@((string?)null)">
                    <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="1">
                        <MudIcon Icon="@Icons.Material.Filled.BrowserUpdated" Size="Size.Small" />
                        <MudText>@Localizer["UseBrowserLanguage"] (@GetLanguageName(_browserCulture))</MudText>
                    </MudStack>
                </MudSelectItem>
                @foreach (var culture in _supportedCultures)
                {
                    <MudSelectItem Value="@culture">
                        @GetLanguageDisplay(culture)
                    </MudSelectItem>
                }
            </MudSelect>
            
            <MudStack Row="true" Spacing="2">
                <MudButton Variant="Variant.Filled"
                           Color="Color.Primary"
                           OnClick="SaveLanguagePreference"
                           Disabled="@(!_hasChanges || _isSaving)"
                           StartIcon="@Icons.Material.Filled.Save">
                    @if (_isSaving)
                    {
                        <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                        @Localizer["Saving"]
                    }
                    else
                    {
                        @Localizer["SaveAndApply"]
                    }
                </MudButton>
                
                @if (_hasChanges)
                {
                    <MudButton Variant="Variant.Outlined"
                               Color="Color.Secondary"
                               OnClick="CancelChanges"
                               Disabled="_isSaving">
                        @Localizer["Cancel"]
                    </MudButton>
                }
            </MudStack>
        }
    </MudStack>
</MudPaper>

@code {
    private string? _profileCulture;
    private string _browserCulture = "en-US";
    private string? _selectedCulture;
    private string? _initialCulture;
    private string[] _supportedCultures = Array.Empty<string>();
    private bool _isLoading = true;
    private bool _isSaving;
    private bool _hasChanges => _selectedCulture != _initialCulture;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _supportedCultures = CultureService.GetSupportedCultures();
            _profileCulture = await CultureService.GetProfileCultureAsync();
            _browserCulture = await CultureService.GetBrowserCultureAsync();
            _selectedCulture = _profileCulture;
            _initialCulture = _profileCulture;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing language settings: {ex.Message}");
            Snackbar.Add(Localizer["ErrorLoading"], Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SaveLanguagePreference()
    {
        _isSaving = true;
        try
        {
            await CultureService.SetProfileCultureAsync(_selectedCulture);
            Snackbar.Add(Localizer["LanguageSaved"], Severity.Success);
            // Page will reload with new culture
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving language: {ex.Message}");
            Snackbar.Add(Localizer["ErrorSaving"], Severity.Error);
            _isSaving = false;
        }
    }

    private void CancelChanges()
    {
        _selectedCulture = _initialCulture;
    }

    private string GetLanguageName(string? culture) => culture switch
    {
        "en-US" => "English",
        "es-ES" => "Español",
        _ => culture ?? Localizer["Default"]
    };

    private string GetLanguageDisplay(string culture) => culture switch
    {
        "en-US" => "🇺🇸 English (United States)",
        "es-ES" => "🇪🇸 Español (España)",
        _ => culture
    };
}
```

**ProfileLanguageSettings.resx**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="LanguagePreferences" xml:space="preserve">
    <value>Language Preferences</value>
  </data>
  <data name="CurrentlySaved" xml:space="preserve">
    <value>Currently saved preference</value>
  </data>
  <data name="BrowserLanguage" xml:space="preserve">
    <value>Your browser language</value>
  </data>
  <data name="ExplanationText" xml:space="preserve">
    <value>Select your preferred language for the interface. If you don't set a preference, we'll use your browser's language.</value>
  </data>
  <data name="PreferredLanguage" xml:space="preserve">
    <value>Preferred Language</value>
  </data>
  <data name="HelperText" xml:space="preserve">
    <value>This setting will be saved to your profile and synced across all your devices</value>
  </data>
  <data name="UseBrowserLanguage" xml:space="preserve">
    <value>Use Browser Language</value>
  </data>
  <data name="Saving" xml:space="preserve">
    <value>Saving...</value>
  </data>
  <data name="SaveAndApply" xml:space="preserve">
    <value>Save & Apply</value>
  </data>
  <data name="Cancel" xml:space="preserve">
    <value>Cancel</value>
  </data>
  <data name="LanguageSaved" xml:space="preserve">
    <value>Language preference saved successfully! The page will reload.</value>
  </data>
  <data name="ErrorSaving" xml:space="preserve">
    <value>Error saving language preference. Please try again.</value>
  </data>
  <data name="ErrorLoading" xml:space="preserve">
    <value>Error loading language settings.</value>
  </data>
  <data name="Default" xml:space="preserve">
    <value>Default</value>
  </data>
</root>
```

**ProfileLanguageSettings.es.resx**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="LanguagePreferences" xml:space="preserve">
    <value>Preferencias de Idioma</value>
  </data>
  <data name="CurrentlySaved" xml:space="preserve">
    <value>Preferencia guardada actualmente</value>
  </data>
  <data name="BrowserLanguage" xml:space="preserve">
    <value>Idioma de tu navegador</value>
  </data>
  <data name="ExplanationText" xml:space="preserve">
    <value>Selecciona tu idioma preferido para la interfaz. Si no estableces una preferencia, usaremos el idioma de tu navegador.</value>
  </data>
  <data name="PreferredLanguage" xml:space="preserve">
    <value>Idioma Preferido</value>
  </data>
  <data name="HelperText" xml:space="preserve">
    <value>Esta configuración se guardará en tu perfil y se sincronizará en todos tus dispositivos</value>
  </data>
  <data name="UseBrowserLanguage" xml:space="preserve">
    <value>Usar Idioma del Navegador</value>
  </data>
  <data name="Saving" xml:space="preserve">
    <value>Guardando...</value>
  </data>
  <data name="SaveAndApply" xml:space="preserve">
    <value>Guardar y Aplicar</value>
  </data>
  <data name="Cancel" xml:space="preserve">
    <value>Cancelar</value>
  </data>
  <data name="LanguageSaved" xml:space="preserve">
    <value>¡Preferencia de idioma guardada exitosamente! La página se recargará.</value>
  </data>
  <data name="ErrorSaving" xml:space="preserve">
    <value>Error al guardar la preferencia de idioma. Inténtelo de nuevo.</value>
  </data>
  <data name="ErrorLoading" xml:space="preserve">
    <value>Error al cargar la configuración de idioma.</value>
  </data>
  <data name="Default" xml:space="preserve">
    <value>Predeterminado</value>
  </data>
</root>
```

#### Acceptance Criteria
- [x] Component displays comprehensive language settings
- [x] Shows current profile language preference
- [x] Shows browser language as reference
- [x] Allows selecting NULL to use browser language
- [x] Save button only enabled when changes made
- [x] Cancel button resets to initial value
- [x] Shows loading state while fetching data
- [x] Shows saving state while updating
- [x] Shows success message on save
- [x] Shows error messages on failures
- [x] Reloads page after successful save
- [x] All text is localized
- [x] Professional UI with icons and spacing
- [x] Responsive design

#### Testing Steps
1. Navigate to profile settings page
2. Verify current preference displayed
3. Change language preference
4. Verify Save button enables
5. Click Cancel - verify resets
6. Change again and click Save
7. Verify success message
8. Verify page reloads in new language
9. Log out and log in - verify preference persists
10. Test on different device - verify syncs

---

### Task 4.3: Integrate Components into Application

**Assignee**: Frontend Developer  
**Estimated Time**: 2-3 hours  
**Priority**: P1 - High

#### Description
Add the language selector to navigation and settings components to the appropriate pages.

#### Files to Modify
- `Sivar.Os.Client/Layout/NavMenu.razor`
- `Sivar.Os.Client/Pages/ProfileSettingsDemo.razor` (or appropriate settings page)

#### Code Changes

**NavMenu.razor** - Add at top of menu (around line 10):
```razor
<div class="pa-2">
    <LanguageSelector />
</div>
<MudDivider Class="my-2" />
```

**ProfileSettingsDemo.razor** - Add to settings sections:
```razor
<MudItem xs="12" md="6">
    <ProfileLanguageSettings />
</MudItem>
```

Don't forget to add the using statement at the top of files:
```razor
@using Sivar.Os.Client.Components.Shared
@using Sivar.Os.Client.Components.Profile
```

#### Acceptance Criteria
- [x] LanguageSelector visible in navigation menu
- [x] ProfileLanguageSettings visible in settings page
- [x] Components render without errors
- [x] Components function correctly in both locations
- [x] No layout issues or overflow
- [x] Mobile responsive

#### Testing Steps
1. Open application
2. Verify language selector in nav menu
3. Navigate to profile settings
4. Verify language settings section
5. Test both components independently
6. Verify no styling conflicts
7. Test on mobile viewport

---

### Phase 4 Completion Checklist

Before moving to Phase 5, verify:

- [ ] LanguageSelector component created
- [ ] ProfileLanguageSettings component created
- [ ] Resource files created for both components
- [ ] Components integrated into application
- [ ] Language switching works from nav menu
- [ ] Language settings work from profile page
- [ ] Both authenticated and anonymous users can switch
- [ ] Profile preferences save correctly
- [ ] Page reloads after language change
- [ ] UI updates in new language
- [ ] No errors in console
- [ ] All translations present
- [ ] Components are responsive

---

