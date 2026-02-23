using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sivar.Os.Configuration;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Services;

/// <summary>
/// Implementation of Keycloak Admin API service
/// Handles authentication and user attribute management
/// </summary>
public class KeycloakAdminService : IKeycloakAdminService
{
    private readonly KeycloakAdminOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<KeycloakAdminService> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public KeycloakAdminService(
        IOptions<KeycloakAdminOptions> options,
        HttpClient httpClient,
        ILogger<KeycloakAdminService> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsEnabled => _options.Enabled && _options.IsValid();

    /// <inheritdoc />
    public async Task<KeycloakUser?> GetUserByIdAsync(string keycloakId)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Keycloak Admin API is disabled");
            return null;
        }

        try
        {
            await EnsureAccessTokenAsync();
            
            var url = $"{_options.UsersEndpoint}/{keycloakId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get user {KeycloakId}: {StatusCode}", keycloakId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<KeycloakUser>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {KeycloakId}", keycloakId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<KeycloakUser?> GetUserByEmailAsync(string email)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Keycloak Admin API is disabled");
            return null;
        }

        try
        {
            await EnsureAccessTokenAsync();
            
            var url = $"{_options.UsersEndpoint}?email={Uri.EscapeDataString(email)}&exact=true";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to search user by email: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<KeycloakUser>>(content, JsonOptions);
            
            return users?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching user by email {Email}", email);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<KeycloakOperationResult> UpdateUserAttributesAsync(
        string keycloakId, 
        Dictionary<string, string> attributes)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Keycloak Admin API is disabled. Skipping attribute update.");
            return new KeycloakOperationResult(true); // Return success in disabled mode
        }

        try
        {
            // First get the current user to preserve existing attributes
            var user = await GetUserByIdAsync(keycloakId);
            if (user == null)
            {
                return new KeycloakOperationResult(false, $"User not found: {keycloakId}");
            }

            // Merge new attributes with existing ones
            foreach (var attr in attributes)
            {
                user.Attributes[attr.Key] = new List<string> { attr.Value };
            }

            await EnsureAccessTokenAsync();

            var url = $"{_options.UsersEndpoint}/{keycloakId}";
            var payload = new { attributes = user.Attributes };
            var json = JsonSerializer.Serialize(payload, JsonOptions);

            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update user attributes: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                return new KeycloakOperationResult(false, $"HTTP {response.StatusCode}: {errorContent}");
            }

            _logger.LogInformation("Updated attributes for user {KeycloakId}: {Attributes}", 
                keycloakId, string.Join(", ", attributes.Keys));
            
            return new KeycloakOperationResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attributes for user {KeycloakId}", keycloakId);
            return new KeycloakOperationResult(false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<KeycloakOperationResult> SetPhoneVerifiedAsync(
        string keycloakId, 
        string phoneNumber, 
        string countryCode)
    {
        var attributes = new Dictionary<string, string>
        {
            ["phone_number"] = phoneNumber,
            ["phone_verified"] = "true",
            ["country_code"] = countryCode.ToUpperInvariant(),
            ["phone_verified_at"] = DateTime.UtcNow.ToString("O")
        };

        return await UpdateUserAttributesAsync(keycloakId, attributes);
    }

    /// <inheritdoc />
    public async Task<KeycloakOperationResult> UpdateWaitingListStatusAsync(
        string keycloakId, 
        WaitingListStatus status)
    {
        var attributes = new Dictionary<string, string>
        {
            ["waiting_list_status"] = status.ToString().ToLowerInvariant()
        };

        if (status == WaitingListStatus.Approved)
        {
            attributes["approved_at"] = DateTime.UtcNow.ToString("O");
        }

        return await UpdateUserAttributesAsync(keycloakId, attributes);
    }

    /// <inheritdoc />
    public async Task<KeycloakCreateUserResult> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        bool emailVerified = true)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Keycloak Admin API is disabled");
            return new KeycloakCreateUserResult(false, null, "Keycloak Admin API is disabled");
        }

        try
        {
            await EnsureAccessTokenAsync();

            var userPayload = new
            {
                username = email,
                email = email,
                firstName = firstName,
                lastName = lastName,
                enabled = true,
                emailVerified = emailVerified,
                credentials = new[]
                {
                    new
                    {
                        type = "password",
                        value = password,
                        temporary = false
                    }
                }
            };

            var json = JsonSerializer.Serialize(userPayload, JsonOptions);
            var request = new HttpRequestMessage(HttpMethod.Post, _options.UsersEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create user {Email}: {StatusCode} - {Error}",
                    email, response.StatusCode, errorContent);
                return new KeycloakCreateUserResult(false, null, $"HTTP {response.StatusCode}: {errorContent}");
            }

            // Get the created user's ID from the Location header
            string? userId = null;
            if (response.Headers.Location != null)
            {
                var locationPath = response.Headers.Location.ToString();
                userId = locationPath.Split('/').LastOrDefault();
            }

            _logger.LogInformation("Created user {Email} with ID {UserId}", email, userId);
            return new KeycloakCreateUserResult(true, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Email}", email);
            return new KeycloakCreateUserResult(false, null, ex.Message);
        }
    }

    /// <summary>
    /// Ensure we have a valid access token for the Admin API
    /// </summary>
    private async Task EnsureAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-1))
        {
            return; // Token still valid
        }

        await _tokenLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-1))
            {
                return;
            }

            _logger.LogDebug("Obtaining new Keycloak admin access token");

            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret
            });

            var response = await _httpClient.PostAsync(_options.TokenEndpoint, tokenRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to obtain admin token: {response.StatusCode} - {error}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content, JsonOptions);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Invalid token response from Keycloak");
            }

            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            _logger.LogDebug("Obtained new admin token, expires in {ExpiresIn} seconds", tokenResponse.ExpiresIn);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}
