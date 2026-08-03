using System.Text;
using System.Text.Json;
using GOpsHub.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.Alerting;

public class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly string? _discordWebhookUrl;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IConfiguration configuration,
        ILogger<NotificationService> logger)
    {
        _httpClient = new HttpClient();
        _discordWebhookUrl = configuration["Alerting:DiscordWebhookUrl"];
        _logger = logger;
    }

    public async Task SendNotificationAsync(string title, string message, string type = "info", CancellationToken ct = default)
    {
        _logger.LogInformation("Notification [{Type}]: {Title} - {Message}", type, title, message);

        // Discord Webhook Push (UC12 Multi-Channel Alerting)
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
    }
}
