using System.Text.RegularExpressions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;

namespace GOpsHub.Application.Features.EmailOps;

public static class CleanupPreferenceSelector
{
    public static IReadOnlyList<CleanupFeedback> SelectRelevant(EmailMessage email,
        IReadOnlyList<CleanupFeedback> all, int limit)
    {
        if (limit <= 0) return Array.Empty<CleanupFeedback>();
        var address = EmailSafetyRules.GetSenderAddress(email.From);
        var domain = address?.Split('@').LastOrDefault();
        var subject = Normalize(email.Subject);

        return all.OrderByDescending(x =>
            Score(x, address, domain, subject))
            .ThenByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToList();
    }

    public static bool CanAutoTrashKnownType(EmailMessage email, AICleanupDecision decision,
        IReadOnlyList<CleanupFeedback> cited, IReadOnlyList<CleanupFeedback> relevantKeep)
    {
        if (decision.Outcome != AICleanupOutcome.Trash) return false;
        var address = EmailSafetyRules.GetSenderAddress(email.From);
        var subject = Normalize(email.Subject);
        if (address == null || subject.Length == 0) return false;
        if (HasMatchingKeep(email, relevantKeep)) return false;

        return cited.Any(x => x.Decision == CleanupDecision.Trash &&
            decision.FeedbackIds.Contains(x.Id) &&
            EmailSafetyRules.GetSenderAddress(x.Sender) == address && Normalize(x.Subject) == subject);
    }

    public static bool HasMatchingKeep(EmailMessage email, IEnumerable<CleanupFeedback> feedback)
    {
        var address = EmailSafetyRules.GetSenderAddress(email.From);
        var subject = Normalize(email.Subject);
        return address != null && subject.Length > 0 && feedback.Any(x =>
            x.Decision == CleanupDecision.Keep &&
            EmailSafetyRules.GetSenderAddress(x.Sender) == address && Normalize(x.Subject) == subject);
    }

    private static int Score(CleanupFeedback feedback, string? address, string? domain, string subject)
    {
        var sampleAddress = EmailSafetyRules.GetSenderAddress(feedback.Sender);
        var sampleDomain = sampleAddress?.Split('@').LastOrDefault();
        var score = sampleAddress != null && sampleAddress == address ? 100 : 0;
        if (sampleDomain != null && sampleDomain == domain) score += 40;
        var sampleSubject = Normalize(feedback.Subject);
        if (subject.Length > 0 && sampleSubject == subject) score += 30;
        else
        {
            var words = Regex.Matches(subject, @"[\p{L}\p{N}]{3,}").Select(x => x.Value).ToHashSet();
            score += Regex.Matches(sampleSubject, @"[\p{L}\p{N}]{3,}")
                .Count(x => words.Contains(x.Value)) * 5;
        }
        return score;
    }

    private static string Normalize(string? value) =>
        Regex.Replace(value?.Trim().ToLowerInvariant() ?? string.Empty, @"\s+", " ");
}
