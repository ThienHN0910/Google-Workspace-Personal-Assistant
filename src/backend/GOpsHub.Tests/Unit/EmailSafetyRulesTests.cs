using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.EmailOps;
using GOpsHub.Domain.Entities;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class EmailSafetyRulesTests
{
    [Theory]
    [InlineData("customercare@vpb.com.vn")]
    [InlineData("no-reply@vietcombank.com.vn")]
    [InlineData("Techcombank <info@techcombank.com.vn>")]
    [InlineData("momo.vn")]
    [InlineData("tpb.com.vn")]
    [InlineData("contact@acb.com.vn")]
    [InlineData("zalopay.vn")]
    public void IsProtectedSender_WhenFromBank_ShouldReturnTrue(string sender)
    {
        var result = EmailSafetyRules.IsProtectedSender(sender, null);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("promotions@shopee.vn")]
    [InlineData("deals@lazada.vn")]
    [InlineData("newsletter@medium.com")]
    public void IsProtectedSender_WhenGeneralSender_ShouldReturnFalse(string sender)
    {
        var result = EmailSafetyRules.IsProtectedSender(sender, null);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsProtectedSender_WithWhitelist_ShouldMatchExactDomainSuffix_NotSubstrings()
    {
        var whitelist = new[] { "company.com" };

        // Exact match
        EmailSafetyRules.IsProtectedSender("ceo@company.com", whitelist).Should().BeTrue();
        EmailSafetyRules.IsProtectedSender("Partner <info@sub.company.com>", whitelist).Should().BeTrue();

        // Substring / impostor match should NOT be whitelisted
        EmailSafetyRules.IsProtectedSender("attacker@evilcompany.com", whitelist).Should().BeFalse();
        EmailSafetyRules.IsProtectedSender("spammer@company.com.attacker.org", whitelist).Should().BeFalse();
    }

    [Fact]
    public void IsSafeToClean_WhenEmailIsStarred_ShouldReturnFalse()
    {
        var email = new EmailMessage
        {
            Id = "1",
            From = "deals@shopee.vn",
            Labels = new List<string> { "STARRED" },
            IsRead = false
        };

        EmailSafetyRules.IsSafeToClean(email, null).Should().BeFalse();
    }

    [Fact]
    public void IsSafeToClean_WhenEmailIsRead_ShouldReturnFalse_PreservingReadEmails()
    {
        var email = new EmailMessage
        {
            Id = "2",
            From = "deals@shopee.vn",
            Labels = new List<string>(),
            IsRead = true
        };

        // User explicit requirement: Keep all read emails!
        EmailSafetyRules.IsSafeToClean(email, null).Should().BeFalse();
    }

    [Fact]
    public void IsSafeToClean_WhenEmailIsUnreadAndUnstarredPromo_ShouldReturnTrue()
    {
        var email = new EmailMessage
        {
            Id = "3",
            From = "deals@shopee.vn",
            Labels = new List<string>(),
            IsRead = false
        };

        EmailSafetyRules.IsSafeToClean(email, null).Should().BeTrue();
    }

    [Fact]
    public void IsEmailMatchingRegex_WithSafeTimeout_ShouldHandleRegexCorrectly()
    {
        var email = new EmailMessage
        {
            Id = "4",
            Subject = "Siêu sale 9.9 giảm 50%",
            From = "deals@shopee.vn"
        };

        var rule = new CleanupRule
        {
            SubjectRegex = @"(?i)siêu\s+sale",
            SenderRegex = @"(?i)@shopee\.vn"
        };

        EmailSafetyRules.IsEmailMatchingRegex(email, rule).Should().BeTrue();
    }

    [Fact]
    public void IsEmailMatchingRegex_WithMalformedRegex_ShouldNotThrow()
    {
        var email = new EmailMessage
        {
            Id = "5",
            Subject = "Test subject",
            From = "test@example.com"
        };

        var malformedRule = new CleanupRule
        {
            SubjectRegex = @"[unclosed bracket"
        };

        var action = () => EmailSafetyRules.IsEmailMatchingRegex(email, malformedRule);
        action.Should().NotThrow();
        action().Should().BeFalse();
    }

    [Fact]
    public void IsEmailMatchingRegex_WhenSenderMatches_ButSubjectDoesNot_ShouldReturnFalse_PreventingAccidentalDeletion()
    {
        // Scenario: Rule targets Vercel deploy bot notifications from GitHub, but email is an important PR notification
        var email = new EmailMessage
        {
            Id = "6",
            Subject = "Re: [ThienHN0910/ACVIS] fix: critical security patch (PR #208)",
            From = "notifications@github.com"
        };

        var vercelDeployRule = new CleanupRule
        {
            RuleName = "Vercel Deploy Bot",
            SenderRegex = @"(?i)notifications@github\.com",
            SubjectRegex = @"(?i).*(vercel\[bot\]|Deployment\s+Ready).*"
        };

        // Strict AND logic must protect this email from being deleted!
        EmailSafetyRules.IsEmailMatchingRegex(email, vercelDeployRule).Should().BeFalse();
    }

    [Fact]
    public void IsEmailMatchingRegex_WhenSubjectMatches_ButSenderDoesNot_ShouldReturnFalse()
    {
        var email = new EmailMessage
        {
            Id = "7",
            Subject = "[QC] Weekly Special Offer",
            From = "boss@company.com"
        };

        var promoRule = new CleanupRule
        {
            RuleName = "Marketing Promo",
            SenderRegex = @"(?i)promo@spammer\.com",
            SubjectRegex = @"(?i).*\[QC\].*"
        };

        EmailSafetyRules.IsEmailMatchingRegex(email, promoRule).Should().BeFalse();
    }

    [Fact]
    public void IsEmailMatchingRegex_WhenBothSenderAndSubjectMatch_ShouldReturnTrue()
    {
        var email = new EmailMessage
        {
            Id = "8",
            Subject = "[vercel] Deployment Completed on ThienHN0910/app",
            From = "notifications@github.com"
        };

        var vercelDeployRule = new CleanupRule
        {
            RuleName = "Vercel Deploy Bot",
            SenderRegex = @"(?i)notifications@github\.com",
            SubjectRegex = @"(?i).*(vercel\[bot\]|\[vercel\]|Deployment\s+Completed).*"
        };

        EmailSafetyRules.IsEmailMatchingRegex(email, vercelDeployRule).Should().BeTrue();
    }

    [Fact]
    public void IsEmailMatchingRegex_WhenNoRegexIsConfigured_ShouldReturnFalse()
    {
        var email = new EmailMessage
        {
            Id = "9",
            Subject = "Test",
            From = "user@example.com"
        };

        var emptyRule = new CleanupRule();
        EmailSafetyRules.IsEmailMatchingRegex(email, emptyRule).Should().BeFalse();
    }

    [Theory]
    [InlineData(@"(?i).*(sale|giảm giá).*", true)]
    [InlineData(@"^\[Quảng cáo\]", true)]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(@"[unclosed bracket", false)]
    [InlineData(@"((unclosed group", false)]
    public void IsValidRegex_ShouldValidateSyntaxProperly(string? pattern, bool expected)
    {
        EmailSafetyRules.IsValidRegex(pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("(?i).*grab.*", "(?i).*grabfood.*", true)] // short brand subset
    [InlineData("(?i).*tiki.*", "(?i).*(tiki|tikinow).*", true)] // brand in OR branch
    [InlineData("deals@lazada.vn", "deals@lazada.vn", true)] // exact
    [InlineData("deals@lazada.vn", "deals@shopee.vn", false)] // different brands
    public void AreRegexPatternsSimilar_ShouldDetectDuplicatesProperly(string p1, string p2, bool expected)
    {
        EmailSafetyRules.AreRegexPatternsSimilar(p1, p2).Should().Be(expected);
    }

    [Fact]
    public void IsDuplicateRule_CrossField_DoesNotConfuseSenderAndSubject()
    {
        var existingRules = new List<CleanupRule>
        {
            new()
            {
                RuleName = "Block Lazada Sender",
                SenderRegex = @"(?i).*@lazada\.vn.*",
                SubjectRegex = null,
                IsActive = true
            }
        };

        // New suggestion is for Subject "Lazada", not Sender "Lazada"
        // It should NOT be flagged as duplicate
        var isDuplicate = EmailSafetyRules.IsDuplicateRule(
            subjectRegex: @"(?i).*lazada.*",
            senderRegex: null,
            existingRules);

        isDuplicate.Should().BeFalse();
    }

    [Fact]
    public void IsDuplicateRule_WhenSenderCoveredByBroaderExistingRule_ReturnsTrue()
    {
        var existingRules = new List<CleanupRule>
        {
            new()
            {
                RuleName = "Block All Shopee",
                SenderRegex = @"(?i).*@shopee\.vn.*",
                SubjectRegex = null,
                IsActive = true
            }
        };

        // New rule targets Shopee with a specific subject "Khuyến mãi"
        // Since all Shopee emails are already cleaned, this rule is redundant
        var isDuplicate = EmailSafetyRules.IsDuplicateRule(
            subjectRegex: @"(?i).*khuyến mãi.*",
            senderRegex: @"(?i).*@shopee\.vn.*",
            existingRules);

        isDuplicate.Should().BeTrue();
    }

    [Fact]
    public void IsDuplicateRule_WhenIdenticalRuleExists_ReturnsTrue()
    {
        var existingRules = new List<CleanupRule>
        {
            new()
            {
                RuleName = "Grab Food Deals",
                SenderRegex = @"(?i).*@grab\.com.*",
                SubjectRegex = @"(?i).*khuyến mãi.*",
                IsActive = true
            }
        };

        var isDuplicate = EmailSafetyRules.IsDuplicateRule(
            subjectRegex: @"(?i).*khuyến mãi.*",
            senderRegex: @"(?i).*@grab\.com.*",
            existingRules);

        isDuplicate.Should().BeTrue();
    }
}
