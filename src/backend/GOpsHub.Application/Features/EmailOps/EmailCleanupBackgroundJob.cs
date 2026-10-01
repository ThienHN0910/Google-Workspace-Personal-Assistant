using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.EmailOps.Commands;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Application.Features.EmailOps;

public class EmailCleanupBackgroundJob
{
    private readonly IRepository<CleanupRule> _rules;
    private readonly IRepository<CleanupLog> _runs;
    private readonly IRepository<EmailActionLog> _actions;
    private readonly IRepository<CleanupFeedback>? _feedback;
    private readonly IRepository<CleanupReview>? _reviews;
    private readonly IGmailService _gmail;
    private readonly IAIService _ai;
    private readonly IAiUsageTracker _usage;
    private readonly INotificationService _notifications;
    private readonly ILogger<EmailCleanupBackgroundJob> _logger;

    public EmailCleanupBackgroundJob(
        IRepository<CleanupRule> ruleRepo,
        IRepository<CleanupLog> logRepo,
        IRepository<EmailActionLog> actionLogRepo,
        IGmailService gmailService,
        IAIService aiService,
        IAiUsageTracker usageTracker,
        INotificationService notificationService,
        ILogger<EmailCleanupBackgroundJob> logger,
        IRepository<CleanupFeedback>? feedbackRepo = null,
        IRepository<CleanupReview>? reviewRepo = null)
    {
        _rules = ruleRepo;
        _runs = logRepo;
        _actions = actionLogRepo;
        _gmail = gmailService;
        _ai = aiService;
        _usage = usageTracker;
        _notifications = notificationService;
        _logger = logger;
        _feedback = feedbackRepo;
        _reviews = reviewRepo;
    }

    public async Task<CleanupLogResult> RunAutoCleanupAsync(CancellationToken ct = default)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var sessionId = Guid.NewGuid().ToString("N")[..8];
        var activeRules = (await _rules.FindAsync(x => x.IsActive &&
            x.ApprovalStatus == CleanupRuleApprovalStatus.Approved, ct))
            .Where(x => x.IsActive && x.ApprovalStatus == CleanupRuleApprovalStatus.Approved &&
                x.Action == CleanupAction.Trash).ToList();
        var candidates = await _gmail.GetEmailsAsync("is:unread in:inbox -is:starred", 100, ct);
        var result = new CleanupLogResult { RulesExecuted = activeRules.Count, TotalProcessed = candidates.Count };
        if (candidates.Count == 0)
        {
            result.TotalDurationMs = watch.ElapsedMilliseconds;
            return result;
        }

        var protectedReviews = _reviews == null
            ? Array.Empty<CleanupReview>()
            : (await _reviews.FindAsync(x => x.Status == CleanupReviewStatus.Pending ||
                x.Status == CleanupReviewStatus.ProcessingTrash ||
                x.Status == CleanupReviewStatus.Kept, ct)).ToArray();
        var pendingIds = protectedReviews.Select(x => x.EmailId).ToHashSet(StringComparer.Ordinal);
        var feedback = _feedback == null
            ? Array.Empty<CleanupFeedback>()
            : (await _feedback.GetAllAsync(ct)).ToArray();
        var keepIds = feedback.Where(x => x.Decision == CleanupDecision.Keep)
            .Select(x => x.EmailId).ToHashSet(StringComparer.Ordinal);
        var applicableRules = activeRules.Where(rule => !feedback.Any(sample =>
            sample.Decision == CleanupDecision.Keep &&
            EmailSafetyRules.IsEmailMatchingRegex(new EmailMessage
            {
                From = sample.Sender,
                Subject = sample.Subject ?? string.Empty,
                Snippet = sample.Snippet ?? string.Empty
            }, rule))).ToList();
        result.RulesExecuted = applicableRules.Count;
        var whitelist = activeRules.SelectMany(x => x.WhitelistDomains ?? new List<string>()).Distinct().ToArray();
        var urgentLogs = await _actions.FindAsync(x => x.Action == "UrgentNotified", ct);
        var notifiedIds = urgentLogs.Select(x => x.EmailId).ToHashSet(StringComparer.Ordinal);
        var remaining = new List<EmailMessage>();

