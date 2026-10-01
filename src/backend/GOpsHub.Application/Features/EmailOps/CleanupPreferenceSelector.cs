using System.Text.RegularExpressions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;

namespace GOpsHub.Application.Features.EmailOps;

public static class CleanupPreferenceSelector
{
    private static readonly HashSet<string> CommonEmailProviderDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "googlemail.com", "google.com", "github.com",
        "outlook.com", "hotmail.com", "live.com", "yahoo.com", "icloud.com"
    };

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

    public static CleanupFeedback? FindMatchingTrash(EmailMessage email, IReadOnlyList<CleanupFeedback>? allFeedback)
    {
        if (allFeedback == null || allFeedback.Count == 0) return null;
        var address = EmailSafetyRules.GetSenderAddress(email.From);
        if (address == null) return null;
        if (EmailSafetyRules.IsProtectedSender(email.From, null)) return null;
        if (HasMatchingKeep(email, allFeedback)) return null;

        var domain = address.Split('@').LastOrDefault();
        var candidateSubject = Normalize(email.Subject);

        // 1. Exact sender match
        var senderMatches = allFeedback.Where(x =>
            x.Decision == CleanupDecision.Trash &&
            string.Equals(EmailSafetyRules.GetSenderAddress(x.Sender), address, StringComparison.OrdinalIgnoreCase)).ToList();

        if (senderMatches.Count > 0)
        {
            // If any feedback has exact subject or high similarity, prioritize it
            var subjectMatch = senderMatches.FirstOrDefault(x =>
                Normalize(x.Subject) == candidateSubject || IsSimilarSubject(Normalize(x.Subject), candidateSubject));
            if (subjectMatch != null) return subjectMatch;

            // Otherwise, if sender is a dedicated service or non-generic provider, sender match is sufficient
            if (!CommonEmailProviderDomains.Contains(domain ?? string.Empty) ||
                address.StartsWith("notifications@") || address.StartsWith("no-reply@") ||
                address.StartsWith("noreply@") || address.StartsWith("news@") || address.StartsWith("team@"))
            {
                return senderMatches.First();
            }
        }

        // 2. Domain match for non-generic providers (e.g. workbridge.io.vn, appflowy.io, datacamp.com, etc.)
        if (!string.IsNullOrEmpty(domain) && !CommonEmailProviderDomains.Contains(domain))
        {
            var domainMatch = allFeedback.FirstOrDefault(x =>
                x.Decision == CleanupDecision.Trash &&
                ((!string.IsNullOrEmpty(x.SenderDomain) &&
                  (domain.Equals(x.SenderDomain, StringComparison.OrdinalIgnoreCase) || domain.EndsWith("." + x.SenderDomain, StringComparison.OrdinalIgnoreCase))) ||
                 (EmailSafetyRules.GetSenderAddress(x.Sender)?.Split('@').LastOrDefault() is string sampleDom &&
                  (domain.Equals(sampleDom, StringComparison.OrdinalIgnoreCase) || domain.EndsWith("." + sampleDom, StringComparison.OrdinalIgnoreCase)))));

            if (domainMatch != null) return domainMatch;
        }

        return null;
    }

    public static bool CanAutoTrashKnownType(EmailMessage email, AICleanupDecision decision,
        IReadOnlyList<CleanupFeedback> cited, IReadOnlyList<CleanupFeedback> relevantKeep)
    {
        if (decision.Outcome != AICleanupOutcome.Trash) return false;
        var address = EmailSafetyRules.GetSenderAddress(email.From);
        if (address == null) return false;
        if (HasMatchingKeep(email, relevantKeep)) return false;
        if (EmailSafetyRules.IsProtectedSender(email.From, null)) return false;

        var domain = address.Split('@').LastOrDefault();

        // Matches if cited preference is Trash and shares sender address or non-generic domain
        return cited.Any(x => x.Decision == CleanupDecision.Trash &&
            (string.Equals(EmailSafetyRules.GetSenderAddress(x.Sender), address, StringComparison.OrdinalIgnoreCase) ||
             (!string.IsNullOrEmpty(domain) && !CommonEmailProviderDomains.Contains(domain) &&
              string.Equals(x.SenderDomain ?? EmailSafetyRules.GetSenderAddress(x.Sender)?.Split('@').LastOrDefault(), domain, StringComparison.OrdinalIgnoreCase))));
    }

    public static bool HasMatchingKeep(EmailMessage email, IEnumerable<CleanupFeedback>? feedback)
    {
        if (feedback == null) return false;
        var address = EmailSafetyRules.GetSenderAddress(email.From);
        if (address == null) return false;
        var subject = Normalize(email.Subject);
        var domain = address.Split('@').LastOrDefault();

        return feedback.Any(x => x.Decision == CleanupDecision.Keep &&
            (
                (string.Equals(EmailSafetyRules.GetSenderAddress(x.Sender), address, StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrEmpty(x.Subject) || Normalize(x.Subject) == subject || IsSimilarSubject(Normalize(x.Subject), subject))) ||
                (!string.IsNullOrEmpty(x.SenderDomain) && domain != null &&
                 (domain.Equals(x.SenderDomain, StringComparison.OrdinalIgnoreCase) || domain.EndsWith("." + x.SenderDomain, StringComparison.OrdinalIgnoreCase)) &&
                 Normalize(x.Subject) == subject)
            ));
    }

    private static bool IsSimilarSubject(string sampleSubject, string candidateSubject)
    {
        if (string.IsNullOrWhiteSpace(sampleSubject) || string.IsNullOrWhiteSpace(candidateSubject)) return false;
        var words = Regex.Matches(candidateSubject, @"[\p{L}\p{N}]{3,}").Select(x => x.Value).ToHashSet();
        if (words.Count == 0) return false;
        int common = Regex.Matches(sampleSubject, @"[\p{L}\p{N}]{3,}").Count(x => words.Contains(x.Value));
        return common >= 3;
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
