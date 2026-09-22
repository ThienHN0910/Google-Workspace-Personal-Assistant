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
}
