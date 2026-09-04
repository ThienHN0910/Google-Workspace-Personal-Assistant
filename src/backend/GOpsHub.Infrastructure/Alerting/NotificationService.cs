using System.Text;
using System.Text.Json;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
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
    private readonly IRepository<AppConfiguration>? _configRepo;
    private readonly ILogger<NotificationService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(
        IConfiguration configuration,
        ILogger<NotificationService> logger,
        IHubContext<NotificationHub> hubContext,
        IRepository<AppConfiguration>? configRepo = null)
    {
        _httpClient = new HttpClient();
        _discordWebhookUrl = configuration["Alerting:DiscordWebhookUrl"] 
            ?? configuration["ALERTING__DISCORDWEBHOOKURL"] 
            ?? configuration["ALERTING_DISCORD_WEBHOOK_URL"];
        _telegramBotToken = configuration["Telegram:BotToken"] ?? configuration["TELEGRAM_BOT_TOKEN"];
        _telegramChatId = configuration["Telegram:ChatId"] ?? configuration["TELEGRAM_CHAT_ID"];
        _logger = logger;
        _hubContext = hubContext;
        _configRepo = configRepo;
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

        // 2. Resolve Dynamic Settings from Database (if available) with fallback to env
        string? discordWebhook = _discordWebhookUrl;
        string? tgBotToken = _telegramBotToken;
        string? tgChatId = _telegramChatId;
        bool enableTelegram = true;
        bool enableDiscord = true;

        if (_configRepo != null)
        {
            try
            {
                var etConf = await _configRepo.FindOneAsync(c => c.Key == "EnableTelegram", ct);
                if (etConf != null && bool.TryParse(etConf.Value, out var et))
                    enableTelegram = et;

                var tTokenConf = await _configRepo.FindOneAsync(c => c.Key == "TelegramBotToken", ct);
                if (!string.IsNullOrWhiteSpace(tTokenConf?.Value))
                    tgBotToken = tTokenConf.Value;

                var tChatConf = await _configRepo.FindOneAsync(c => c.Key == "TelegramChatId", ct);
                if (!string.IsNullOrWhiteSpace(tChatConf?.Value))
                    tgChatId = tChatConf.Value;

                var edConf = await _configRepo.FindOneAsync(c => c.Key == "EnableDiscord", ct);
                if (edConf != null && bool.TryParse(edConf.Value, out var ed))
                    enableDiscord = ed;

                var dWebConf = await _configRepo.FindOneAsync(c => c.Key == "DiscordWebhookUrl", ct);
                if (!string.IsNullOrWhiteSpace(dWebConf?.Value))
                    discordWebhook = dWebConf.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load dynamic notification settings from AppConfiguration. Using defaults.");
            }
        }

        // 3. Discord Webhook Push (UC12 Multi-Channel Alerting)
        if (enableDiscord && !string.IsNullOrEmpty(discordWebhook))
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
                await _httpClient.PostAsync(discordWebhook, content, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Discord webhook notification.");
            }
        }

        // 4. Telegram Bot Push (Multi-Channel Alerting - Background Real-Time)
        if (enableTelegram && !string.IsNullOrEmpty(tgBotToken) && !string.IsNullOrEmpty(tgChatId))
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
                    chat_id = tgChatId,
                    text = telegramText,
                    parse_mode = "HTML"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var tgUrl = $"https://api.telegram.org/bot{tgBotToken}/sendMessage";
                await _httpClient.PostAsync(tgUrl, content, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram notification.");
            }
        }
    }
}
