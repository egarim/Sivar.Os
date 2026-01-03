namespace Sivar.Os.Configuration;

/// <summary>
/// Configuration options for Keycloak Admin API access
/// Used to update user attributes (phone_verified, waiting_list_status, etc.)
/// </summary>
public class KeycloakAdminOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "KeycloakAdmin";

    /// <summary>
    /// Base URL for Keycloak Admin API
    /// Example: https://auth.sivar.lat/admin/realms/sivar-os
    /// </summary>
    public string AdminApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// Realm name (extracted from Authority URL if not set)
    /// </summary>
    public string Realm { get; set; } = "sivar-os";

    /// <summary>
    /// Client ID for service account (must have admin permissions)
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret for service account
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Token endpoint for obtaining admin access token
    /// </summary>
    public string TokenEndpoint => $"{BaseUrl}/realms/{Realm}/protocol/openid-connect/token";

    /// <summary>
    /// Users endpoint for managing users
    /// </summary>
    public string UsersEndpoint => $"{AdminApiUrl}/users";

    /// <summary>
    /// Base Keycloak URL (derived from AdminApiUrl)
    /// </summary>
    public string BaseUrl
    {
        get
        {
            // Extract base URL from AdminApiUrl (remove /admin/realms/xxx)
            if (string.IsNullOrEmpty(AdminApiUrl))
                return string.Empty;
                
            var idx = AdminApiUrl.IndexOf("/admin/realms", StringComparison.OrdinalIgnoreCase);
            return idx > 0 ? AdminApiUrl[..idx] : AdminApiUrl;
        }
    }

    /// <summary>
    /// Whether admin API access is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Validate configuration
    /// </summary>
    public bool IsValid()
    {
        if (!Enabled)
            return true;

        return !string.IsNullOrWhiteSpace(AdminApiUrl)
            && !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret)
            && !string.IsNullOrWhiteSpace(Realm);
    }
}
