using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.DriveGuard;
using GOpsHub.Application.Features.EmailOps;
using GOpsHub.Application.Features.Finance;
using GOpsHub.Application.Features.Scheduling;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GOpsHub.Application.Features.Settings;

public class SystemSettingsDto
{
    // 1. Chu kỳ tác vụ ngầm & Ngưỡng an ninh
    public int DriveGuardIntervalMinutes { get; set; } = 50;
    public int BankTelemetryIntervalMinutes { get; set; } = 30;
    public int EmailCleanupIntervalHours { get; set; } = 12;
    public int CalendarExtractorIntervalHours { get; set; } = 2;
    public int BulkDeleteThreshold { get; set; } = 3;

    // 2. Kênh thông báo & Cảnh báo
    public bool EnableTelegram { get; set; } = true;
    public string? TelegramBotToken { get; set; }
    public string? TelegramChatId { get; set; }
    public bool EnableDiscord { get; set; } = true;
    public string? DiscordWebhookUrl { get; set; }

    // 3. Trợ lý AI & Quota
    public string GeminiModel { get; set; } = "gemini-3.1-flash-lite";
    public string DefaultLanguage { get; set; } = "vi";
    public string DefaultTone { get; set; } = "polite";
    public int MaxRequestsPerMinute { get; set; } = 15;
    public int MaxRequestsPerDay { get; set; } = 500;
    public long AiMonthlyTokenQuota { get; set; } = 250_000;
    public long AiWarningTokenThreshold { get; set; } = 200_000;

    // 4. Lưu trữ Drive & Whitelist Email
    public string? FinanceFolderId { get; set; }
    public string? FinanceSpreadsheetId { get; set; }
    public string FinanceFileNamePattern { get; set; } = "BaoCaoTaiChinh_{yyyy_MM}";
    public List<string> EmailWhitelistDomains { get; set; } = new();
}

public record GetSystemSettingsQuery : IQuery<SystemSettingsDto>;

public class GetSystemSettingsQueryHandler : IQueryHandler<GetSystemSettingsQuery, SystemSettingsDto>
{
    private readonly IRepository<AppConfiguration> _configRepo;
    private readonly IConfiguration _configuration;

    public GetSystemSettingsQueryHandler(
        IRepository<AppConfiguration> configRepo,
        IConfiguration configuration)
    {
        _configRepo = configRepo;
        _configuration = configuration;
    }

