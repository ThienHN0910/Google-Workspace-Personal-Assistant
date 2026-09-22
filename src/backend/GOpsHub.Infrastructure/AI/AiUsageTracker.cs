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

    public Task<bool> CanRunBackgroundAiAsync(CancellationToken ct = default)
    {
        // Theo dõi hạn mức tháng không còn giới hạn token hàng tháng; tác vụ ngầm chạy bình thường
        return Task.FromResult(true);
    }

    public async Task<long> GetRemainingTokensAsync(CancellationToken ct = default)
    {
        var current = await GetCurrentMonthlyUsageAsync(ct);
        if (current.MonthlyQuotaLimit <= 0) return long.MaxValue;
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
