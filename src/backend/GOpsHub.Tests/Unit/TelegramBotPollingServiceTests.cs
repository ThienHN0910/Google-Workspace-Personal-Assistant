using FluentAssertions;
using GOpsHub.Domain.Entities;
using GOpsHub.Infrastructure.Alerting;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class TelegramBotPollingServiceTests
{
    [Fact]
    public void FormatReadMoreResponse_WhenNoLogs_ShouldReturnEmptyMessage()
    {
        var logs = new List<EmailActionLog>();
        var result = TelegramBotPollingService.FormatReadMoreResponse(logs, DateTime.UtcNow, 0);

        result.Should().Contain("Không có email nào được dọn dẹp");
    }

    [Fact]
    public void FormatReadMoreResponse_WithLogs_ShouldFormatCorrectly()
    {
        var logs = new List<EmailActionLog>
        {
            new()
            {
                Id = "1",
                Sender = "deals@shopee.vn",
                Subject = "Siêu sale 9.9 freeship toàn quốc",
                Action = "Trashed",
                Reason = "RegexMatched: Rule 'Shopee Promo'"
            },
            new()
            {
                Id = "2",
                Sender = "newsletter@medium.com",
                Subject = "Daily tech digest stories for you",
                Action = "Archived",
                Reason = "AiPatternMatched (90%): Category 'Newsletter'"
            }
        };

        var executedAt = new DateTime(2026, 9, 22, 15, 30, 0, DateTimeKind.Utc);
        var result = TelegramBotPollingService.FormatReadMoreResponse(logs, executedAt, 2);

        result.Should().Contain("Chi tiết phiên dọn dẹp [22/09/2026 15:30]:");
        result.Should().Contain("[🗑️ Xóa - Regex]");
        result.Should().Contain("deals@shopee.vn");
        result.Should().Contain("Siêu sale 9.9");
        result.Should().Contain("[📦 Lưu trữ - AI]");
        result.Should().Contain("newsletter@medium.com");
    }

    [Fact]
    public void FormatReadMoreResponse_WithMoreThan15Logs_ShouldDisplayTop15AndRemainingCount()
    {
        var logs = new List<EmailActionLog>();
        for (int i = 1; i <= 20; i++)
        {
            logs.Add(new EmailActionLog
            {
                Id = $"log-{i}",
                Sender = $"spammer{i}@junk.com",
                Subject = $"Spam Subject #{i}",
                Action = "Trashed",
                Reason = "RegexMatched"
            });
        }

        var result = TelegramBotPollingService.FormatReadMoreResponse(logs, DateTime.UtcNow, 20);

        result.Should().Contain("1. [🗑️ Xóa - Regex]");
        result.Should().Contain("15. [🗑️ Xóa - Regex]");
        result.Should().NotContain("16. [🗑️ Xóa - Regex]");
        result.Should().Contain("... và <b>5</b> email khác đã được xử lý.");
    }

    [Fact]
    public void FormatReadMoreResponse_WithSpecialHtmlCharacters_ShouldProperlyEncode()
    {
        var logs = new List<EmailActionLog>
        {
            new()
            {
                Id = "html-1",
                Sender = "\"Vercel Bot\" <bot@vercel.com>",
                Subject = "Build <alert> & deploy fail & succeed",
                Action = "Trashed",
                Reason = "RegexMatched"
            }
        };

        var result = TelegramBotPollingService.FormatReadMoreResponse(logs, DateTime.UtcNow, 1);

        result.Should().Contain("&lt;bot@vercel.com&gt;");
        result.Should().Contain("&lt;alert&gt;");
        result.Should().NotContain("<bot@vercel.com>");
    }

    [Fact]
    public void FormatRulesResponse_WhenNoRules_ShouldReturnEmptyMessage()
    {
        var rules = new List<CleanupRule>();
        var result = TelegramBotPollingService.FormatRulesResponse(rules);

        result.Should().Contain("Hiện chưa có quy tắc dọn dẹp nào");
    }

    [Fact]
    public void FormatRulesResponse_WithActiveAndInactiveRules_ShouldFormatProperly()
    {
        var rules = new List<CleanupRule>
        {
            new()
            {
                Id = "6ab38717857a16ad307239df",
                RuleName = "Shopee Promo",
                SubjectRegex = "(?i).*khuyến mãi.*",
                Action = GOpsHub.Domain.Enums.CleanupAction.Trash,
                IsActive = true,
                IsAutoLearned = false
            },
            new()
            {
                Id = "6ab31692ec3217f352afa7df",
                RuleName = "Tự động học: Lazada",
                SenderRegex = "(?i).*@lazada\\.vn.*",
                Action = GOpsHub.Domain.Enums.CleanupAction.Trash,
                IsActive = false,
                IsAutoLearned = true
            }
        };

        var result = TelegramBotPollingService.FormatRulesResponse(rules);

        result.Should().Contain("Danh sách Quy tắc Dọn dẹp Email:");
        result.Should().Contain("1. <b>Shopee Promo</b>");
        result.Should().Contain("✅ Bật");
        result.Should().Contain("Xóa");
        result.Should().Contain("2. <b>Tự động học: Lazada</b> [AI Học]");
        result.Should().Contain("⏸️ Tắt");
        result.Should().Contain("ID: <code>6ab38717</code>");
        result.Should().Contain("ID: <code>6ab31692</code>");
        result.Should().Contain("/enable_rule 6ab31692");
    }

    [Fact]
    public async Task FindRuleByIdOrPrefixAsync_WithExactId_ShouldReturnRule()
    {
        var ruleRepo = NSubstitute.Substitute.For<GOpsHub.Domain.Interfaces.IRepository<CleanupRule>>();
        var rule = new CleanupRule { Id = "6ab38717857a16ad307239df", RuleName = "Rule 1" };
        ruleRepo.GetByIdAsync("6ab38717857a16ad307239df", Arg.Any<CancellationToken>()).Returns(rule);

        var result = await TelegramBotPollingService.FindRuleByIdOrPrefixAsync(ruleRepo, "6ab38717857a16ad307239df");

        result.Should().NotBeNull();
        result!.RuleName.Should().Be("Rule 1");
    }

    [Fact]
    public async Task FindRuleByIdOrPrefixAsync_WithShortIdPrefix_ShouldReturnMatchingRule()
    {
        var ruleRepo = NSubstitute.Substitute.For<GOpsHub.Domain.Interfaces.IRepository<CleanupRule>>();
        var rule = new CleanupRule { Id = "6ab38717857a16ad307239df", RuleName = "Rule 1" };
        ruleRepo.GetByIdAsync("6ab38717", Arg.Any<CancellationToken>()).Returns((CleanupRule?)null);
        ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule> { rule });

        var result = await TelegramBotPollingService.FindRuleByIdOrPrefixAsync(ruleRepo, "6ab38717");

        result.Should().NotBeNull();
        result!.Id.Should().Be("6ab38717857a16ad307239df");
        result.RuleName.Should().Be("Rule 1");
    }

    [Fact]
    public async Task FindRuleByIdOrPrefixAsync_WhenNotFound_ShouldReturnNull()
    {
        var ruleRepo = NSubstitute.Substitute.For<GOpsHub.Domain.Interfaces.IRepository<CleanupRule>>();
        ruleRepo.GetByIdAsync("notfound", Arg.Any<CancellationToken>()).Returns((CleanupRule?)null);
        ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule>());

        var result = await TelegramBotPollingService.FindRuleByIdOrPrefixAsync(ruleRepo, "notfound");

        result.Should().BeNull();
    }
}