    public async Task<SystemSettingsDto> HandleAsync(GetSystemSettingsQuery query, CancellationToken ct = default)
    {
        var configs = await _configRepo.GetAllAsync(ct);
        var configMap = configs.ToDictionary(c => c.Key, c => c.Value);

        var dto = new SystemSettingsDto();

        // 1. Intervals
        if (configMap.TryGetValue("DriveGuardIntervalMinutes", out var dgMin) && int.TryParse(dgMin, out var dgVal))
            dto.DriveGuardIntervalMinutes = Math.Clamp(dgVal, 1, 1440);
        else if (configMap.TryGetValue("DriveGuardInterval", out var oldDg) && int.TryParse(oldDg, out var oldDgVal))
            dto.DriveGuardIntervalMinutes = Math.Clamp(oldDgVal, 1, 1440);

        if (configMap.TryGetValue("BankTelemetryIntervalMinutes", out var btMin) && int.TryParse(btMin, out var btVal))
            dto.BankTelemetryIntervalMinutes = Math.Clamp(btVal, 1, 1440);

        if (configMap.TryGetValue("EmailCleanupIntervalHours", out var ecHr) && int.TryParse(ecHr, out var ecVal))
            dto.EmailCleanupIntervalHours = Math.Clamp(ecVal, 1, 168);

        if (configMap.TryGetValue("CalendarExtractorIntervalHours", out var ceHr) && int.TryParse(ceHr, out var ceVal))
            dto.CalendarExtractorIntervalHours = Math.Clamp(ceVal, 1, 168);

        if (configMap.TryGetValue("BulkDeleteThreshold", out var bdt) && int.TryParse(bdt, out var bdtVal))
            dto.BulkDeleteThreshold = Math.Max(1, bdtVal);

        // 2. Alerting
        if (configMap.TryGetValue("EnableTelegram", out var et) && bool.TryParse(et, out var etVal))
            dto.EnableTelegram = etVal;

        dto.TelegramBotToken = configMap.GetValueOrDefault("TelegramBotToken")
            ?? _configuration["Telegram:BotToken"]
            ?? _configuration["TELEGRAM_BOT_TOKEN"];

        dto.TelegramChatId = configMap.GetValueOrDefault("TelegramChatId")
            ?? _configuration["Telegram:ChatId"]
            ?? _configuration["TELEGRAM_CHAT_ID"];

        if (configMap.TryGetValue("EnableDiscord", out var ed) && bool.TryParse(ed, out var edVal))
            dto.EnableDiscord = edVal;

        dto.DiscordWebhookUrl = configMap.GetValueOrDefault("DiscordWebhookUrl")
            ?? _configuration["Alerting:DiscordWebhookUrl"]
            ?? _configuration["ALERTING_DISCORD_WEBHOOK_URL"];

        // 3. AI
        dto.GeminiModel = configMap.GetValueOrDefault("GeminiModel")
            ?? _configuration["Gemini:Model"]
            ?? _configuration["GEMINI_MODEL"]
            ?? "gemini-3.1-flash-lite";

        dto.DefaultLanguage = configMap.GetValueOrDefault("DefaultLanguage") ?? "vi";
        dto.DefaultTone = configMap.GetValueOrDefault("DefaultTone") ?? "polite";

        if (configMap.TryGetValue("AiMonthlyTokenQuota", out var amtq) && long.TryParse(amtq, out var amtqVal))
            dto.AiMonthlyTokenQuota = Math.Max(1000, amtqVal);

        if (configMap.TryGetValue("AiWarningTokenThreshold", out var awtt) && long.TryParse(awtt, out var awttVal))
            dto.AiWarningTokenThreshold = Math.Max(1000, awttVal);

        // 4. Drive & Finance
        dto.FinanceFolderId = configMap.GetValueOrDefault("Finance_FolderId");
        dto.FinanceSpreadsheetId = configMap.GetValueOrDefault("Finance_SpreadsheetId");
        dto.FinanceFileNamePattern = configMap.GetValueOrDefault("Finance_FileNamePattern") ?? "BaoCaoTaiChinh_{yyyy_MM}";

        if (configMap.TryGetValue("EmailWhitelistDomains", out var domainsJson) && !string.IsNullOrWhiteSpace(domainsJson))
        {
            try
            {
                dto.EmailWhitelistDomains = JsonSerializer.Deserialize<List<string>>(domainsJson) ?? new();
            }
            catch
            {
                dto.EmailWhitelistDomains = domainsJson.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }
        }

        return dto;
    }
}

public record UpdateSystemSettingsCommand(SystemSettingsDto Settings) : ICommand<bool>;

public class UpdateSystemSettingsCommandHandler : ICommandHandler<UpdateSystemSettingsCommand, bool>
{
    private readonly IRepository<AppConfiguration> _configRepo;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<UpdateSystemSettingsCommandHandler> _logger;

