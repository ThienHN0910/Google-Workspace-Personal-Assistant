using System.Text;
using System.Text.Json;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.Alerting;

/// <summary>
/// Long-polling background service that listens for inbound Telegram commands (/readmore, /status, /help)
/// without requiring public webhooks or ngrok.
/// </summary>
public class TelegramBotPollingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramBotPollingService> _logger;
    private readonly HttpClient _httpClient;

    public TelegramBotPollingService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<TelegramBotPollingService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(50) };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TelegramBotPollingService started.");

        long offset = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (botToken, allowedChatId) = await GetTelegramConfigAsync(stoppingToken);

                if (string.IsNullOrWhiteSpace(botToken))
                {
                    // No token configured, sleep and recheck later
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }

                var url = $"https://api.telegram.org/bot{botToken}/getUpdates?offset={offset}&timeout=25";
                var response = await _httpClient.GetAsync(url, stoppingToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Telegram getUpdates returned status {StatusCode}. Backing off...", response.StatusCode);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(stoppingToken);
                var updateResponse = JsonSerializer.Deserialize<TelegramUpdateResponse>(json);

                if (updateResponse?.ok == true && updateResponse.result != null)
                {
                    foreach (var update in updateResponse.result)
                    {
                        offset = update.update_id + 1;

                        if (update.message?.chat == null || string.IsNullOrWhiteSpace(update.message.text))
                            continue;

                        var chatId = update.message.chat.id.ToString();

                        // Security check: only reply to configured admin chatId (if configured)
                        if (!string.IsNullOrEmpty(allowedChatId) && !chatId.Equals(allowedChatId, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning("Received Telegram message from unauthorized chat_id: {ChatId}", chatId);
                            continue;
                        }

                        var text = update.message.text.Trim();
                        await HandleCommandAsync(botToken, chatId, text, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in TelegramBotPollingService loop. Retrying in 5 seconds...");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("TelegramBotPollingService stopped.");
    }

    private async Task HandleCommandAsync(string botToken, string chatId, string text, CancellationToken ct)
    {
        if (text.StartsWith("/readmore", StringComparison.OrdinalIgnoreCase))
        {
            await ProcessReadMoreCommandAsync(botToken, chatId, ct);
        }
        else if (text.StartsWith("/rules", StringComparison.OrdinalIgnoreCase))
        {
            await ProcessRulesCommandAsync(botToken, chatId, ct);
        }
        else if (text.StartsWith("/enable_rule", StringComparison.OrdinalIgnoreCase))
        {
            await ProcessToggleRuleCommandAsync(botToken, chatId, text, enable: true, ct);
        }
        else if (text.StartsWith("/disable_rule", StringComparison.OrdinalIgnoreCase))
        {
            await ProcessToggleRuleCommandAsync(botToken, chatId, text, enable: false, ct);
        }
        else if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase) || text.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
        {
            var helpMsg = "🤖 <b>G-Ops Hub Assistant Bot</b>\n\n" +
                          "Các lệnh khả dụng:\n" +
                          "• <code>/readmore</code> — Xem chi tiết phiên dọn dẹp email gần nhất\n" +
                          "• <code>/rules</code> — Xem danh sách các quy tắc dọn dẹp\n" +
                          "• <code>/enable_rule &lt;id&gt;</code> — Bật quy tắc dọn dẹp\n" +
                          "• <code>/disable_rule &lt;id&gt;</code> — Tắt quy tắc dọn dẹp\n" +
                          "• <code>/status</code> — Kiểm tra trạng thái hệ thống";
            await SendTelegramMessageAsync(botToken, chatId, helpMsg, ct);
        }
        else if (text.StartsWith("/status", StringComparison.OrdinalIgnoreCase))
        {
            var statusMsg = $"✅ <b>G-Ops Hub đang hoạt động bình thường</b>\n" +
                            $"• Thời gian máy chủ: <code>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</code>\n" +
                            $"• Telegram Polling: Kết nối ổn định";
            await SendTelegramMessageAsync(botToken, chatId, statusMsg, ct);
        }
    }

    private async Task ProcessReadMoreCommandAsync(string botToken, string chatId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var actionLogRepo = scope.ServiceProvider.GetRequiredService<IRepository<EmailActionLog>>();
        var cleanupLogRepo = scope.ServiceProvider.GetRequiredService<IRepository<CleanupLog>>();

        // 1. Find the most recent cleanup session
        var recentCleanup = (await cleanupLogRepo.FindAsync(_ => true, ct))
            .OrderByDescending(c => c.ExecutedAt)
            .FirstOrDefault();

        string responseText;

        if (recentCleanup != null && !string.IsNullOrEmpty(recentCleanup.SessionId))
        {
            // Find action logs matching this session
            var sessionLogs = (await actionLogRepo.FindAsync(l => l.SessionId == recentCleanup.SessionId, ct))
                .OrderByDescending(l => l.ExecutedAt)
                .ToList();

            if (!sessionLogs.Any())
            {
                // Fallback to logs around execution time
                sessionLogs = (await actionLogRepo.FindAsync(l => l.SourceJob == "EmailCleanup" || l.SourceJob == "ManualCleanup", ct))
                    .OrderByDescending(l => l.ExecutedAt)
                    .Take(15)
                    .ToList();
            }

            responseText = FormatReadMoreResponse(sessionLogs, recentCleanup.ExecutedAt, recentCleanup.TotalProcessed);
        }
        else
        {
            // Fallback: take latest 15 cleanup action logs
            var latestLogs = (await actionLogRepo.FindAsync(l => l.SourceJob == "EmailCleanup" || l.SourceJob == "ManualCleanup", ct))
                .OrderByDescending(l => l.ExecutedAt)
                .Take(15)
                .ToList();

            if (latestLogs.Any())
            {
                var latestTime = latestLogs.First().ExecutedAt;
                responseText = FormatReadMoreResponse(latestLogs, latestTime, latestLogs.Count);
            }
            else
            {
                responseText = "ℹ️ Không tìm thấy nhật ký phiên dọn dẹp nào gần đây.";
            }
        }

        await SendTelegramMessageAsync(botToken, chatId, responseText, ct);
    }

    public static string FormatReadMoreResponse(IReadOnlyList<EmailActionLog> logs, DateTime executedAt, int totalProcessed)
    {
        if (logs == null || logs.Count == 0)
        {
            return "ℹ️ Không có email nào được dọn dẹp trong phiên này.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"📋 <b>Chi tiết phiên dọn dẹp [{executedAt:dd/MM/yyyy HH:mm}]:</b>");
        sb.AppendLine();

        const int maxDisplay = 15;
        var displayLogs = logs.Take(maxDisplay).ToList();

        for (int i = 0; i < displayLogs.Count; i++)
        {
            var log = displayLogs[i];
            var actionTag = log.Action switch
            {
                "Trashed" => "🗑️ Xóa",
                "Archived" => "📦 Lưu trữ",
                "PendingApproval" => "⏳ Chờ duyệt",
                _ => log.Action
            };

            var matchSource = log.Reason.Contains("AiPatternMatched") ? "AI" : "Regex";

            var sender = !string.IsNullOrWhiteSpace(log.Sender) ? log.Sender : "Không rõ người gửi";
            var subject = !string.IsNullOrWhiteSpace(log.Subject) ? log.Subject : "(Không có tiêu đề)";

            // Truncate long subjects for readability
            if (subject.Length > 45)
                subject = subject[..42] + "...";

            sb.AppendLine($"{i + 1}. [{actionTag} - {matchSource}] <code>{EscapeTelegramHtml(sender)}</code>\n   ↳ <i>{EscapeTelegramHtml(subject)}</i>");
        }

        if (logs.Count > maxDisplay)
        {
            sb.AppendLine();
            sb.AppendLine($"<i>... và <b>{logs.Count - maxDisplay}</b> email khác đã được xử lý.</i>");
        }

        return sb.ToString().TrimEnd();
    }

    private async Task ProcessRulesCommandAsync(string botToken, string chatId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var ruleRepo = scope.ServiceProvider.GetRequiredService<IRepository<CleanupRule>>();

        var rules = (await ruleRepo.GetAllAsync(ct))
            .OrderByDescending(r => r.IsAutoLearned)
            .ThenByDescending(r => r.IsActive)
            .ToList();

        var responseText = FormatRulesResponse(rules);
        await SendTelegramMessageAsync(botToken, chatId, responseText, ct);
    }

    private async Task ProcessToggleRuleCommandAsync(string botToken, string chatId, string text, bool enable, CancellationToken ct)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            var cmdName = enable ? "/enable_rule" : "/disable_rule";
            await SendTelegramMessageAsync(botToken, chatId, $"⚠️ Vui lòng cung cấp ID quy tắc.\nVí dụ: <code>{cmdName} 65fa1234abcd</code>", ct);
            return;
        }

        var ruleId = parts[1].Trim();
        using var scope = _serviceProvider.CreateScope();
        var ruleRepo = scope.ServiceProvider.GetRequiredService<IRepository<CleanupRule>>();

        var rule = await ruleRepo.GetByIdAsync(ruleId, ct);
        if (rule == null)
        {
            await SendTelegramMessageAsync(botToken, chatId, $"⚠️ Không tìm thấy quy tắc nào có ID <code>{EscapeTelegramHtml(ruleId)}</code>.", ct);
            return;
        }

        rule.IsActive = enable;
        await ruleRepo.UpdateAsync(rule, ct);

        var statusText = enable
            ? $"✅ Đã kích hoạt quy tắc <b>{EscapeTelegramHtml(rule.RuleName)}</b>!\nTừ các phiên dọn dẹp tiếp theo, quy tắc này sẽ được áp dụng tự động."
            : $"⏸️ Đã tạm dừng quy tắc <b>{EscapeTelegramHtml(rule.RuleName)}</b>.";

        await SendTelegramMessageAsync(botToken, chatId, statusText, ct);
    }

    public static string FormatRulesResponse(IReadOnlyList<CleanupRule> rules)
    {
        if (rules == null || rules.Count == 0)
        {
            return "ℹ️ Hiện chưa có quy tắc dọn dẹp nào được cấu hình.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("📜 <b>Danh sách Quy tắc Dọn dẹp Email:</b>");
        sb.AppendLine();

        int index = 1;
        const int maxDisplay = 15;
        var displayRules = rules.Take(maxDisplay).ToList();

        foreach (var rule in displayRules)
        {
            var statusIcon = rule.IsActive ? "✅ Bật" : "⏸️ Tắt";
            var actionText = rule.Action == Domain.Enums.CleanupAction.Trash ? "Xóa" : "Lưu trữ";
            var learnedTag = rule.IsAutoLearned ? " [AI Học]" : "";
            sb.AppendLine($"{index++}. <b>{EscapeTelegramHtml(rule.RuleName)}</b>{learnedTag}");
            sb.AppendLine($"   • Trạng thái: <b>{statusIcon}</b> | Hành động: <b>{actionText}</b>");
            if (!string.IsNullOrWhiteSpace(rule.SubjectRegex))
                sb.AppendLine($"   • Regex Tiêu đề: <code>{EscapeTelegramHtml(rule.SubjectRegex)}</code>");
            if (!string.IsNullOrWhiteSpace(rule.SenderRegex))
                sb.AppendLine($"   • Regex Người gửi: <code>{EscapeTelegramHtml(rule.SenderRegex)}</code>");
            sb.AppendLine($"   • ID: <code>{rule.Id}</code>");
            if (!rule.IsActive)
                sb.AppendLine($"   👉 <i>Bật nhanh:</i> <code>/enable_rule {rule.Id}</code>");
            sb.AppendLine();
        }

        if (rules.Count > maxDisplay)
        {
            sb.AppendLine($"<i>... và còn {rules.Count - maxDisplay} quy tắc khác trên Dashboard.</i>");
        }

        return sb.ToString().TrimEnd();
    }

    public static string EscapeTelegramHtml(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private async Task SendTelegramMessageAsync(string botToken, string chatId, string text, CancellationToken ct)
    {
        try
        {
            var payload = new
            {
                chat_id = chatId,
                text = text,
                parse_mode = "HTML"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            await _httpClient.PostAsync(url, content, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send response message to Telegram chat {ChatId}.", chatId);
        }
    }

    private async Task<(string? botToken, string? allowedChatId)> GetTelegramConfigAsync(CancellationToken ct)
    {
        string? botToken = _configuration["Telegram:BotToken"] ?? _configuration["TELEGRAM_BOT_TOKEN"];
        string? chatId = _configuration["Telegram:ChatId"] ?? _configuration["TELEGRAM_CHAT_ID"];

        using var scope = _serviceProvider.CreateScope();
        var configRepo = scope.ServiceProvider.GetService<IRepository<AppConfiguration>>();

        if (configRepo != null)
        {
            try
            {
                var tokenConf = await configRepo.FindOneAsync(c => c.Key == "TelegramBotToken", ct);
                if (!string.IsNullOrWhiteSpace(tokenConf?.Value))
                    botToken = tokenConf.Value;

                var chatConf = await configRepo.FindOneAsync(c => c.Key == "TelegramChatId", ct);
                if (!string.IsNullOrWhiteSpace(chatConf?.Value))
                    chatId = chatConf.Value;
            }
            catch
            {
                // Fallback to env config
            }
        }

        return (botToken, chatId);
    }
}

// Telegram Update Models
public class TelegramUpdateResponse
{
    public bool ok { get; set; }
    public List<TelegramUpdate>? result { get; set; }
}

public class TelegramUpdate
{
    public long update_id { get; set; }
    public TelegramMessage? message { get; set; }
}

public class TelegramMessage
{
    public long message_id { get; set; }
    public TelegramChat? chat { get; set; }
    public string? text { get; set; }
}

public class TelegramChat
{
    public long id { get; set; }
}
