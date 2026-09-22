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
    public string GeminiModel { get; set; } = "gemini-3.5-flash-lite";
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

    // 5. Duy trì hoạt động máy chủ (Keep-Alive cho MonsterASP / Free Tier)
    public string? KeepAliveKey { get; set; }
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
            ?? "gemini-3.5-flash-lite";

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

        // 5. Keep-Alive
        dto.KeepAliveKey = configMap.GetValueOrDefault("KeepAliveKey")
            ?? _configuration["KEEP_ALIVE_KEY"]
            ?? _configuration["KeepAlive:Key"];

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

            ["GeminiModel"] = string.IsNullOrWhiteSpace(s.GeminiModel) ? "gemini-3.5-flash-lite" : s.GeminiModel.Trim(),
            ["DefaultLanguage"] = s.DefaultLanguage ?? "vi",
            ["DefaultTone"] = s.DefaultTone ?? "polite",
            ["AiMonthlyTokenQuota"] = s.AiMonthlyTokenQuota.ToString(),
            ["AiWarningTokenThreshold"] = s.AiWarningTokenThreshold.ToString(),

            ["Finance_FolderId"] = s.FinanceFolderId ?? string.Empty,
            ["Finance_SpreadsheetId"] = s.FinanceSpreadsheetId ?? string.Empty,
            ["Finance_FileNamePattern"] = string.IsNullOrWhiteSpace(s.FinanceFileNamePattern) ? "BaoCaoTaiChinh_{yyyy_MM}" : s.FinanceFileNamePattern,
            ["EmailWhitelistDomains"] = JsonSerializer.Serialize(s.EmailWhitelistDomains ?? new()),

            ["KeepAliveKey"] = s.KeepAliveKey ?? string.Empty
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
        HangfireJobRescheduler.RescheduleJobs(_recurringJobManager, s, _logger);

        return true;
    }
}

public static class HangfireJobRescheduler
{
    public static void RescheduleJobs(IRecurringJobManager recurringJobManager, SystemSettingsDto s, ILogger logger)
    {
        try
        {
            var driveCron = GOpsHub.Application.Common.CronScheduleHelper.FromMinutes(s.DriveGuardIntervalMinutes, defaultMinutes: 50);
            var bankCron = GOpsHub.Application.Common.CronScheduleHelper.FromMinutes(s.BankTelemetryIntervalMinutes, defaultMinutes: 30);
            var emailCron = GOpsHub.Application.Common.CronScheduleHelper.FromHours(s.EmailCleanupIntervalHours, defaultHours: 12);
            var calCron = GOpsHub.Application.Common.CronScheduleHelper.FromHours(s.CalendarExtractorIntervalHours, defaultHours: 2);

            recurringJobManager.AddOrUpdate<DriveGuardBackgroundJob>(
                "drive-guard-audit",
                job => job.RunAuditAsync(CancellationToken.None),
                driveCron);

            recurringJobManager.AddOrUpdate<BankTelemetryBackgroundJob>(
                "bank-telemetry",
                job => job.RunTelemetryAsync(CancellationToken.None),
                bankCron);

            recurringJobManager.AddOrUpdate<EmailCleanupBackgroundJob>(
                "email-cleanup",
                job => job.RunAutoCleanupAsync(CancellationToken.None),
                emailCron);

            recurringJobManager.AddOrUpdate<CalendarScheduleBackgroundJob>(
                "calendar-extractor",
                job => job.RunScheduleExtractionAsync(CancellationToken.None),
                calCron);

            logger.LogInformation("Successfully rescheduled all Hangfire background jobs with new intervals.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reschedule Hangfire jobs.");
        }
    }
}

public record UpdateSettingsSectionCommand(string Section, SystemSettingsDto Settings) : ICommand<bool>;

public class UpdateSettingsSectionCommandHandler : ICommandHandler<UpdateSettingsSectionCommand, bool>
{
    private readonly IRepository<AppConfiguration> _configRepo;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<UpdateSettingsSectionCommandHandler> _logger;

