using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace GOpsHub.Application.Features.EmailOps;

public class EmailCleanupBackgroundJob
{
    private readonly IRepository<CleanupRule> _ruleRepo;
    private readonly IRepository<CleanupLog> _logRepo;
    private readonly IRepository<EmailActionLog> _actionLogRepo;
    private readonly IGmailService _gmailService;
    private readonly IAIService _aiService;
    private readonly IAiUsageTracker _usageTracker;
    private readonly INotificationService _notificationService;
    private readonly ILogger<EmailCleanupBackgroundJob> _logger;

    private static readonly HashSet<string> ProtectedBankDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "vpb.com.vn",
        "vietcombank.com.vn",
        "techcombank.com.vn",
        "mbbank.com.vn",
        "momo.vn"
    };

    public EmailCleanupBackgroundJob(
        IRepository<CleanupRule> ruleRepo,
        IRepository<CleanupLog> logRepo,
        IRepository<EmailActionLog> actionLogRepo,
        IGmailService gmailService,
        IAIService aiService,
        IAiUsageTracker usageTracker,
        INotificationService notificationService,
        ILogger<EmailCleanupBackgroundJob> logger)
    {
        _ruleRepo = ruleRepo;
        _logRepo = logRepo;
        _actionLogRepo = actionLogRepo;
        _gmailService = gmailService;
        _aiService = aiService;
        _usageTracker = usageTracker;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task RunAutoCleanupAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting scheduled Email Cleanup (UC01 Inbox Zero - Regex First)...");
        var allActiveRules = await _ruleRepo.FindAsync(r => r.IsActive, ct);

        // Separate regex rules and AI rules
        var regexRules = allActiveRules
            .Where(r => !string.IsNullOrEmpty(r.SubjectRegex) || !string.IsNullOrEmpty(r.SenderRegex) || !string.IsNullOrEmpty(r.BodyRegex))
            .ToList();

        // Fetch recent unread or promotional candidates from Inbox
        var candidateEmails = await _gmailService.GetEmailsAsync("in:inbox -is:starred", 100, ct);
        if (candidateEmails == null || !candidateEmails.Any())
        {
            _logger.LogInformation("Inbox is clean or no candidate emails found.");
            return;
        }

        var processedEmailIds = new HashSet<string>();
        int totalTrashed = 0;
        int totalArchived = 0;
        int totalRegexCleaned = 0;

        // ==========================================
        // GIAI ĐOẠN 1: Dọn dẹp bằng Regex trước (Tiết kiệm Token AI)
        // ==========================================
        foreach (var email in candidateEmails)
        {
            if (email.IsStarred) continue;

            // Bảo vệ các domain ngân hàng và domain whitelist
            if (IsProtectedSender(email.From, allActiveRules.SelectMany(r => r.WhitelistDomains)))
            {
                continue;
            }

            foreach (var rule in regexRules)
            {
                if (IsEmailMatchingRegex(email, rule))
                {
                    if (rule.Action == CleanupAction.Trash)
                    {
                        await _gmailService.TrashEmailAsync(email.Id, ct);
                        totalTrashed++;
                    }
                    else
                    {
                        await _gmailService.ArchiveEmailAsync(email.Id, ct);
                        totalArchived++;
                    }

                    processedEmailIds.Add(email.Id);
                    totalRegexCleaned++;

                    // Ghi audit log chi tiết
                    await _actionLogRepo.CreateAsync(new EmailActionLog
                    {
                        EmailId = email.Id,
                        Subject = email.Subject,
                        Sender = email.From,
                        Action = rule.Action == CleanupAction.Trash ? "Trashed" : "Archived",
                        SourceJob = "EmailCleanup",
                        Reason = $"RegexMatched: Rule '{rule.RuleName}' (SubjectRegex: '{rule.SubjectRegex}', SenderRegex: '{rule.SenderRegex}')"
                    }, ct);

                    break; // Đã dọn bởi rule này, chuyển sang email tiếp theo
                }
            }
        }

        _logger.LogInformation("Giai đoạn 1 (Regex-First): Đã dọn {Count} emails mà KHÔNG tốn token AI.", totalRegexCleaned);

        // ==========================================
        // GIAI ĐOẠN 2: Học Regex Tự động & Phân tích AI cho các email còn lại
        // ==========================================
        var remainingEmails = candidateEmails
            .Where(e => !processedEmailIds.Contains(e.Id) && !e.IsStarred && !IsProtectedSender(e.From, Enumerable.Empty<string>()))
            .Take(15)
            .ToList();

        if (remainingEmails.Any() && await _usageTracker.CanRunBackgroundAiAsync(ct))
        {
            try
            {
                var snippetsBuilder = new StringBuilder();
                foreach (var rem in remainingEmails)
                {
                    snippetsBuilder.AppendLine($"[ID: {rem.Id}] Từ: {rem.From} | Tiêu đề: {rem.Subject} | Nội dung: {rem.Snippet}");
                }

                var suggestion = await _aiService.AnalyzeSpamPatternsAsync(snippetsBuilder.ToString(), ct);

                if (suggestion != null && suggestion.HasPattern && (!string.IsNullOrEmpty(suggestion.SuggestedSubjectRegex) || !string.IsNullOrEmpty(suggestion.SuggestedSenderRegex)))
                {
                    var existingPatterns = allActiveRules
                        .SelectMany(r => new[] { r.SubjectRegex, r.SenderRegex, r.BodyRegex })
                        .Where(p => !string.IsNullOrEmpty(p))
                        .Select(p => p!)
                        .ToList();

                    var patternToCheck = suggestion.SuggestedSubjectRegex ?? suggestion.SuggestedSenderRegex!;
                    bool isDuplicate = IsRegexSimilarOrDuplicate(patternToCheck, existingPatterns);

                    if (!isDuplicate)
                    {
                        var newRule = new CleanupRule
                        {
                            RuleName = $"Tự động học: {suggestion.Category}",
                            Action = suggestion.Action.Equals("Archive", StringComparison.OrdinalIgnoreCase) ? CleanupAction.Archive : CleanupAction.Trash,
                            SubjectRegex = suggestion.SuggestedSubjectRegex,
                            SenderRegex = suggestion.SuggestedSenderRegex,
                            IsActive = true,
                            IsAutoLearned = true,
                            UseAI = false
                        };

                        await _ruleRepo.CreateAsync(newRule, ct);
                        _logger.LogInformation("Tạo thành công CleanupRule tự động: {RuleName}", newRule.RuleName);

                        // Thông báo Telegram
                        await _notificationService.SendNotificationAsync(
                            "🤖 AI vừa học Quy tắc Dọn dẹp mới!",
                            $"Đã phân tích và tạo quy tắc tự động: <b>{newRule.RuleName}</b>\n• Regex Tiêu đề: <code>{newRule.SubjectRegex ?? "N/A"}</code>\n• Regex Người gửi: <code>{newRule.SenderRegex ?? "N/A"}</code>\nTừ các lần sau, hệ thống sẽ tự động dọn dẹp nhóm này bằng Regex!",
                            "info",
                            ct);
                    }

                    // Dọn dẹp các email mục tiêu được AI chỉ định
                    if (suggestion.TargetEmailIds != null && suggestion.TargetEmailIds.Any())
                    {
                        foreach (var targetId in suggestion.TargetEmailIds)
                        {
                            var targetEmail = remainingEmails.FirstOrDefault(e => e.Id == targetId);
                            if (targetEmail != null)
                            {
                                if (suggestion.Action.Equals("Archive", StringComparison.OrdinalIgnoreCase))
                                {
                                    await _gmailService.ArchiveEmailAsync(targetId, ct);
                                    totalArchived++;
                                }
                                else
                                {
                                    await _gmailService.TrashEmailAsync(targetId, ct);
                                    totalTrashed++;
                                }

                                await _actionLogRepo.CreateAsync(new EmailActionLog
                                {
                                    EmailId = targetId,
                                    Subject = targetEmail.Subject,
                                    Sender = targetEmail.From,
                                    Action = suggestion.Action.Equals("Archive", StringComparison.OrdinalIgnoreCase) ? "Archived" : "Trashed",
                                    SourceJob = "EmailCleanup",
                                    Reason = $"AiPatternMatched: Category '{suggestion.Category}' - {suggestion.Reason}"
                                }, ct);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gọi AI phân tích học pattern rác mới.");
            }
        }

        // Bắn thông báo tóm tắt nếu có dọn dẹp
        if (totalTrashed > 0 || totalArchived > 0)
        {
            await _notificationService.SendNotificationAsync(
                "🧹 Báo cáo tự động dọn dẹp Inbox",
                $"Đã quét và xử lý thành công: {totalTrashed} thư vào Thùng rác, {totalArchived} thư Lưu trữ (Trong đó {totalRegexCleaned} thư được dọn sạch bằng Regex).",
                "info",
                ct);
        }

        _logger.LogInformation("Hoàn tất Email Cleanup: Trashed={Trashed}, Archived={Archived}, RegexCount={Regex}", totalTrashed, totalArchived, totalRegexCleaned);
    }

    private static bool IsProtectedSender(string? from, IEnumerable<string> whitelistDomains)
    {
        if (string.IsNullOrEmpty(from)) return false;

        // Never touch bank notification emails
        foreach (var bankDomain in ProtectedBankDomains)
        {
            if (from.Contains(bankDomain, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Check user custom whitelist
        foreach (var domain in whitelistDomains)
        {
            if (!string.IsNullOrWhiteSpace(domain) && from.EndsWith(domain.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsEmailMatchingRegex(EmailMessage email, CleanupRule rule)
    {
        try
        {
            if (!string.IsNullOrEmpty(rule.SubjectRegex) && !string.IsNullOrEmpty(email.Subject))
            {
                if (Regex.IsMatch(email.Subject, rule.SubjectRegex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500)))
                    return true;
            }

            if (!string.IsNullOrEmpty(rule.SenderRegex) && !string.IsNullOrEmpty(email.From))
            {
                if (Regex.IsMatch(email.From, rule.SenderRegex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500)))
                    return true;
            }

            if (!string.IsNullOrEmpty(rule.BodyRegex))
            {
                var bodyToCheck = email.Snippet ?? email.Body ?? string.Empty;
                if (Regex.IsMatch(bodyToCheck, rule.BodyRegex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500)))
                    return true;
            }
        }
        catch
        {
            // Ignore malformed regex
        }

        return false;
    }

    private static bool IsRegexSimilarOrDuplicate(string newPattern, IEnumerable<string> existingPatterns)
    {
        if (string.IsNullOrWhiteSpace(newPattern)) return false;

        string Normalize(string p) =>
            Regex.Replace(p.ToLowerInvariant().Replace("(?i)", "").Trim(), @"[\s\(\)\[\]\\\|\^\$\.\*\+\?]", "");

        var normNew = Normalize(newPattern);
        if (string.IsNullOrEmpty(normNew)) return false;

        foreach (var exist in existingPatterns)
        {
            if (string.IsNullOrWhiteSpace(exist)) continue;
            var normExist = Normalize(exist);
            if (string.IsNullOrEmpty(normExist)) continue;

            if (normNew.Equals(normExist, StringComparison.OrdinalIgnoreCase))
                return true;

            if (normNew.Length > 6 && normExist.Length > 6)
            {
                if (normNew.Contains(normExist) || normExist.Contains(normNew))
                    return true;
            }
        }

        return false;
    }
}
