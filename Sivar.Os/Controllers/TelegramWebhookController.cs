using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Sivar.Os.Services;
using Sivar.Os.Configuration;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sivar.Os.Controllers;

/// <summary>
/// Handles incoming Telegram webhook updates.
/// Maps Telegram user → Sivar agent session → AgentFactory → reply.
/// No Keycloak required: Telegram user ID is used as the identity key.
/// </summary>
[ApiController]
[Route("webhooks/telegram")]
public class TelegramWebhookController : ControllerBase
{
    private readonly IAgentFactory _agentFactory;
    private readonly IHttpClientFactory _httpFactory;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        IAgentFactory agentFactory,
        IHttpClientFactory httpFactory,
        IOptions<TelegramOptions> options,
        ILogger<TelegramWebhookController> logger)
    {
        _agentFactory = agentFactory;
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Handle([FromBody] TelegramUpdate update, CancellationToken ct)
    {
        var message = update.Message ?? update.EditedMessage;
        if (message?.Text is null)
            return Ok(); // ignore non-text updates

        var chatId = message.Chat.Id;
        var userId = message.From?.Id ?? chatId;
        var username = message.From?.Username ?? message.From?.FirstName ?? userId.ToString();
        var text = message.Text;

        _logger.LogInformation("Telegram message from {Username} ({UserId}): {Text}", username, userId, text);

        try
        {
            var agent = await _agentFactory.GetAgentForIntentAsync(text);

            var history = new List<ChatMessage>
            {
                new(ChatRole.User, text)
            };

            var agentResponse = await agent.RunAsync(history);
            var replyText = agentResponse?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(replyText))
                replyText = "⚠️ No response from agent.";

            await SendTelegramMessageAsync(chatId, replyText, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Telegram message");
            await SendTelegramMessageAsync(chatId, "❌ Something went wrong. Please try again.", ct);
        }

        return Ok();
    }

    private async Task SendTelegramMessageAsync(long chatId, string text, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient();
        var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";

        // Telegram messages max 4096 chars
        if (text.Length > 4096) text = text[..4090] + "\n...";

        var payload = new { chat_id = chatId, text, parse_mode = "Markdown" };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var resp = await http.PostAsync(url, content, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Telegram sendMessage failed: {Status} {Error}", resp.StatusCode, err);

            // Retry without markdown if parse error
            if (err.Contains("parse") || err.Contains("markdown"))
            {
                var plain = new { chat_id = chatId, text };
                await http.PostAsync(url, new StringContent(JsonSerializer.Serialize(plain), Encoding.UTF8, "application/json"), ct);
            }
        }
    }
}

// --- Telegram update models ---
public class TelegramUpdate
{
    [JsonPropertyName("update_id")] public long UpdateId { get; set; }
    [JsonPropertyName("message")] public TelegramMessage? Message { get; set; }
    [JsonPropertyName("edited_message")] public TelegramMessage? EditedMessage { get; set; }
}

public class TelegramMessage
{
    [JsonPropertyName("message_id")] public long MessageId { get; set; }
    [JsonPropertyName("from")] public TelegramUser? From { get; set; }
    [JsonPropertyName("chat")] public TelegramChat Chat { get; set; } = null!;
    [JsonPropertyName("text")] public string? Text { get; set; }
}

public class TelegramUser
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("username")] public string? Username { get; set; }
    [JsonPropertyName("first_name")] public string? FirstName { get; set; }
}

public class TelegramChat
{
    [JsonPropertyName("id")] public long Id { get; set; }
}
