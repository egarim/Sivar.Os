# Complete Keycloak Setup Script for Sivar.Os
# This script creates the sivar-os realm, configures clients, and handles all redirect URI configurations
# 
# Usage:
#   .\Setup-SivarOsKeycloak.ps1 -KeycloakUrl "https://auth.sivar.lat"
#   .\Setup-SivarOsKeycloak.ps1 -KeycloakUrl "https://auth.sivar.lat" -AdminPassword "your-password"
#
# Parameters:
# - KeycloakUrl: Keycloak server URL (default: https://auth.sivar.lat)
# - AdminUsername: Admin username (default: admin)
# - AdminPassword: Admin password (required)
# - RecreateClients: Force recreation of clients if they exist

param(
    [string]$KeycloakUrl = "https://auth.sivar.lat",
    [string]$AdminUsername = "AuthRoot",
    [Parameter(Mandatory=$false)]
    [string]$AdminPassword = "",
    [string]$RealmName = "sivar-os",
    [string]$ServerClientId = "sivaros-server",
    [string]$WasmClientId = "sivaros-client",
    [string]$ProductionBaseUrl = "http://os.sivar.lat",
    [string]$LocalHttpsUrl = "https://localhost:5001",
    [string]$LocalHttpUrl = "http://localhost:5000",
    [switch]$UpdateOnly = $false,
    [switch]$ShowInstructions = $false,
    [switch]$RecreateClients = $false
)

# Prompt for password if not provided
if ([string]::IsNullOrEmpty($AdminPassword)) {
    $securePassword = Read-Host -Prompt "Enter Keycloak admin password" -AsSecureString
    $AdminPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword))
}

# Function to get admin access token
function Get-AdminToken {
    param($KeycloakUrl, $Username, $Password)
    
    $tokenUrl = "$KeycloakUrl/realms/master/protocol/openid-connect/token"
    $body = @{
        grant_type = "password"
        client_id = "admin-cli"
        username = $Username
        password = $Password
    }
    
    try {
        $response = Invoke-RestMethod -Uri $tokenUrl -Method Post -Body $body -ContentType "application/x-www-form-urlencoded"
        return $response.access_token
    }
    catch {
        Write-Error "Failed to get admin token: $($_.Exception.Message)"
        Write-Host ""
        Write-Host "Troubleshooting tips:" -ForegroundColor Yellow
        Write-Host "1. Verify Keycloak is running at: $KeycloakUrl" -ForegroundColor Gray
        Write-Host "2. Check admin credentials" -ForegroundColor Gray
        Write-Host "3. Ensure Keycloak admin console is accessible" -ForegroundColor Gray
        Write-Host ""
        exit 1
    }
}

# Function to make authenticated API calls
function Invoke-KeycloakApi {
    param($Uri, $Method = "GET", $Body = $null, $Token)
    
    $headers = @{
        "Authorization" = "Bearer $Token"
        "Content-Type" = "application/json"
    }
    
    try {
        if ($Body) {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            return Invoke-RestMethod -Uri $Uri -Method $Method -Body $jsonBody -Headers $headers
        }
        else {
            return Invoke-RestMethod -Uri $Uri -Method $Method -Headers $headers
        }
    }
    catch {
        Write-Warning "API call failed: $($_.Exception.Message)"
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Warning "Response: $responseBody"
        }
        throw
    }
}

# Function to create realm
function New-Realm {
    param($RealmName, $KeycloakUrl, $Token)
    
    $realm = @{
        realm = $RealmName
        enabled = $true
        displayName = "Sivar.Os"
        displayNameHtml = "<div class='kc-logo-text'><span>Sivar.Os</span></div>"
        registrationAllowed = $true
        loginWithEmailAllowed = $true
        duplicateEmailsAllowed = $false
        resetPasswordAllowed = $true
        editUsernameAllowed = $false
        rememberMe = $true
        verifyEmail = $false
        bruteForceProtected = $true
        permanentLockout = $false
        maxFailureWaitSeconds = 900
        minimumQuickLoginWaitSeconds = 60
        waitIncrementSeconds = 60
        quickLoginCheckMilliSeconds = 1000
        maxDeltaTimeSeconds = 43200
        failureFactor = 5
        accessTokenLifespan = 300
        accessTokenLifespanForImplicitFlow = 900
        ssoSessionIdleTimeout = 1800
        ssoSessionMaxLifespan = 36000
        offlineSessionIdleTimeout = 2592000
        accessCodeLifespan = 60
        accessCodeLifespanUserAction = 300
        accessCodeLifespanLogin = 1800
        actionTokenGeneratedByAdminLifespan = 43200
        actionTokenGeneratedByUserLifespan = 300
        internationalizationEnabled = $true
        supportedLocales = @("en", "es")
        defaultLocale = "en"
        passwordPolicy = "length(8) and upperCase(1) and lowerCase(1) and digits(1)"
    }
    
    try {
        Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms" -Method Post -Body $realm -Token $Token
        Write-Host "Realm '$RealmName' created successfully" -ForegroundColor Green
        return $true
    }
    catch {
        if ($_.Exception.Message -like "*409*") {
            Write-Host "Realm '$RealmName' already exists" -ForegroundColor Yellow
            return $true
        }
        else {
            Write-Error "Failed to create realm: $($_.Exception.Message)"
            return $false
        }
    }
}