    public UpdateSystemSettingsCommandHandler(
        IRepository<AppConfiguration> configRepo,
        IRecurringJobManager recurringJobManager,
        ILogger<UpdateSystemSettingsCommandHandler> logger)
    {
        _configRepo = configRepo;
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(UpdateSystemSettingsCommand command, CancellationToken ct = default)
    {
        var s = command.Settings;

        var keyValues = new Dictionary<string, string>
        {
            ["DriveGuardIntervalMinutes"] = s.DriveGuardIntervalMinutes.ToString(),
            ["DriveGuardInterval"] = s.DriveGuardIntervalMinutes.ToString(),
            ["BankTelemetryIntervalMinutes"] = s.BankTelemetryIntervalMinutes.ToString(),
            ["EmailCleanupIntervalHours"] = s.EmailCleanupIntervalHours.ToString(),
            ["CalendarExtractorIntervalHours"] = s.CalendarExtractorIntervalHours.ToString(),
            ["BulkDeleteThreshold"] = s.BulkDeleteThreshold.ToString(),

            ["EnableTelegram"] = s.EnableTelegram.ToString(),
            ["TelegramBotToken"] = s.TelegramBotToken ?? string.Empty,
            ["TelegramChatId"] = s.TelegramChatId ?? string.Empty,
            ["EnableDiscord"] = s.EnableDiscord.ToString(),
            ["DiscordWebhookUrl"] = s.DiscordWebhookUrl ?? string.Empty,

            ["GeminiModel"] = s.GeminiModel ?? "gemini-3.1-flash-lite",
            ["DefaultLanguage"] = s.DefaultLanguage ?? "vi",
            ["DefaultTone"] = s.DefaultTone ?? "polite",
            ["AiMonthlyTokenQuota"] = s.AiMonthlyTokenQuota.ToString(),
            ["AiWarningTokenThreshold"] = s.AiWarningTokenThreshold.ToString(),

            ["Finance_FolderId"] = s.FinanceFolderId ?? string.Empty,
            ["Finance_SpreadsheetId"] = s.FinanceSpreadsheetId ?? string.Empty,
            ["Finance_FileNamePattern"] = string.IsNullOrWhiteSpace(s.FinanceFileNamePattern) ? "BaoCaoTaiChinh_{yyyy_MM}" : s.FinanceFileNamePattern,
            ["EmailWhitelistDomains"] = JsonSerializer.Serialize(s.EmailWhitelistDomains ?? new())
        };

        foreach (var (key, value) in keyValues)
        {
            var existing = await _configRepo.FindOneAsync(c => c.Key == key, ct);
            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
                await _configRepo.UpdateAsync(existing, ct);
            }
            else
            {
                await _configRepo.CreateAsync(new AppConfiguration
                {
                    Key = key,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow
                }, ct);
            }
        }

        // Dynamic Hangfire Rescheduling (Không cần restart server!)
        try
        {
            _recurringJobManager.AddOrUpdate<DriveGuardBackgroundJob>(
                "drive-guard-audit",
                job => job.RunAuditAsync(CancellationToken.None),
                $"*/{Math.Max(1, s.DriveGuardIntervalMinutes)} * * * *");

            _recurringJobManager.AddOrUpdate<BankTelemetryBackgroundJob>(
                "bank-telemetry",
                job => job.RunTelemetryAsync(CancellationToken.None),
                $"*/{Math.Max(1, s.BankTelemetryIntervalMinutes)} * * * *");

            _recurringJobManager.AddOrUpdate<EmailCleanupBackgroundJob>(
                "email-cleanup",
                job => job.RunAutoCleanupAsync(CancellationToken.None),
                $"0 */{Math.Max(1, s.EmailCleanupIntervalHours)} * * *");

            _recurringJobManager.AddOrUpdate<CalendarScheduleBackgroundJob>(
                "calendar-extractor",
                job => job.RunScheduleExtractionAsync(CancellationToken.None),
                $"0 */{Math.Max(1, s.CalendarExtractorIntervalHours)} * * *");

            _logger.LogInformation("Successfully rescheduled all Hangfire background jobs with new intervals.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reschedule Hangfire jobs.");
        }

        return true;
    }
}

public record TestTelegramConnectionCommand(string BotToken, string ChatId) : ICommand<bool>;

public class TestTelegramConnectionCommandHandler : ICommandHandler<TestTelegramConnectionCommand, bool>
{
    public async Task<bool> HandleAsync(TestTelegramConnectionCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.BotToken) || string.IsNullOrWhiteSpace(command.ChatId))
            throw new ArgumentException("Bot Token và Chat ID không được để trống.");

        using var client = new HttpClient();
        var url = $"https://api.telegram.org/bot{command.BotToken}/sendMessage";
        var payload = new
        {
            chat_id = command.ChatId,
            text = $"⚡ <b>G-Ops Hub Test Ping</b>\n\nKết nối thành công! Bạn vừa bấm thử nghiệm từ trang Cài đặt hệ thống.\n\n<i>Thời gian: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</i>",
            parse_mode = "HTML"
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Lỗi kết nối Telegram ({response.StatusCode}): {err}");
        }

        return true;
    }
}
