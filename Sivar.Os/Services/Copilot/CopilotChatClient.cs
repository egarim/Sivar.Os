using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Sivar.Os.Services.Copilot;

/// <summary>
/// IChatClient implementation backed by GitHub Copilot.
/// Uses the OpenAI-compatible streaming API at api.individual.githubcopilot.com.
/// Implements Microsoft.Extensions.AI.IChatClient.
/// </summary>
public class CopilotChatClient : IChatClient
{
    private const string ApiBase = "https://api.individual.githubcopilot.com";

    private readonly IHttpClientFactory _httpFactory;
    private readonly CopilotTokenService _tokenService;
    private readonly string _modelId;
    private readonly ILogger<CopilotChatClient> _logger;

    public ChatClientMetadata Metadata => new("github-copilot", new Uri(ApiBase), _modelId);

    public CopilotChatClient(IHttpClientFactory httpFactory, CopilotTokenService tokenService, string modelId, ILogger<CopilotChatClient> logger)
    {
        _httpFactory = httpFactory;
        _tokenService = tokenService;
        _modelId = modelId;
        _logger = logger;
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var chunks = new StringBuilder();
        await foreach (var update in GetStreamingResponseAsync(messages, options, cancellationToken))
            foreach (var content in update.Contents.OfType<TextContent>())
                chunks.Append(content.Text);

        return new ChatResponse([new ChatMessage(ChatRole.Assistant, chunks.ToString())]);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var token = await _tokenService.GetTokenAsync(cancellationToken);
        var body = BuildRequestBody(messages, options);
        var json = JsonSerializer.Serialize(body);

        var http = _httpFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("User-Agent", "Sivar.Os/1.0");
        request.Headers.Add("Editor-Version", "vscode/1.85.0");
        request.Headers.Add("Openai-Organization", "github-copilot");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage? response = null;
        Exception? err = null;
        try
        {
            response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) { err = ex; _logger.LogError(ex, "Copilot API request failed"); }

        if (err is not null || response is null)
        {
            yield return new ChatResponseUpdate { Contents = [new TextContent("Error: AI service unavailable.")] };
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;
            if (!line.StartsWith("data: ")) continue;
            var data = line[6..].Trim();
            if (data == "[DONE]") yield break;

            JsonElement chunk;
            try { chunk = JsonDocument.Parse(data).RootElement; } catch { continue; }

            if (!chunk.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0) continue;
            var choice = choices[0];
            if (!choice.TryGetProperty("delta", out var delta)) continue;

            if (delta.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
            {
                var text = content.GetString();
                if (!string.IsNullOrEmpty(text))
                    yield return new ChatResponseUpdate { Contents = [new TextContent(text)] };
            }
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }

    private object BuildRequestBody(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        var msgs = messages.Select(m => new
        {
            role = m.Role == ChatRole.System ? "system"
                 : m.Role == ChatRole.Assistant ? "assistant"
                 : "user",
            content = string.Concat(m.Contents.OfType<TextContent>().Select(c => c.Text))
        }).ToList();

        return new
        {
            model = _modelId,
            messages = msgs,
            stream = true,
            max_tokens = options?.MaxOutputTokens ?? 2000,
            temperature = options?.Temperature ?? 0.7
        };
    }
}