# Function to delete a client (if needed for cleanup)
function Remove-KeycloakClient {
    param($RealmName, $ClientId, $KeycloakUrl, $Token)
    
    try {
        $clients = Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms/$RealmName/clients?clientId=$ClientId" -Token $Token
        if ($clients.Count -gt 0) {
            $clientUuid = $clients[0].id
            Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms/$RealmName/clients/$clientUuid" -Method Delete -Token $Token
            Write-Host "Client '$ClientId' deleted successfully" -ForegroundColor Green
            return $true
        }
        else {
            Write-Host "Client '$ClientId' not found" -ForegroundColor Yellow
            return $false
        }
    }
    catch {
        Write-Warning "Failed to delete client '$ClientId': $($_.Exception.Message)"
        return $false
    }
}

# Function to create Sivar.Os Server client (Confidential)
function New-SivarServerClient {
    param($RealmName, $ClientId, $ProductionUrl, $LocalHttpsUrl, $LocalHttpUrl, $KeycloakUrl, $Token)
    
    $redirectUris = @(
        "$ProductionUrl/signin-oidc",
        "$ProductionUrl/signout-callback-oidc",
        "$LocalHttpsUrl/signin-oidc",
        "$LocalHttpsUrl/signout-callback-oidc",
        "$LocalHttpUrl/signin-oidc",
        "$LocalHttpUrl/signout-callback-oidc"
    )
    
    $webOrigins = @(
        $ProductionUrl,
        $LocalHttpsUrl,
        $LocalHttpUrl
    )
    
    $postLogoutRedirectUris = @(
        "$ProductionUrl/*",
        "$LocalHttpsUrl/*",
        "$LocalHttpUrl/*"
    )
    
    $client = @{
        clientId = $ClientId
        name = "Sivar.Os Server"
        description = "Confidential client for Blazor Server"
        enabled = $true
        clientAuthenticatorType = "client-secret"
        secret = "CHANGE_THIS_SECRET_AFTER_IMPORT"
        redirectUris = $redirectUris
        webOrigins = $webOrigins
        protocol = "openid-connect"
        publicClient = $false
        frontchannelLogout = $true
        attributes = @{
            "pkce.code.challenge.method" = "S256"
            "post.logout.redirect.uris" = ($postLogoutRedirectUris -join "##")
            "oauth2.device.authorization.grant.enabled" = "false"
            "backchannel.logout.session.required" = "true"
            "backchannel.logout.revoke.offline.tokens" = "false"
        }
        standardFlowEnabled = $true
        implicitFlowEnabled = $false
        directAccessGrantsEnabled = $false
        serviceAccountsEnabled = $true
        fullScopeAllowed = $true
        defaultClientScopes = @("openid", "profile", "email", "roles", "web-origins", "acr")
        optionalClientScopes = @("address", "phone", "offline_access", "microprofile-jwt")
    }
    
    try {
        Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms/$RealmName/clients" -Method Post -Body $client -Token $Token
        Write-Host "Server client '$ClientId' created successfully" -ForegroundColor Green
        
        # Get the client UUID for further configuration
        $clients = Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms/$RealmName/clients?clientId=$ClientId" -Token $Token
        $clientUuid = $clients[0].id
        
        return $clientUuid
    }
    catch {
        if ($_.Exception.Message -like "*409*") {
            Write-Host "Server client '$ClientId' already exists" -ForegroundColor Yellow
            $clients = Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms/$RealmName/clients?clientId=$ClientId" -Token $Token
            return $clients[0].id
        }
        else {
            Write-Error "Failed to create Server client: $($_.Exception.Message)"
            return $null
        }
    }
}

