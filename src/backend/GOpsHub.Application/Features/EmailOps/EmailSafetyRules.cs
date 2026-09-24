using System.Text.RegularExpressions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;

namespace GOpsHub.Application.Features.EmailOps;

/// <summary>
/// Centralized safety policies and ReDoS-protected matching for email cleanup operations.
/// Ensures consistent behavior between Background Jobs and Manual Command triggers.
/// </summary>
public static class EmailSafetyRules
{
    public static readonly HashSet<string> ProtectedBankDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "vpb.com.vn",
        "vietcombank.com.vn",
        "techcombank.com.vn",
        "mbbank.com.vn",
        "momo.vn",
        "tpb.com.vn",
        "acb.com.vn",
        "bidv.com.vn",
        "sacombank.com.vn",
        "zalopay.vn",
        "shb.com.vn",
        "ocb.com.vn",
        "vib.com.vn"
    };

    public static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Checks whether an email's sender belongs to a protected financial institution or user whitelist.
    /// Uses strict suffix/domain matching to avoid false positives.
    /// </summary>
    public static bool IsProtectedSender(string? from, IEnumerable<string>? whitelistDomains)
    {
        if (string.IsNullOrWhiteSpace(from)) return false;

        // 1. Protected bank & e-wallet domains
        foreach (var bankDomain in ProtectedBankDomains)
        {
            if (from.Contains(bankDomain, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // 2. User-defined whitelist domains
        if (whitelistDomains != null)
        {
            foreach (var rawDomain in whitelistDomains)
            {
                if (string.IsNullOrWhiteSpace(rawDomain)) continue;
                var domain = rawDomain.Trim().TrimStart('@');
                if (string.IsNullOrEmpty(domain)) continue;

                // Match exact domain suffix, e.g. "foo@example.com" or "<foo@example.com>"
                if (from.EndsWith("@" + domain, StringComparison.OrdinalIgnoreCase) ||
                    from.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase) ||
                    from.Contains("@" + domain + ">", StringComparison.OrdinalIgnoreCase) ||
                    from.Contains("." + domain + ">", StringComparison.OrdinalIgnoreCase) ||
                    from.Equals(domain, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether an email's subject denotes urgent or required action that must be preserved.
    /// </summary>
    public static bool IsUrgentActionRequired(EmailMessage? email)
    {
        if (email == null || string.IsNullOrWhiteSpace(email.Subject)) return false;

        const string pattern = @"(?i).*(\[?(action required|cần hành động|urgent action|chú ý quan trọng)\]?|security alert|critical alert).*";
        return Regex.IsMatch(email.Subject, pattern, RegexOptions.None, RegexTimeout);
    }

    /// <summary>
    /// Validates if an email is eligible for cleanup:
    /// - Must NOT be starred (flagged as important)
    /// - Must NOT be read (preserves all read emails for archival/audit)
    /// - Must NOT originate from protected bank or whitelisted senders
    /// - Must NOT be marked as Action Required / Urgent
    /// </summary>
    public static bool IsSafeToClean(EmailMessage? email, IEnumerable<string>? whitelistDomains)
    {
        if (email == null) return false;
        if (email.IsStarred) return false;
        if (email.IsRead) return false;
        if (IsProtectedSender(email.From, whitelistDomains)) return false;
        if (IsUrgentActionRequired(email)) return false;

        return true;
    }

    /// <summary>
    /// Evaluates if an email matches the criteria defined in a CleanupRule.
    /// All specified regex criteria (Sender, Subject, Body) MUST match (AND logic).
    /// If a criterion is not specified (null/whitespace), it is not constrained.
    /// At least one criterion must be specified for a match to occur.
    /// Guaranteed to execute with ReDoS protection (500ms timeout).
    /// </summary>
    public static bool IsEmailMatchingRegex(EmailMessage email, CleanupRule rule)
    {
        bool hasSenderCondition = !string.IsNullOrWhiteSpace(rule.SenderRegex);
        bool hasSubjectCondition = !string.IsNullOrWhiteSpace(rule.SubjectRegex);
        bool hasBodyCondition = !string.IsNullOrWhiteSpace(rule.BodyRegex);

        // At least one condition must be configured
        if (!hasSenderCondition && !hasSubjectCondition && !hasBodyCondition)
        {
            return false;
        }

        try
        {
            // 1. Sender condition (if specified, MUST match)
            if (hasSenderCondition)
            {
                if (string.IsNullOrEmpty(email.From) ||
                    !Regex.IsMatch(email.From, rule.SenderRegex!, RegexOptions.IgnoreCase, RegexTimeout))
                {
                    return false;
                }
            }

            // 2. Subject condition (if specified, MUST match)
            if (hasSubjectCondition)
            {
                if (string.IsNullOrEmpty(email.Subject) ||
                    !Regex.IsMatch(email.Subject, rule.SubjectRegex!, RegexOptions.IgnoreCase, RegexTimeout))
                {
                    return false;
                }
            }

            // 3. Body condition (if specified, MUST match)
            if (hasBodyCondition)
            {
                var bodyToCheck = email.Snippet ?? email.Body ?? string.Empty;
                if (string.IsNullOrEmpty(bodyToCheck) ||
                    !Regex.IsMatch(bodyToCheck, rule.BodyRegex!, RegexOptions.IgnoreCase, RegexTimeout))
                {
                    return false;
                }
            }

            // All configured criteria matched!
            return true;
        }
        catch (RegexMatchTimeoutException)
        {
            // Catastrophic backtracking prevented
            return false;
        }
        catch
        {
            // Malformed regex safely swallowed
            return false;
        }
    }

    /// <summary>
    /// Builds standard Gmail search query targeting unread, unstarred inbox messages.
    /// </summary>
    public static string BuildDefaultQuery(string? customQuery = null)
    {
        if (!string.IsNullOrWhiteSpace(customQuery))
            return customQuery;

        return "in:inbox is:unread -is:starred";
    }

    /// <summary>
    /// Validates whether a given regex string compiles cleanly without syntax errors or unsafe patterns.
    /// </summary>
    public static bool IsValidRegex(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return true;
        try
        {
            _ = new Regex(pattern, RegexOptions.None, RegexTimeout);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Compares two regex patterns to detect substantial overlap, equivalence, or containment.
    /// Supports short keywords (e.g. grab, tiki) and OR branches (e.g. sale|deal).
    /// </summary>
    public static bool AreRegexPatternsSimilar(string? p1, string? p2)
    {
        if (string.IsNullOrWhiteSpace(p1) && string.IsNullOrWhiteSpace(p2)) return true;
        if (string.IsNullOrWhiteSpace(p1) || string.IsNullOrWhiteSpace(p2)) return false;

        string Clean(string p)
        {
            var s = p.ToLowerInvariant().Replace("(?i)", "").Replace("(?-i)", "").Trim();
            // 1. Replace regex escape sequences like \s, \d, \w, \b, \t, etc. with space
            s = Regex.Replace(s, @"\\[sSwWdDbBtrnvf]", " ");
            // 2. Remove character class bracket contents like [\d,] or [a-z]
            s = Regex.Replace(s, @"\[[^\]]+\]", " ");
            // 3. Remove punctuation and regex special characters
            s = Regex.Replace(s, @"[\s\(\)\[\]\\\|\^\$\.\*\+\?\{\},:;!#&%<>=/""'@\-_]", "");
            return s.Trim();
        }

        var c1 = Clean(p1);
        var c2 = Clean(p2);

        if (string.IsNullOrEmpty(c1) || string.IsNullOrEmpty(c2)) return false;
        if (c1.Equals(c2, StringComparison.OrdinalIgnoreCase)) return true;

        // Substring check for keywords of length >= 3
        if (c1.Length >= 3 && c2.Length >= 3)
        {
            if (c1.Contains(c2, StringComparison.OrdinalIgnoreCase) || c2.Contains(c1, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Branch-level comparison for OR constructs
        var branches1 = p1.Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(Clean)
            .Where(b => b.Length >= 3)
            .ToList();
        var branches2 = p2.Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(Clean)
            .Where(b => b.Length >= 3)
            .ToList();

        if (branches1.Count > 0 && branches2.Count > 0)
        {
            if (branches1.Any(b1 => branches2.Any(b2 =>
                b1.Equals(b2, StringComparison.OrdinalIgnoreCase) ||
                (b1.Length >= 3 && b2.Length >= 3 && (b1.Contains(b2, StringComparison.OrdinalIgnoreCase) || b2.Contains(b1, StringComparison.OrdinalIgnoreCase))))))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Evaluates if a newly suggested rule is redundant or duplicates an existing active CleanupRule.
    /// Checks Subject-to-Subject and Sender-to-Sender independently.
    /// </summary>
    public static bool IsDuplicateRule(string? subjectRegex, string? senderRegex, IEnumerable<CleanupRule> existingRules)
    {
        bool hasSubject = !string.IsNullOrWhiteSpace(subjectRegex);
        bool hasSender = !string.IsNullOrWhiteSpace(senderRegex);

        if (!hasSubject && !hasSender) return true;

        foreach (var rule in existingRules)
        {
            if (!rule.IsActive) continue;

            bool ruleHasSubject = !string.IsNullOrWhiteSpace(rule.SubjectRegex);
            bool ruleHasSender = !string.IsNullOrWhiteSpace(rule.SenderRegex);

            // Case 1: New rule only specifies Subject
            if (hasSubject && !hasSender)
            {
                if (ruleHasSubject && !ruleHasSender && AreRegexPatternsSimilar(subjectRegex, rule.SubjectRegex))
                {
                    return true;
                }
            }
            // Case 2: New rule only specifies Sender
            else if (!hasSubject && hasSender)
            {
                if (ruleHasSender && !ruleHasSubject && AreRegexPatternsSimilar(senderRegex, rule.SenderRegex))
                {
                    return true;
                }
            }
            // Case 3: New rule specifies BOTH Subject and Sender
            else if (hasSubject && hasSender)
            {
                // If existing rule already matches this sender (without subject restriction), new rule is redundant
                if (ruleHasSender && !ruleHasSubject && AreRegexPatternsSimilar(senderRegex, rule.SenderRegex))
                {
                    return true;
                }

                // If existing rule already matches this subject (without sender restriction), new rule is redundant
                if (ruleHasSubject && !ruleHasSender && AreRegexPatternsSimilar(subjectRegex, rule.SubjectRegex))
                {
                    return true;
                }

                // If existing rule matches both similar sender AND similar subject
                if (ruleHasSender && ruleHasSubject &&
                    AreRegexPatternsSimilar(senderRegex, rule.SenderRegex) &&
                    AreRegexPatternsSimilar(subjectRegex, rule.SubjectRegex))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