    public UpdateSettingsSectionCommandHandler(
        IRepository<AppConfiguration> configRepo,
        IRecurringJobManager recurringJobManager,
        ILogger<UpdateSettingsSectionCommandHandler> logger)
    {
        _configRepo = configRepo;
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(UpdateSettingsSectionCommand command, CancellationToken ct = default)
    {
        var s = command.Settings;
        var section = (command.Section ?? string.Empty).Trim().ToLowerInvariant();

        var keyValues = new Dictionary<string, string>();

        switch (section)
        {
            case "jobs":
            case "intervals":
                keyValues["DriveGuardIntervalMinutes"] = s.DriveGuardIntervalMinutes.ToString();
                keyValues["DriveGuardInterval"] = s.DriveGuardIntervalMinutes.ToString();
                keyValues["BankTelemetryIntervalMinutes"] = s.BankTelemetryIntervalMinutes.ToString();
                keyValues["EmailCleanupIntervalHours"] = s.EmailCleanupIntervalHours.ToString();
                keyValues["CalendarExtractorIntervalHours"] = s.CalendarExtractorIntervalHours.ToString();
                keyValues["BulkDeleteThreshold"] = s.BulkDeleteThreshold.ToString();
                break;

            case "alerts":
            case "alerting":
                keyValues["EnableTelegram"] = s.EnableTelegram.ToString();
                keyValues["TelegramBotToken"] = s.TelegramBotToken ?? string.Empty;
                keyValues["TelegramChatId"] = s.TelegramChatId ?? string.Empty;
                keyValues["EnableDiscord"] = s.EnableDiscord.ToString();
                keyValues["DiscordWebhookUrl"] = s.DiscordWebhookUrl ?? string.Empty;
                break;

            case "ai":
                keyValues["GeminiModel"] = string.IsNullOrWhiteSpace(s.GeminiModel) ? "gemini-3.5-flash-lite" : s.GeminiModel.Trim();
                keyValues["DefaultLanguage"] = s.DefaultLanguage ?? "vi";
                keyValues["DefaultTone"] = s.DefaultTone ?? "polite";
                keyValues["AiMonthlyTokenQuota"] = s.AiMonthlyTokenQuota.ToString();
                keyValues["AiWarningTokenThreshold"] = s.AiWarningTokenThreshold.ToString();
                break;

            case "storage":
                keyValues["Finance_FolderId"] = s.FinanceFolderId ?? string.Empty;
                keyValues["Finance_SpreadsheetId"] = s.FinanceSpreadsheetId ?? string.Empty;
                keyValues["Finance_FileNamePattern"] = string.IsNullOrWhiteSpace(s.FinanceFileNamePattern) ? "BaoCaoTaiChinh_{yyyy_MM}" : s.FinanceFileNamePattern;
                keyValues["EmailWhitelistDomains"] = JsonSerializer.Serialize(s.EmailWhitelistDomains ?? new());
                break;

            case "keepalive":
                keyValues["KeepAliveKey"] = s.KeepAliveKey ?? string.Empty;
                break;

            default:
                throw new ArgumentException($"Section không hợp lệ: '{command.Section}'. Các giá trị hợp lệ: jobs, alerts, ai, storage, keepalive.");
        }

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

        // CHỈ cập nhật Hangfire khi thay đổi section jobs/intervals!
        if (section == "jobs" || section == "intervals")
        {
            HangfireJobRescheduler.RescheduleJobs(_recurringJobManager, s, _logger);
        }

        return true;
    }
}

public record TestGeminiModelCommand(string Model) : ICommand<TestGeminiModelResult>;

public class TestGeminiModelResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long ResponseTimeMs { get; set; }
}

public class TestGeminiModelCommandHandler : ICommandHandler<TestGeminiModelCommand, TestGeminiModelResult>
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public TestGeminiModelCommandHandler(IConfiguration configuration, HttpClient? httpClient = null)
    {
        _configuration = configuration;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<TestGeminiModelResult> HandleAsync(TestGeminiModelCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Model))
            throw new ArgumentException("Tên model không được để trống.");

        var modelName = command.Model.Trim();
        var apiKey = _configuration["Gemini:ApiKey"] ?? _configuration["GEMINI_API_KEY"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new TestGeminiModelResult
            {
                Success = false,
                Message = "Chưa cấu hình Gemini API Key trên hệ thống (biến GEMINI_API_KEY)."
            };
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = "ping" }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.PostAsync(url, content, ct);
            stopwatch.Stop();

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new TestGeminiModelResult
                {
                    Success = false,
                    Message = $"Mô hình '{modelName}' không tồn tại hoặc đã bị Google khai tử/chưa mở quyền truy cập (Lỗi HTTP 404). Vui lòng kiểm tra lại chính tả tên model.",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                return new TestGeminiModelResult
                {
                    Success = false,
                    Message = $"Google AI trả về mã lỗi HTTP {(int)response.StatusCode} ({response.StatusCode}): {errorBody}",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            return new TestGeminiModelResult
            {
                Success = true,
                Message = $"Kết nối thành công tới mô hình '{modelName}' (Thời gian phản hồi: {stopwatch.ElapsedMilliseconds}ms)!",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new TestGeminiModelResult
            {
                Success = false,
                Message = $"Lỗi kết nối khi gọi thử nghiệm tới model '{modelName}': {ex.Message}",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
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