# Function to create Sivar.Os WebAssembly client (Public)
function New-SivarWasmClient {
    param($RealmName, $ClientId, $ProductionUrl, $LocalHttpsUrl, $LocalHttpUrl, $KeycloakUrl, $Token)
    
    $redirectUris = @(
        "$ProductionUrl/authentication/login-callback",
        "$ProductionUrl/authentication/logout-callback",
        "$LocalHttpsUrl/authentication/login-callback",
        "$LocalHttpsUrl/authentication/logout-callback",
        "$LocalHttpUrl/authentication/login-callback",
        "$LocalHttpUrl/authentication/logout-callback"
    )
    
    $webOrigins = @(
        $ProductionUrl,
        $LocalHttpsUrl,
        $LocalHttpUrl
    )
    
    $postLogoutRedirectUris = @(
        "$ProductionUrl/*",
        "$LocalHttpsUrl/*",
        "$LocalHttpUrl/*"
    )
    
    $client = @{
        clientId = $ClientId
        name = "Sivar.Os WebAssembly Client"
        description = "Public client for Blazor WebAssembly"
        enabled = $true
        redirectUris = $redirectUris
        webOrigins = $webOrigins
        protocol = "openid-connect"
        publicClient = $true
        frontchannelLogout = $true
        attributes = @{
            "pkce.code.challenge.method" = "S256"
            "post.logout.redirect.uris" = ($postLogoutRedirectUris -join "##")
            "oauth2.device.authorization.grant.enabled" = "false"
            "oidc.ciba.grant.enabled" = "false"
        }
        standardFlowEnabled = $true
        implicitFlowEnabled = $false
        directAccessGrantsEnabled = $false
        serviceAccountsEnabled = $false
        fullScopeAllowed = $true
        defaultClientScopes = @("openid", "profile", "email", "roles", "web-origins", "acr")
        optionalClientScopes = @("address", "phone", "offline_access", "microprofile-jwt")
    }
    
    try {
        Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms/$RealmName/clients" -Method Post -Body $client -Token $Token
        Write-Host "WebAssembly client '$ClientId' created successfully" -ForegroundColor Green
        
        $clients = Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms/$RealmName/clients?clientId=$ClientId" -Token $Token
        return $clients[0].id
    }
    catch {
        if ($_.Exception.Message -like "*409*") {
            Write-Host "WebAssembly client '$ClientId' already exists" -ForegroundColor Yellow
            try {
                $clients = Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms/$RealmName/clients?clientId=$ClientId" -Token $Token
                return $clients[0].id
            }
            catch {
                Write-Warning "Could not retrieve existing WebAssembly client UUID"
                return $null
            }
        }
        else {
            Write-Error "Failed to create WebAssembly client: $($_.Exception.Message)"
            return $null
        }
    }
}

# Function to create roles
function New-Roles {
    param($RealmName, $KeycloakUrl, $Token)
    
    $roles = @(
        @{ name = "user"; description = "Regular user role" },
        @{ name = "admin"; description = "Administrator role" },
        @{ name = "moderator"; description = "Moderator role" }
    )
    
    foreach ($role in $roles) {
        try {
            Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms/$RealmName/roles" -Method Post -Body $role -Token $Token
            Write-Host "Role '$($role.name)' created successfully" -ForegroundColor Green
        }
        catch {
            if ($_.Exception.Message -like "*409*") {
                Write-Host "Role '$($role.name)' already exists" -ForegroundColor Yellow
            }
            else {
                Write-Warning "Failed to create role '$($role.name)': $($_.Exception.Message)"
            }
        }
    }
}

