using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Application.Features.EmailOps;

public class EmailCleanupBackgroundJob
{
    private readonly IRepository<CleanupRule> _ruleRepo;
    private readonly IRepository<CleanupLog> _logRepo;
    private readonly IGmailService _gmailService;
    private readonly IAIService _aiService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<EmailCleanupBackgroundJob> _logger;

    public EmailCleanupBackgroundJob(
        IRepository<CleanupRule> ruleRepo,
        IRepository<CleanupLog> logRepo,
        IGmailService gmailService,
        IAIService aiService,
        INotificationService notificationService,
        ILogger<EmailCleanupBackgroundJob> logger)
    {
        _ruleRepo = ruleRepo;
        _logRepo = logRepo;
        _gmailService = gmailService;
        _aiService = aiService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task RunAutoCleanupAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting scheduled Email Cleanup (UC01 Inbox Zero)...");
        var activeRules = await _ruleRepo.FindAsync(r => r.IsActive, ct);

        if (!activeRules.Any())
        {
            _logger.LogInformation("No active cleanup rules found. Skipping email cleanup.");
            return;
        }

        int totalProcessed = 0;
        int totalTrashed = 0;
        int totalArchived = 0;
        int totalSkipped = 0;

        foreach (var rule in activeRules)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var query = BuildGmailQuery(rule);
                var emails = await _gmailService.GetEmailsAsync(query, 100, ct);

                int ruleTrashed = 0;
                int ruleArchived = 0;
                int ruleSkipped = 0;

                foreach (var email in emails)
                {
                    // Safeguard 1: Không bao giờ dọn dẹp email có gắn sao (⭐ Starred)
                    if (email.IsStarred)
                    {
                        ruleSkipped++;
                        continue;
                    }

                    // Safeguard 2: Kiểm tra whitelist domain
                    if (rule.WhitelistDomains.Any(domain => 
                        !string.IsNullOrEmpty(email.From) && email.From.EndsWith(domain, StringComparison.OrdinalIgnoreCase)))
                    {
                        ruleSkipped++;
                        continue;
                    }

                    // Safeguard 3: Nếu là rule sử dụng AI điều kiện
                    if (rule.UseAI && !string.IsNullOrEmpty(rule.AIPrompt))
                    {
                        var matches = await _aiService.CheckCleanupConditionAsync(email.Snippet, rule.AIPrompt, ct);
                        if (!matches)
                        {
                            ruleSkipped++;
                            continue;
                        }
                    }

                    // Thực thi Action
                    if (rule.Action == CleanupAction.Trash)
                    {
                        await _gmailService.TrashEmailAsync(email.Id, ct);
                        ruleTrashed++;
                    }
                    else if (rule.Action == CleanupAction.Archive)
                    {
                        await _gmailService.ArchiveEmailAsync(email.Id, ct);
                        ruleArchived++;
                    }
                }

                sw.Stop();
                var log = new CleanupLog
                {
                    RuleId = rule.Id,
                    RuleName = rule.RuleName,
                    TotalProcessed = emails.Count,
                    TotalTrashed = ruleTrashed,
                    TotalArchived = ruleArchived,
                    TotalSkipped = ruleSkipped,
                    DurationMs = sw.ElapsedMilliseconds,
                    ExecutedAt = DateTime.UtcNow
                };
                await _logRepo.CreateAsync(log, ct);

                totalProcessed += emails.Count;
                totalTrashed += ruleTrashed;
                totalArchived += ruleArchived;
                totalSkipped += ruleSkipped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute cleanup rule {RuleName}", rule.RuleName);
            }
        }

        // Bắn thông báo báo cáo dọn dẹp
        if (totalTrashed > 0 || totalArchived > 0)
        {
            await _notificationService.SendNotificationAsync(
                "🧹 Báo cáo tự động dọn dẹp Inbox",
                $"Đã quét {totalProcessed} email: Chuyển thùng rác {totalTrashed}, Lưu trữ {totalArchived}, Bỏ qua {totalSkipped} email an toàn.",
                "info",
                ct);
        }

        _logger.LogInformation("Completed Email Cleanup: Trashed={Trashed}, Archived={Archived}", totalTrashed, totalArchived);
    }

    private static string BuildGmailQuery(CleanupRule rule)
    {
        var queryParts = new List<string>();

        if (!string.IsNullOrEmpty(rule.CustomQuery))
        {
            queryParts.Add(rule.CustomQuery);
        }

        // Mặc định chỉ dọn dẹp thư trong Inbox và không gắn sao
        queryParts.Add("label:INBOX");
        queryParts.Add("-is:starred");

        return string.Join(" ", queryParts);
    }
}