        foreach (var email in candidates)
        {
            if (pendingIds.Contains(email.Id) || keepIds.Contains(email.Id) ||
                CleanupPreferenceSelector.HasMatchingKeep(email, feedback)) continue;

            // 1. Bank domains and whitelisted domains are ALWAYS protected
            if (EmailSafetyRules.IsProtectedSender(email.From, whitelist)) continue;

            // 2. USER SAVED PREFERENCE: Highest priority override!
            var matchingTrashPref = CleanupPreferenceSelector.FindMatchingTrash(email, feedback);
            if (matchingTrashPref != null)
            {
                await TrashAndLogAsync(email, $"SavedPreference: {matchingTrashPref.Reason}", sessionId, ct);
                result.TotalTrashed++;
                continue;
            }

            // 3. Urgent action required (only for emails WITHOUT a saved trash preference)
            if (EmailSafetyRules.IsUrgentActionRequired(email) &&
                !EmailSafetyRules.IsSafeForAutomaticTrash(email, whitelist))
            {
                if (!notifiedIds.Contains(email.Id))
                    await NotifyUrgentAsync(email, sessionId, ct);
                continue;
            }

            // 4. Active Regex rules
            var rule = applicableRules.FirstOrDefault(x => EmailSafetyRules.IsEmailMatchingRegex(email, x));
            if (rule != null)
            {
                if (EmailSafetyRules.IsSafeForAutomaticTrash(email, whitelist))
                {
                    await TrashAndLogAsync(email, $"RegexMatched: Rule '{rule.RuleName}'", sessionId, ct);
                    result.TotalTrashed++;
                    continue;
                }
            }

            // 5. Eligible for AI cleanup analysis
            if (EmailSafetyRules.IsSafeToClean(email, whitelist))
            {
                remaining.Add(email);
            }
        }

