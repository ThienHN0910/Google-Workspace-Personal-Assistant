using System.Text;
using System.Text.Json;
using GOpsHub.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.Alerting;

public class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly string? _discordWebhookUrl;
    private readonly string? _telegramBotToken;
    private readonly string? _telegramChatId;
    private readonly ILogger<NotificationService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(
        IConfiguration configuration,
        ILogger<NotificationService> logger,
        IHubContext<NotificationHub> hubContext)
    {
        _httpClient = new HttpClient();
        _discordWebhookUrl = configuration["Alerting:DiscordWebhookUrl"] ?? configuration["ALERTING__DISCORDWEBHOOKURL"];
        _telegramBotToken = configuration["Telegram:BotToken"] ?? configuration["TELEGRAM_BOT_TOKEN"];
        _telegramChatId = configuration["Telegram:ChatId"] ?? configuration["TELEGRAM_CHAT_ID"];
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(string title, string message, string type = "info", CancellationToken ct = default)
    {
        _logger.LogInformation("Notification [{Type}]: {Title} - {Message}", type, title, message);

        // 1. Broadcast via SignalR to all connected frontend clients
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                title,
                message,
                type,
                timestamp = DateTime.UtcNow
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast SignalR notification.");
        }

        // 2. Discord Webhook Push (UC12 Multi-Channel Alerting)
        if (!string.IsNullOrEmpty(_discordWebhookUrl))
        {
            try
            {
                var color = type switch
                {
                    "critical" => 15158332, // Red
                    "warning" => 15105570,  // Orange
                    _ => 3066993             // Green
                };

                var payload = new
                {
                    username = "G-Ops Hub Alert Bot",
                    embeds = new[]
                    {
                        new
                        {
                            title = title,
                            description = message,
                            color = color,
                            timestamp = DateTime.UtcNow.ToString("o")
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(_discordWebhookUrl, content, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Discord webhook notification.");
            }
        }

        // 3. Telegram Bot Push (Multi-Channel Alerting)
        if (!string.IsNullOrEmpty(_telegramBotToken) && !string.IsNullOrEmpty(_telegramChatId))
        {
            try
            {
                var icon = type switch
                {
                    "critical" => "🚨",
                    "warning" => "⚠️",
                    _ => "ℹ️"
                };

                var telegramText = $"{icon} <b>{System.Net.WebUtility.HtmlEncode(title)}</b>\n\n{System.Net.WebUtility.HtmlEncode(message)}\n\n<i>G-Ops Hub • {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</i>";

                var payload = new
                {
                    chat_id = _telegramChatId,
                    text = telegramText,
                    parse_mode = "HTML"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var tgUrl = $"https://api.telegram.org/bot{_telegramBotToken}/sendMessage";
                await _httpClient.PostAsync(tgUrl, content, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram notification.");
            }
        }
    }
}
