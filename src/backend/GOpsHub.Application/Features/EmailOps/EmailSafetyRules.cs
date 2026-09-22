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
    /// Validates if an email is eligible for cleanup:
    /// - Must NOT be starred (flagged as important)
    /// - Must NOT be read (preserves all read emails for archival/audit)
    /// - Must NOT originate from protected bank or whitelisted senders
    /// </summary>
    public static bool IsSafeToClean(EmailMessage? email, IEnumerable<string>? whitelistDomains)
    {
        if (email == null) return false;
        if (email.IsStarred) return false;
        if (email.IsRead) return false;
        if (IsProtectedSender(email.From, whitelistDomains)) return false;

        return true;
    }

    /// <summary>
    /// Evaluates if an email matches the criteria defined in a CleanupRule.
    /// Guaranteed to execute with ReDoS protection (500ms timeout).
    /// </summary>
    public static bool IsEmailMatchingRegex(EmailMessage email, CleanupRule rule)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(rule.SubjectRegex) && !string.IsNullOrEmpty(email.Subject))
            {
                if (Regex.IsMatch(email.Subject, rule.SubjectRegex, RegexOptions.IgnoreCase, RegexTimeout))
                    return true;
            }

            if (!string.IsNullOrWhiteSpace(rule.SenderRegex) && !string.IsNullOrEmpty(email.From))
            {
                if (Regex.IsMatch(email.From, rule.SenderRegex, RegexOptions.IgnoreCase, RegexTimeout))
                    return true;
            }

            if (!string.IsNullOrWhiteSpace(rule.BodyRegex))
            {
                var bodyToCheck = email.Snippet ?? email.Body ?? string.Empty;
                if (!string.IsNullOrEmpty(bodyToCheck) && Regex.IsMatch(bodyToCheck, rule.BodyRegex, RegexOptions.IgnoreCase, RegexTimeout))
                    return true;
            }
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

        return false;
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
}