        if (remaining.Count > 0 && await _usage.CanRunBackgroundAiAsync(ct))
        {
            var batch = remaining.Take(15).ToList();
            var examples = batch.SelectMany(email => CleanupPreferenceSelector.SelectRelevant(email, feedback, 10))
                .GroupBy(x => x.Id).Select(x => x.First()).Take(30).ToList();
            try
            {
                var decisions = await _ai.AnalyzeCleanupBatchAsync(batch, examples, ct);
                var byId = decisions.GroupBy(x => x.EmailId).ToDictionary(x => x.Key, x => x.First());
                foreach (var email in batch)
                {
                    if (!byId.TryGetValue(email.Id, out var decision)) continue;
                    if (decision.Outcome == AICleanupOutcome.Keep) continue;

                    var matchingTrash = CleanupPreferenceSelector.FindMatchingTrash(email, feedback);
                    bool isKnownType = matchingTrash != null ||
                        CleanupPreferenceSelector.CanAutoTrashKnownType(email, decision,
                            examples.Where(x => decision.FeedbackIds.Contains(x.Id)).ToList(),
                            feedback.Where(x => x.Decision == CleanupDecision.Keep).ToList());

                    if (decision.Outcome == AICleanupOutcome.Trash && isKnownType &&
                        !EmailSafetyRules.IsProtectedSender(email.From, whitelist))
                    {
                        await TrashAndLogAsync(email, $"AiKnownPreference: {decision.Reason}", sessionId, ct);
                        result.TotalTrashed++;
                    }
                    else if (decision.Outcome is AICleanupOutcome.Trash or AICleanupOutcome.Review)
                    {
                        var proposedRuleId = decision.Outcome == AICleanupOutcome.Trash
                            ? await SaveDraftProposalAsync(decision, ct) : null;
                        await CreateReviewAsync(email, decision.Reason, proposedRuleId, ct);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cleanup AI step failed; no further messages were moved.");
            }
        }

        result.TotalSkipped = Math.Max(0, result.TotalProcessed - result.TotalTrashed);
        result.TotalArchived = 0;
        result.TotalDurationMs = watch.ElapsedMilliseconds;

        await _runs.CreateAsync(new CleanupLog
        {
            RuleName = "AutoCleanupBackgroundJob",
            SessionId = sessionId,
            ExecutedAt = DateTime.UtcNow,
            TotalProcessed = result.TotalProcessed,
            TotalTrashed = result.TotalTrashed,
            TotalArchived = 0,
            TotalSkipped = result.TotalSkipped,
            DurationMs = result.TotalDurationMs,
            Details = $"{result.TotalTrashed} moved to Trash, {result.TotalSkipped} skipped"
        }, ct);

        if (result.TotalTrashed > 0)
        {
            await _notifications.SendNotificationAsync("Dọn dẹp Inbox hoàn tất",
                $"Đã chuyển {result.TotalTrashed} email vào Thùng rác.", "info", ct);
        }
        return result;
    }

    private async Task<string?> SaveDraftProposalAsync(AICleanupDecision decision, CancellationToken ct)
    {
        var proposal = decision.Proposal;
        if (proposal?.HasPattern != true ||
            string.IsNullOrWhiteSpace(proposal.SuggestedSenderRegex) ||
            string.IsNullOrWhiteSpace(proposal.SuggestedSubjectRegex) ||
            !EmailSafetyRules.IsValidRegex(proposal.SuggestedSenderRegex) ||
            !EmailSafetyRules.IsValidRegex(proposal.SuggestedSubjectRegex) ||
            proposal.SuggestedSenderRegex.Contains(".*") ||
            proposal.SuggestedSubjectRegex.Trim() is ".*" or "^.*$") return null;

        var existing = await _rules.GetAllAsync(ct);
        var duplicate = existing.FirstOrDefault(x =>
            string.Equals(x.SenderRegex?.Trim(), proposal.SuggestedSenderRegex.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.SubjectRegex?.Trim(), proposal.SuggestedSubjectRegex.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(x.BodyRegex));
        if (duplicate != null) return duplicate.Id;

        var draft = await _rules.CreateAsync(new CleanupRule
        {
            RuleName = $"AI proposal: {proposal.Category}",
            SenderRegex = proposal.SuggestedSenderRegex,
            SubjectRegex = proposal.SuggestedSubjectRegex,
            Action = CleanupAction.Trash,
            IsActive = false,
            IsAutoLearned = true,
            ApprovalStatus = CleanupRuleApprovalStatus.Draft,
            SourceFeedbackId = decision.FeedbackIds.FirstOrDefault()
        }, ct);
        return draft?.Id;
    }

    private async Task CreateReviewAsync(EmailMessage email, string reason, string? proposedRuleId, CancellationToken ct)
    {
        if (_reviews == null) return;
        if (await _reviews.FindOneAsync(x => x.EmailId == email.Id, ct) != null) return;
        try
        {
            await _reviews.CreateAsync(new CleanupReview
            {
                EmailId = email.Id,
                Sender = email.From,
                Subject = email.Subject,
                Snippet = email.Snippet is { Length: > 300 } ? email.Snippet[..300] : email.Snippet,
                AiReason = reason,
                ProposedRuleId = proposedRuleId,
                Status = CleanupReviewStatus.Pending
            }, ct);
            await _notifications.SendNotificationAsync("Email chờ duyệt dọn dẹp",
                $"Email '{email.Subject}' đang chờ bạn chọn Xóa hoặc Giữ.", "warning", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save cleanup review for {EmailId}", email.Id);
        }
    }

    private async Task TrashAndLogAsync(EmailMessage email, string reason, string sessionId, CancellationToken ct)
    {
        await _gmail.TrashEmailAsync(email.Id, ct);
        await _actions.CreateAsync(new EmailActionLog
        {
            EmailId = email.Id,
            Sender = email.From,
            Subject = email.Subject,
            Action = "Trashed",
            SourceJob = "EmailCleanup",
            SessionId = sessionId,
            Reason = reason
        }, ct);
    }

    private async Task NotifyUrgentAsync(EmailMessage email, string sessionId, CancellationToken ct)
    {
        try
        {
            var analysis = await _ai.AnalyzeUrgentEmailAsync(email.Subject, email.From,
                email.Snippet ?? email.Body ?? string.Empty, ct);
            await _notifications.SendNotificationAsync("[CẦN HÀNH ĐỘNG] Email quan trọng từ dịch vụ",
                $"Tiêu đề: {email.Subject}\nMức độ: {analysis.UrgencyLevel}\nHành động: {analysis.ActionSummary}",
                "warning", ct);
            await _actions.CreateAsync(new EmailActionLog
            {
                EmailId = email.Id,
                Sender = email.From,
                Subject = email.Subject,
                Action = "UrgentNotified",
                SourceJob = "EmailCleanup",
                SessionId = sessionId,
                Reason = $"UrgentAction ({analysis.UrgencyLevel}): {analysis.ActionSummary}"
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not notify urgent email {EmailId}", email.Id);
        }
    }
}