# Function to create test users
function New-TestUsers {
    param($RealmName, $KeycloakUrl, $Token)
    
    $users = @(
        # Personal users
        @{
            username = "roberto.guzman"
            email = "roberto.guzman@sivar.lat"
            firstName = "Roberto"
            lastName = "Guzman"
        },
        @{
            username = "jaime.macias"
            email = "jaime.macias@sivar.lat"
            firstName = "Jaime"
            lastName = "Macias"
        },
        @{
            username = "joche.ojeda"
            email = "joche.ojeda@sivar.lat"
            firstName = "Joche"
            lastName = "Ojeda"
        },
        @{
            username = "oscar.ojeda"
            email = "oscar.ojeda@sivar.lat"
            firstName = "Oscar"
            lastName = "Ojeda"
        },
        # Business users (restaurants with booking enabled)
        @{
            username = "dragon-palace"
            email = "dragon-palace@sivar.lat"
            firstName = "Dragon"
            lastName = "Palace"
        },
        @{
            username = "el-gaucho"
            email = "el-gaucho@sivar.lat"
            firstName = "El"
            lastName = "Gaucho"
        },
        @{
            username = "la-pampa-argentina"
            email = "la-pampa-argentina@sivar.lat"
            firstName = "La Pampa"
            lastName = "Argentina"
        },
        @{
            username = "barberia-el-caballero"
            email = "barberia-el-caballero@sivar.lat"
            firstName = "Barbería"
            lastName = "El Caballero"
        }
    )
    
    foreach ($userData in $users) {
        $user = @{
            username = $userData.username
            email = $userData.email
            firstName = $userData.firstName
            lastName = $userData.lastName
            enabled = $true
            emailVerified = $true
            credentials = @(
                @{
                    type = "password"
                    value = "SivarOs123!"
                    temporary = $false
                }
            )
        }
        
        try {
            Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms/$RealmName/users" -Method Post -Body $user -Token $Token
            Write-Host "User '$($userData.username)' created successfully" -ForegroundColor Green
            
            # Get user ID and assign user role
            $users = Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms/$RealmName/users?username=$($userData.username)" -Token $Token
            $userId = $users[0].id
            
            # Assign user role
            $userRole = Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms/$RealmName/roles/user" -Token $Token
            $rolesToAssign = @($userRole)
            
            Invoke-KeycloakApi -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$userId/role-mappings/realm" -Method Post -Body $rolesToAssign -Token $Token
            Write-Host "Role 'user' assigned to '$($userData.username)'" -ForegroundColor Green
        }
        catch {
            if ($_.Exception.Message -like "*409*") {
                Write-Host "User '$($userData.username)' already exists" -ForegroundColor Yellow
            }
            else {
                Write-Warning "Failed to create user '$($userData.username)': $($_.Exception.Message)"
            }
        }
    }
}

# Main script execution
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  Sivar.Os Keycloak Setup Script" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

if ($ShowInstructions) {
    Write-Host "Manual Setup Instructions:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Access Keycloak Admin Console: $KeycloakUrl/admin" -ForegroundColor Gray
    Write-Host "2. Login with admin credentials" -ForegroundColor Gray
    Write-Host "3. Create realm: $RealmName" -ForegroundColor Gray
    Write-Host "4. Create client: $ServerClientId (Confidential)" -ForegroundColor Gray
    Write-Host "5. Create client: $WasmClientId (Public)" -ForegroundColor Gray
    Write-Host "6. Configure redirect URIs and web origins" -ForegroundColor Gray
    Write-Host "7. Create roles: admin, user, moderator" -ForegroundColor Gray
    Write-Host "8. Create test users" -ForegroundColor Gray
    Write-Host ""
    exit 0
}

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Keycloak URL: $KeycloakUrl" -ForegroundColor Gray
Write-Host "  Realm: $RealmName" -ForegroundColor Gray
Write-Host "  Server Client: $ServerClientId (Confidential)" -ForegroundColor Gray
Write-Host "  WASM Client: $WasmClientId (Public)" -ForegroundColor Gray
Write-Host ""
Write-Host "URLs to be configured:" -ForegroundColor Yellow
Write-Host "  Production: $ProductionBaseUrl" -ForegroundColor Gray
Write-Host "  Dev HTTPS: $LocalHttpsUrl" -ForegroundColor Gray
Write-Host "  Dev HTTP: $LocalHttpUrl" -ForegroundColor Gray
Write-Host ""

# Get admin token
Write-Host "Getting admin access token..." -ForegroundColor Yellow
$adminToken = Get-AdminToken -KeycloakUrl $KeycloakUrl -Username $AdminUsername -Password $AdminPassword
Write-Host "Admin token obtained successfully" -ForegroundColor Green
Write-Host ""

if (-not $UpdateOnly) {
    # Create realm
    Write-Host "Step 1: Creating realm..." -ForegroundColor Yellow
    $realmCreated = New-Realm -RealmName $RealmName -KeycloakUrl $KeycloakUrl -Token $adminToken
    
    if (-not $realmCreated) {
        Write-Error "Failed to create realm. Exiting."
        exit 1
    }
    Write-Host ""
    
    # Create roles
    Write-Host "Step 2: Creating roles..." -ForegroundColor Yellow
    New-Roles -RealmName $RealmName -KeycloakUrl $KeycloakUrl -Token $adminToken
    Write-Host ""
}

