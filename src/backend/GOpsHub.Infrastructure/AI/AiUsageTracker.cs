using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.AI;

public class AiUsageTracker : IAiUsageTracker
{
    private readonly IRepository<AiTokenUsageMonthly> _usageRepo;
    private readonly INotificationService _notificationService;
    private readonly IRepository<AppConfiguration>? _configRepo;
    private readonly ILogger<AiUsageTracker> _logger;

    public AiUsageTracker(
        IRepository<AiTokenUsageMonthly> usageRepo,
        INotificationService notificationService,
        ILogger<AiUsageTracker> logger,
        IRepository<AppConfiguration>? configRepo = null)
    {
        _usageRepo = usageRepo;
        _notificationService = notificationService;
        _logger = logger;
        _configRepo = configRepo;
    }

    public async Task<AiTokenUsageMonthly> GetCurrentMonthlyUsageAsync(CancellationToken ct = default)
    {
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var record = await _usageRepo.FindOneAsync(u => u.YearMonth == currentMonth, ct);

        long configuredQuota = 250_000;
        long configuredWarning = 200_000;

        if (_configRepo != null)
        {
            var quotaConfig = await _configRepo.FindOneAsync(c => c.Key == "AiMonthlyTokenQuota", ct);
            if (quotaConfig != null && long.TryParse(quotaConfig.Value, out var qVal) && qVal > 0)
                configuredQuota = qVal;

            var warnConfig = await _configRepo.FindOneAsync(c => c.Key == "AiWarningTokenThreshold", ct);
            if (warnConfig != null && long.TryParse(warnConfig.Value, out var wVal) && wVal > 0)
                configuredWarning = wVal;
        }

        if (record == null)
        {
            record = new AiTokenUsageMonthly
            {
                YearMonth = currentMonth,
                TotalTokens = 0,
                PromptTokens = 0,
                CandidatesTokens = 0,
                CallCount = 0,
                MonthlyQuotaLimit = configuredQuota,
                WarningThreshold = configuredWarning,
                FeatureBreakdown = new Dictionary<string, long>
                {
                    ["EmailReply"] = 0,
                    ["BankTelemetry"] = 0,
                    ["EmailCleanup"] = 0,
                    ["ScheduleExtractor"] = 0
                },
                LastCalledAt = DateTime.UtcNow
            };
            record = await _usageRepo.CreateAsync(record, ct);
        }
        else
        {
            if (record.MonthlyQuotaLimit != configuredQuota || record.WarningThreshold != configuredWarning)
            {
                record.MonthlyQuotaLimit = configuredQuota;
                record.WarningThreshold = configuredWarning;
                await _usageRepo.UpdateAsync(record, ct);
            }
        }

        return record;
    }

    public async Task<bool> CanRunBackgroundAiAsync(CancellationToken ct = default)
    {
        var current = await GetCurrentMonthlyUsageAsync(ct);
        return current.TotalTokens < current.MonthlyQuotaLimit;
    }

    public async Task<long> GetRemainingTokensAsync(CancellationToken ct = default)
    {
        var current = await GetCurrentMonthlyUsageAsync(ct);
        return Math.Max(0, current.MonthlyQuotaLimit - current.TotalTokens);
    }

    public async Task<AiTokenUsageMonthly> RecordUsageAsync(
        string feature,
        long promptTokens,
        long candidatesTokens,
        long totalTokens,
        CancellationToken ct = default)
    {
        var current = await GetCurrentMonthlyUsageAsync(ct);

        current.PromptTokens += promptTokens;
        current.CandidatesTokens += candidatesTokens;
        current.TotalTokens += totalTokens;
        current.CallCount++;
        current.LastCalledAt = DateTime.UtcNow;
        current.UpdatedAt = DateTime.UtcNow;

        if (!current.FeatureBreakdown.ContainsKey(feature))
        {
            current.FeatureBreakdown[feature] = 0;
        }
        current.FeatureBreakdown[feature] += totalTokens;

        // Check Warning Threshold (200,000)
        if (current.TotalTokens >= current.WarningThreshold && !current.WarningSent)
        {
            current.WarningSent = true;
            try
            {
                await _notificationService.SendNotificationAsync(
                    "⚠️ Cảnh báo Hạn mức Token AI (200k)",
                    $"Bạn đã sử dụng {current.TotalTokens:N0} / {current.MonthlyQuotaLimit:N0} token trong tháng {current.YearMonth} ({(double)current.TotalTokens / current.MonthlyQuotaLimit * 100:F1}%). Hệ thống sẽ tự động tạm khóa các tác vụ AI chạy ngầm khi chạm 250k token.",
                    "warning",
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send AI token warning notification.");
            }
        }

        // Check Exceeded Quota Limit (250,000)
        if (current.TotalTokens >= current.MonthlyQuotaLimit && !current.QuotaExceededSent)
        {
            current.QuotaExceededSent = true;
            try
            {
                await _notificationService.SendNotificationAsync(
                    "🚨 Đã khóa Tác vụ AI Chạy ngầm (250k Tokens)",
                    $"Tổng token sử dụng trong tháng {current.YearMonth} đã chạm {current.TotalTokens:N0} / {current.MonthlyQuotaLimit:N0} token. Các tác vụ AI chạy ngầm (AI Clean, Lịch hẹn) được tạm khóa cho đến đầu tháng sau. Dọn dẹp bằng Regex vẫn chạy bình thường.",
                    "critical",
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send AI quota exceeded notification.");
            }
        }

        await _usageRepo.UpdateAsync(current, ct);
        return current;
    }

    public async Task ResetMonthlyUsageAsync(CancellationToken ct = default)
    {
        var current = await GetCurrentMonthlyUsageAsync(ct);
        current.TotalTokens = 0;
        current.PromptTokens = 0;
        current.CandidatesTokens = 0;
        current.CallCount = 0;
        current.WarningSent = false;
        current.QuotaExceededSent = false;
        current.FeatureBreakdown.Clear();
        current.UpdatedAt = DateTime.UtcNow;
        await _usageRepo.UpdateAsync(current, ct);
    }
}
