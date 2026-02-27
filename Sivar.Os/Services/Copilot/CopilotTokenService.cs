using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Sivar.Os.Services.Copilot;

/// <summary>
/// Exchanges a long-lived GitHub OAuth token for a short-lived Copilot API token.
/// Caches and auto-refreshes 5 minutes before expiry.
/// </summary>
public class CopilotTokenService
{
    private const string CopilotTokenUrl = "https://api.github.com/copilot_internal/v2/token";
    private const int RefreshBufferSeconds = 300;

    private readonly IHttpClientFactory _httpFactory;
    private readonly string _githubToken;
    private readonly ILogger<CopilotTokenService> _logger;

    private string? _cachedToken;
    private long _expiresAtMs;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public CopilotTokenService(IHttpClientFactory httpFactory, string githubToken, ILogger<CopilotTokenService> logger)
    {
        _httpFactory = httpFactory;
        _githubToken = githubToken;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (_cachedToken is not null && _expiresAtMs - nowMs > RefreshBufferSeconds * 1000)
            return _cachedToken;

        await _lock.WaitAsync(ct);
        try
        {
            nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (_cachedToken is not null && _expiresAtMs - nowMs > RefreshBufferSeconds * 1000)
                return _cachedToken;

            _logger.LogInformation("Refreshing GitHub Copilot API token...");
            var http = _httpFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, CopilotTokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _githubToken);
            request.Headers.Add("User-Agent", "Sivar.Os/1.0");
            request.Headers.Add("Editor-Version", "vscode/1.85.0");
            request.Headers.Add("Editor-Plugin-Version", "copilot-chat/0.11.1");
            request.Headers.Add("Openai-Organization", "github-copilot");

            var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var token = root.GetProperty("token").GetString()!;
            var raw = root.GetProperty("expires_at").GetInt64();
            var expiresAtMs = raw > 1_000_000_000_000L ? raw : raw * 1000L;

            _cachedToken = token;
            _expiresAtMs = expiresAtMs;
            _logger.LogInformation("Copilot token refreshed, expires {At}", DateTimeOffset.FromUnixTimeMilliseconds(expiresAtMs));
            return token;
        }
        finally { _lock.Release(); }
    }
}
