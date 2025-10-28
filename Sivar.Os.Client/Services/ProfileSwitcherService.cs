using Sivar.Os.Client.Clients;
using Sivar.Os.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sivar.Os.Client.Services;

/// <summary>
/// Service for managing profile switching and creation operations
/// </summary>
public interface IProfileSwitcherService
{
    /// <summary>
    /// Get all profiles for the current user
    /// </summary>
    Task<List<ProfileDto>> GetUserProfilesAsync();

    /// <summary>
    /// Get the currently active profile
    /// </summary>
    Task<ProfileDto?> GetActiveProfileAsync();

    /// <summary>
    /// Switch to a different profile
    /// </summary>
    Task<bool> SwitchProfileAsync(Guid profileId);

    /// <summary>
    /// Create a new profile
    /// </summary>
    Task<ProfileDto?> CreateProfileAsync(CreateAnyProfileDto request);

    /// <summary>
    /// Get all available profile types
    /// </summary>
    Task<List<ProfileTypeDto>> GetProfileTypesAsync();
}

/// <summary>
/// Implementation of profile switcher service
/// </summary>
public class ProfileSwitcherService : IProfileSwitcherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProfileSwitcherService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProfileSwitcherService(HttpClient httpClient, ILogger<ProfileSwitcherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Configure JSON serialization options to match server-side configuration
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <inheritdoc/>
    public async Task<List<ProfileDto>> GetUserProfilesAsync()
    {
        try
        {
            _logger.LogInformation("[ProfileSwitcherService] Getting user profiles");
            
            var response = await _httpClient.GetAsync("/api/profiles/my/all");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var profiles = JsonSerializer.Deserialize<List<ProfileDto>>(content, _jsonOptions);
                _logger.LogInformation($"[ProfileSwitcherService] Retrieved {profiles?.Count ?? 0} profiles");
                return profiles ?? new();
            }

            _logger.LogWarning($"[ProfileSwitcherService] Failed to get profiles: {response.StatusCode}");
            return new();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ProfileSwitcherService] Error getting profiles: {ex.Message}");
            return new();
        }
    }

    /// <inheritdoc/>
    public async Task<ProfileDto?> GetActiveProfileAsync()
    {
        try
        {
            _logger.LogInformation("[ProfileSwitcherService] Getting active profile");
            
            var response = await _httpClient.GetAsync("/api/profiles/my/active");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var profile = JsonSerializer.Deserialize<ProfileDto>(content, _jsonOptions);
                _logger.LogInformation($"[ProfileSwitcherService] Retrieved active profile: {profile?.Id}");
                return profile;
            }

            _logger.LogWarning($"[ProfileSwitcherService] Failed to get active profile: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ProfileSwitcherService] Error getting active profile: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SwitchProfileAsync(Guid profileId)
    {
        try
        {
            _logger.LogInformation($"[ProfileSwitcherService] Switching to profile: {profileId}");
            
            var response = await _httpClient.PutAsync($"/api/profiles/{profileId}/set-active", null);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"[ProfileSwitcherService] Successfully switched to profile: {profileId}");
                return true;
            }

            _logger.LogWarning($"[ProfileSwitcherService] Failed to switch profile: {response.StatusCode}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ProfileSwitcherService] Error switching profile: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<ProfileDto?> CreateProfileAsync(CreateAnyProfileDto request)
    {
        try
        {
            _logger.LogInformation("[ProfileSwitcherService] Creating new profile");
            
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/profiles", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var profile = JsonSerializer.Deserialize<ProfileDto>(responseContent, _jsonOptions);
                _logger.LogInformation($"[ProfileSwitcherService] Successfully created profile: {profile?.Id}");
                return profile;
            }

            _logger.LogWarning($"[ProfileSwitcherService] Failed to create profile: {response.StatusCode}");
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning($"[ProfileSwitcherService] Error content: {errorContent}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ProfileSwitcherService] Error creating profile: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ProfileTypeDto>> GetProfileTypesAsync()
    {
        try
        {
            _logger.LogInformation("[ProfileSwitcherService] Getting profile types");
            
            var response = await _httpClient.GetAsync("/api/profiletypes");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var types = JsonSerializer.Deserialize<List<ProfileTypeDto>>(content, _jsonOptions);
                _logger.LogInformation($"[ProfileSwitcherService] Retrieved {types?.Count ?? 0} profile types");
                return types ?? new();
            }

            _logger.LogWarning($"[ProfileSwitcherService] Failed to get profile types: {response.StatusCode}");
            return new();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ProfileSwitcherService] Error getting profile types: {ex.Message}");
            return new();
        }
    }
}