# Create/update clients
Write-Host "Step 3: Creating Server client (Confidential)..." -ForegroundColor Yellow
if ($RecreateClients) {
    Write-Host "  RecreateClients flag set - removing existing client first..." -ForegroundColor Yellow
    Remove-KeycloakClient -RealmName $RealmName -ClientId $ServerClientId -KeycloakUrl $KeycloakUrl -Token $adminToken
}
$serverClientUuid = New-SivarServerClient -RealmName $RealmName -ClientId $ServerClientId -ProductionUrl $ProductionBaseUrl -LocalHttpsUrl $LocalHttpsUrl -LocalHttpUrl $LocalHttpUrl -KeycloakUrl $KeycloakUrl -Token $adminToken
Write-Host ""

Write-Host "Step 4: Creating WebAssembly client (Public)..." -ForegroundColor Yellow
if ($RecreateClients) {
    Write-Host "  RecreateClients flag set - removing existing client first..." -ForegroundColor Yellow
    Remove-KeycloakClient -RealmName $RealmName -ClientId $WasmClientId -KeycloakUrl $KeycloakUrl -Token $adminToken
}
$wasmClientUuid = New-SivarWasmClient -RealmName $RealmName -ClientId $WasmClientId -ProductionUrl $ProductionBaseUrl -LocalHttpsUrl $LocalHttpsUrl -LocalHttpUrl $LocalHttpUrl -KeycloakUrl $KeycloakUrl -Token $adminToken
Write-Host ""

if (-not $UpdateOnly) {
    # Create test users
    Write-Host "Step 5: Creating test users..." -ForegroundColor Yellow
    New-TestUsers -RealmName $RealmName -KeycloakUrl $KeycloakUrl -Token $adminToken
    Write-Host ""
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host "  Setup completed successfully!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Configuration Summary:" -ForegroundColor Cyan
Write-Host "  Keycloak URL: $KeycloakUrl" -ForegroundColor White
Write-Host "  Realm: $RealmName" -ForegroundColor White
Write-Host "  Authority: $KeycloakUrl/realms/$RealmName" -ForegroundColor White
Write-Host ""
Write-Host "Clients Created:" -ForegroundColor Cyan
Write-Host "  Server Client: $ServerClientId (Confidential)" -ForegroundColor White
Write-Host "  WASM Client: $WasmClientId (Public)" -ForegroundColor White
Write-Host ""
Write-Host "Test Users (Password: SivarOs123!):" -ForegroundColor Cyan
Write-Host "  - roberto.guzman@sivar.lat" -ForegroundColor White
Write-Host "  - jaime.macias@sivar.lat" -ForegroundColor White
Write-Host "  - joche.ojeda@sivar.lat" -ForegroundColor White
Write-Host "  - oscar.ojeda@sivar.lat" -ForegroundColor White
Write-Host ""
Write-Host "=========================================" -ForegroundColor Red
Write-Host "  IMPORTANT: Next Steps Required" -ForegroundColor Red
Write-Host "=========================================" -ForegroundColor Red
Write-Host ""
Write-Host "1. GET CLIENT SECRET from Keycloak Admin Console:" -ForegroundColor Yellow
Write-Host "   - Open: $KeycloakUrl/admin" -ForegroundColor Gray
Write-Host "   - Navigate to: sivar-os realm > Clients > $ServerClientId" -ForegroundColor Gray
Write-Host "   - Go to 'Credentials' tab" -ForegroundColor Gray
Write-Host "   - Copy the 'Client secret' value" -ForegroundColor Gray
Write-Host ""
Write-Host "2. UPDATE appsettings.json:" -ForegroundColor Yellow
Write-Host "   File: Sivar.Os/appsettings.json" -ForegroundColor Gray
Write-Host ""
Write-Host '   "Keycloak": {' -ForegroundColor Cyan
Write-Host "     `"Authority`": `"$KeycloakUrl/realms/$RealmName`"," -ForegroundColor Cyan
Write-Host "     `"ClientIdServer`": `"$ServerClientId`"," -ForegroundColor Cyan
Write-Host "     `"ClientIdClient`": `"$WasmClientId`"," -ForegroundColor Cyan
Write-Host '     "ClientSecret": "PASTE_YOUR_SECRET_HERE"' -ForegroundColor Red
Write-Host '   }' -ForegroundColor Cyan
Write-Host ""
Write-Host "3. RUN THE APPLICATION:" -ForegroundColor Yellow
Write-Host "   cd Sivar.Os && dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "4. TEST AUTHENTICATION:" -ForegroundColor Yellow
Write-Host "   Development: $LocalHttpsUrl" -ForegroundColor Gray
Write-Host "   Production: $ProductionBaseUrl" -ForegroundColor Gray
Write-Host "   Login with any test user using password: SivarOs123!" -ForegroundColor Gray
Write-Host ""
