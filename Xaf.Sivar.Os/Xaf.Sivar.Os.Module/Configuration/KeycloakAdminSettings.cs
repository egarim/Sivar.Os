namespace Xaf.Sivar.Os.Module.Configuration
{
    /// <summary>
    /// Configuration settings for Keycloak Admin API access.
    /// Used by the SeederLog actions to create users in Keycloak.
    /// </summary>
    public class KeycloakAdminSettings
    {
        /// <summary>
        /// Configuration section name in appsettings.json
        /// </summary>
        public const string SectionName = "KeycloakAdmin";

        /// <summary>
        /// Base URL of the Keycloak server (e.g., "https://auth.sivar.lat")
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:8080";

        /// <summary>
        /// The Keycloak realm to manage users in (e.g., "sivar-os")
        /// </summary>
        public string Realm { get; set; } = "sivar-os";

        /// <summary>
        /// Admin username for Keycloak Admin API authentication
        /// </summary>
        public string AdminUsername { get; set; } = "admin";

        /// <summary>
        /// Admin password for Keycloak Admin API authentication
        /// </summary>
        public string AdminPassword { get; set; } = string.Empty;

        /// <summary>
        /// Client ID for admin authentication (typically "admin-cli")
        /// </summary>
        public string ClientId { get; set; } = "admin-cli";

        /// <summary>
        /// Default password to set for newly created users
        /// </summary>
        public string DefaultUserPassword { get; set; } = "SivarOs123!";

        /// <summary>
        /// Whether to set the default password as temporary (user must change on first login)
        /// </summary>
        public bool TemporaryPassword { get; set; } = false;

        /// <summary>
        /// Whether to set email as verified for new users
        /// </summary>
        public bool EmailVerified { get; set; } = true;

        /// <summary>
        /// Whether to enable new users by default
        /// </summary>
        public bool EnabledByDefault { get; set; } = true;

        /// <summary>
        /// Gets the Admin API base URL for the configured realm
        /// </summary>
        public string AdminApiUrl => $"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}";

        /// <summary>
        /// Gets the token endpoint URL for authentication
        /// </summary>
        public string TokenEndpoint => $"{BaseUrl.TrimEnd('/')}/realms/master/protocol/openid-connect/token";

        /// <summary>
        /// Validates the configuration and returns any error messages
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(BaseUrl))
                errors.Add("KeycloakAdmin:BaseUrl is required");

            if (string.IsNullOrWhiteSpace(Realm))
                errors.Add("KeycloakAdmin:Realm is required");

            if (string.IsNullOrWhiteSpace(AdminUsername))
                errors.Add("KeycloakAdmin:AdminUsername is required");

            if (string.IsNullOrWhiteSpace(AdminPassword))
                errors.Add("KeycloakAdmin:AdminPassword is required");

            if (string.IsNullOrWhiteSpace(DefaultUserPassword))
                errors.Add("KeycloakAdmin:DefaultUserPassword is required");

            return errors;
        }

        /// <summary>
        /// Returns true if the configuration is valid
        /// </summary>
        public bool IsValid => Validate().Count == 0;
    }
}
