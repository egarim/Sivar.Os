using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for ChatBotSettings entity operations
/// Phase 0.5: Configurable welcome messages and chat settings
/// </summary>
public interface IChatBotSettingsRepository : IBaseRepository<ChatBotSettings>
{
    /// <summary>
    /// Gets settings by unique key
    /// </summary>
    /// <param name="key">Settings key (e.g., "default", "es-SV")</param>
    /// <returns>Settings if found, null otherwise</returns>
    Task<ChatBotSettings?> GetByKeyAsync(string key);

    /// <summary>
    /// Gets the best matching active settings for a culture and optional region
    /// </summary>
    /// <param name="culture">Culture code (e.g., "es", "en")</param>
    /// <param name="regionCode">Optional region code (e.g., "SV", "GT")</param>
    /// <returns>Best matching settings or null</returns>
    Task<ChatBotSettings?> GetActiveSettingsAsync(string? culture = null, string? regionCode = null);

    /// <summary>
    /// Gets all active settings
    /// </summary>
    /// <returns>Collection of active settings</returns>
    Task<IEnumerable<ChatBotSettings>> GetAllActiveAsync();

    /// <summary>
    /// Gets settings by culture
    /// </summary>
    /// <param name="culture">Culture code</param>
    /// <returns>Collection of settings for that culture</returns>
    Task<IEnumerable<ChatBotSettings>> GetByCultureAsync(string culture);
}
