using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;

namespace GOpsHub.Application.Features.EmailOps.Queries;

public record PreviewCleanupRuleQuery(string RuleId) : IQuery<CleanupRulePreview>;

public record CleanupRulePreview(string RuleId, IReadOnlyList<CleanupRuleMatch> Matches,
    IReadOnlyList<string> Blockers);

public record CleanupRuleMatch(string EmailId, string Sender, string? Subject, string Source);

public class PreviewCleanupRuleQueryHandler : IQueryHandler<PreviewCleanupRuleQuery, CleanupRulePreview>
{
    private readonly IRepository<CleanupRule> _rules;
    private readonly IRepository<CleanupFeedback> _feedback;
    private readonly IRepository<CleanupReview> _reviews;
    private readonly IGmailService _gmail;

    public PreviewCleanupRuleQueryHandler(IRepository<CleanupRule> rules,
        IRepository<CleanupFeedback> feedback, IRepository<CleanupReview> reviews, IGmailService gmail)
    {
        _rules = rules;
        _feedback = feedback;
        _reviews = reviews;
        _gmail = gmail;
    }

    public async Task<CleanupRulePreview> HandleAsync(PreviewCleanupRuleQuery query,
        CancellationToken ct = default)
    {
        var rule = await _rules.GetByIdAsync(query.RuleId, ct)
            ?? throw new KeyNotFoundException($"Cleanup rule {query.RuleId} not found.");
        var blockers = new List<string>();
        var matches = new List<CleanupRuleMatch>();

        if (rule.Action != CleanupAction.Trash) blockers.Add("Only Trash action is supported.");
        if (string.IsNullOrWhiteSpace(rule.SenderRegex)) blockers.Add("Sender regex is required.");
        if (string.IsNullOrWhiteSpace(rule.SubjectRegex)) blockers.Add("Sender-only rule is too broad.");
        if (IsMatchAny(rule.SenderRegex)) blockers.Add("Match-any sender regex is forbidden.");
        if (IsMatchAny(rule.SubjectRegex)) blockers.Add("Match-any subject regex is forbidden.");
        if (!EmailSafetyRules.IsValidRegex(rule.SenderRegex) ||
            !EmailSafetyRules.IsValidRegex(rule.SubjectRegex) ||
            !EmailSafetyRules.IsValidRegex(rule.BodyRegex)) blockers.Add("Invalid regex.");
        if (rule.ApprovalStatus == CleanupRuleApprovalStatus.Rejected)
            blockers.Add("Rejected rule cannot be approved.");

        var allRules = await _rules.GetAllAsync(ct);
        if (allRules.Any(x => x.Id != rule.Id &&
            string.Equals(x.SenderRegex?.Trim(), rule.SenderRegex?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.SubjectRegex?.Trim(), rule.SubjectRegex?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.BodyRegex?.Trim(), rule.BodyRegex?.Trim(), StringComparison.OrdinalIgnoreCase)))
            blockers.Add("Duplicate rule with same AND criteria already exists.");

        if (blockers.Any(x => x is "Invalid regex." or "Match-any sender regex is forbidden." or
                "Match-any subject regex is forbidden."))
            return new CleanupRulePreview(rule.Id, matches, blockers);

        var feedback = await _feedback.GetAllAsync(ct);
        foreach (var sample in feedback.Where(x => x.Decision == CleanupDecision.Keep))
        {
            var email = new EmailMessage { Id = sample.EmailId, From = sample.Sender,
                Subject = sample.Subject ?? "", Snippet = sample.Snippet ?? "" };
            if (!EmailSafetyRules.IsEmailMatchingRegex(email, rule)) continue;
            blockers.Add("Rule matches a Keep preference.");
            AddMatch(matches, email, "Keep");
        }

        var pending = await _reviews.FindAsync(x => x.Status == CleanupReviewStatus.Pending ||
            x.Status == CleanupReviewStatus.ProcessingTrash, ct);
        foreach (var sample in pending)
        {
            var email = new EmailMessage { Id = sample.EmailId, From = sample.Sender,
                Subject = sample.Subject ?? "", Snippet = sample.Snippet ?? "" };
            if (!EmailSafetyRules.IsEmailMatchingRegex(email, rule)) continue;
            blockers.Add("Rule matches a pending review.");
            AddMatch(matches, email, "Pending");
        }

        var live = await _gmail.GetEmailsAsync("is:unread in:inbox -is:starred", 100, ct);
        foreach (var email in live)
        {
            if (!EmailSafetyRules.IsEmailMatchingRegex(email, rule)) continue;
            AddMatch(matches, email, "Inbox");
            if (!EmailSafetyRules.IsSafeForAutomaticTrash(email, rule.WhitelistDomains))
                blockers.Add("Rule matches a protected sender or alert.");
        }
        return new CleanupRulePreview(rule.Id, matches.Take(20).ToList(), blockers.Distinct().ToList());
    }

    private static bool IsMatchAny(string? pattern) =>
        pattern?.Trim().Replace("(?i)", "", StringComparison.OrdinalIgnoreCase) is
            ".*" or "^.*$" or ".+" or "^.+$";

    private static void AddMatch(List<CleanupRuleMatch> matches, EmailMessage email, string source)
    {
        if (matches.Count < 20)
            matches.Add(new CleanupRuleMatch(email.Id, email.From, email.Subject, source));
    }
}
