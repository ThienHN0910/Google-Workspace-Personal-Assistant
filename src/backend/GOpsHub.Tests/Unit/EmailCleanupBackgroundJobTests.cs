using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.EmailOps;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class EmailCleanupBackgroundJobTests
{
    private readonly IRepository<CleanupRule> _ruleRepo = Substitute.For<IRepository<CleanupRule>>();
    private readonly IRepository<CleanupLog> _logRepo = Substitute.For<IRepository<CleanupLog>>();
    private readonly IRepository<EmailActionLog> _actionLogRepo = Substitute.For<IRepository<EmailActionLog>>();
    private readonly IGmailService _gmailService = Substitute.For<IGmailService>();
    private readonly IAIService _aiService = Substitute.For<IAIService>();
    private readonly IAiUsageTracker _usageTracker = Substitute.For<IAiUsageTracker>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly ILogger<EmailCleanupBackgroundJob> _logger = Substitute.For<ILogger<EmailCleanupBackgroundJob>>();

    private EmailCleanupBackgroundJob CreateJob()
    {
        return new EmailCleanupBackgroundJob(
            _ruleRepo,
            _logRepo,
            _actionLogRepo,
            _gmailService,
            _aiService,
            _usageTracker,
            _notificationService,
            _logger);
    }

    [Fact]
    public async Task RunAutoCleanupAsync_ShouldQueryUnreadInboxOnly_AndReturnIfEmpty()
    {
        // Arrange
        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule>());

        _gmailService.GetEmailsAsync("is:unread in:inbox -is:starred", 100, Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage>());

        var job = CreateJob();

        // Act
        await job.RunAutoCleanupAsync();

        // Assert
        await _gmailService.Received(1).GetEmailsAsync("is:unread in:inbox -is:starred", 100, Arg.Any<CancellationToken>());
        await _aiService.DidNotReceiveWithAnyArgs().AnalyzeSpamPatternsAsync(default!, default);
    }

    [Fact]
    public async Task RunAutoCleanupAsync_WhenRegexMatches_ShouldCleanAndNotCallAI()
    {
        // Arrange
        var regexRule = new CleanupRule
        {
            Id = "rule-1",
            RuleName = "Shopee Promo",
            SubjectRegex = "khuyến mãi",
            Action = CleanupAction.Trash,
            IsActive = true
        };

        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule> { regexRule });

        var unreadEmail = new EmailMessage
        {
            Id = "email-1",
            From = "deals@shopee.vn",
            Subject = "Siêu khuyến mãi 9.9",
            Snippet = "Giảm giá 50%..."
        };

        _gmailService.GetEmailsAsync("is:unread in:inbox -is:starred", 100, Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { unreadEmail });

        _actionLogRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<EmailActionLog, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailActionLog>());

        var job = CreateJob();

        // Act
        await job.RunAutoCleanupAsync();

        // Assert
        await _gmailService.Received(1).TrashEmailAsync("email-1", Arg.Any<CancellationToken>());
        await _actionLogRepo.Received(1).CreateAsync(Arg.Is<EmailActionLog>(l => l.EmailId == "email-1" && l.Action == "Trashed"), Arg.Any<CancellationToken>());
        await _aiService.DidNotReceiveWithAnyArgs().AnalyzeSpamPatternsAsync(default!, default);
    }

    [Fact]
    public async Task RunAutoCleanupAsync_WhenAIUncertain_ShouldMarkAsPendingApproval()
    {
        // Arrange
        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule>());

        var unreadEmail = new EmailMessage
        {
            Id = "email-2",
            From = "newsletter@medium.com",
            Subject = "Weekly Digest",
            Snippet = "Here are your top stories..."
        };

        _gmailService.GetEmailsAsync("is:unread in:inbox -is:starred", 100, Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { unreadEmail });

        _actionLogRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<EmailActionLog, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailActionLog>());

        _usageTracker.CanRunBackgroundAiAsync(Arg.Any<CancellationToken>()).Returns(true);

        _aiService.AnalyzeSpamPatternsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AIRegexRuleSuggestion
            {
                HasPattern = true,
                Category = "Medium Digest",
                SuggestedSubjectRegex = "Weekly Digest",
                TargetEmailIds = new List<string> { "email-2" },
                Reason = "Possible newsletter but might contain desired articles",
                ConfidenceScore = 0.65 // < 0.85 => Uncertain
            });

        var job = CreateJob();

        // Act
        await job.RunAutoCleanupAsync();

        // Assert: Should NOT auto-trash on Gmail
        await _gmailService.DidNotReceive().TrashEmailAsync("email-2", Arg.Any<CancellationToken>());

        // Assert: Should create ActionLog with PendingApproval
        await _actionLogRepo.Received(1).CreateAsync(
            Arg.Is<EmailActionLog>(l => l.EmailId == "email-2" && l.Action == "PendingApproval"),
            Arg.Any<CancellationToken>());

        // Assert: Should send alert for pending approval
        await _notificationService.Received(1).SendNotificationAsync(
            Arg.Is<string>(t => t.Contains("chờ")),
            Arg.Any<string>(),
            "warning",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAutoCleanupAsync_WhenAIHighConfidence_ShouldCreateInactiveRuleAndNotifyTelegram()
    {
        // Arrange
        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule>());

        var unreadEmail = new EmailMessage
        {
            Id = "email-3",
            From = "newsletter@spammybrand.com",
            Subject = "Flash Sale 70% Off Today",
            Snippet = "Big deals for you today..."
        };

        _gmailService.GetEmailsAsync("is:unread in:inbox -is:starred", 100, Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { unreadEmail });

        _actionLogRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<EmailActionLog, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailActionLog>());

        _usageTracker.CanRunBackgroundAiAsync(Arg.Any<CancellationToken>()).Returns(true);

        _aiService.AnalyzeSpamPatternsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AIRegexRuleSuggestion
            {
                HasPattern = true,
                Category = "Flash Sale",
                SuggestedSubjectRegex = @"(?i).*flash\s+sale.*",
                SuggestedSenderRegex = @"(?i).*@spammybrand\.com.*",
                Action = "Trash",
                TargetEmailIds = new List<string> { "email-3" },
                Reason = "Repetitive flash sales",
                ConfidenceScore = 0.92 // >= 0.85
            });

        var job = CreateJob();

        // Act
        await job.RunAutoCleanupAsync();

        // Assert: Rule must be created with IsActive = false (Safe default)
        await _ruleRepo.Received(1).CreateAsync(
            Arg.Is<CleanupRule>(r =>
                r.RuleName.Contains("Flash Sale") &&
                r.IsActive == false && // Crucial safety assertion
                r.IsAutoLearned == true &&
                r.SubjectRegex == @"(?i).*flash\s+sale.*" &&
                r.SenderRegex == @"(?i).*@spammybrand\.com.*" &&
                r.Action == CleanupAction.Trash),
            Arg.Any<CancellationToken>());

        // Assert: Target email should be cleaned
        await _gmailService.Received(1).TrashEmailAsync("email-3", Arg.Any<CancellationToken>());

        // Assert: Notification sent with activation instruction and interactive buttons
        await _notificationService.Received(1).SendNotificationAsync(
            Arg.Is<string>(t => t.Contains("chờ kích hoạt")),
            Arg.Is<string>(m => m.Contains("/enable_rule")),
            "info",
            Arg.Is<List<NotificationButtonRow>>(b => b != null && b.Count > 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAutoCleanupAsync_WhenAIRegexMalformed_ShouldNotCreateRule()
    {
        // Arrange
        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule>());

        var unreadEmail = new EmailMessage
        {
            Id = "email-4",
            From = "malformed@spammer.com",
            Subject = "Spam test",
            Snippet = "Test snippet"
        };

        _gmailService.GetEmailsAsync("is:unread in:inbox -is:starred", 100, Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { unreadEmail });

        _actionLogRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<EmailActionLog, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailActionLog>());

        _usageTracker.CanRunBackgroundAiAsync(Arg.Any<CancellationToken>()).Returns(true);

        _aiService.AnalyzeSpamPatternsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AIRegexRuleSuggestion
            {
                HasPattern = true,
                Category = "Broken Regex",
                SuggestedSubjectRegex = @"[unclosed bracket", // Malformed
                SuggestedSenderRegex = null,
                Action = "Trash",
                TargetEmailIds = new List<string> { "email-4" },
                Reason = "Malformed regex test",
                ConfidenceScore = 0.95
            });

        var job = CreateJob();

        // Act
        await job.RunAutoCleanupAsync();

        // Assert: Should NOT create rule due to malformed regex
        await _ruleRepo.DidNotReceive().CreateAsync(Arg.Any<CleanupRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAutoCleanupAsync_WhenAIRuleDuplicate_ShouldNotCreateRule()
    {
        // Arrange
        var existingRule = new CleanupRule
        {
            Id = "rule-existing",
            RuleName = "Block All Shopee",
            SenderRegex = @"(?i).*@shopee\.vn.*",
            IsActive = true
        };

        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule> { existingRule });

        var unreadEmail = new EmailMessage
        {
            Id = "email-5",
            From = "deals@shopee.vn",
            Subject = "Flash Sale Shopee",
            Snippet = "Shopee deal"
        };

        _gmailService.GetEmailsAsync("is:unread in:inbox -is:starred", 100, Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { unreadEmail });

        _actionLogRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<EmailActionLog, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailActionLog>());

        _usageTracker.CanRunBackgroundAiAsync(Arg.Any<CancellationToken>()).Returns(true);

        // AI suggests rule for Shopee sender that's already covered by existingRule
        _aiService.AnalyzeSpamPatternsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AIRegexRuleSuggestion
            {
                HasPattern = true,
                Category = "Shopee Deals",
                SuggestedSubjectRegex = null,
                SuggestedSenderRegex = @"(?i).*@shopee\.vn.*",
                Action = "Trash",
                TargetEmailIds = new List<string> { "email-5" },
                Reason = "Shopee sender",
                ConfidenceScore = 0.95
            });

        var job = CreateJob();

        // Act
        await job.RunAutoCleanupAsync();

        // Assert: Duplicate detected => no new rule inserted
        await _ruleRepo.DidNotReceive().CreateAsync(Arg.Any<CleanupRule>(), Arg.Any<CancellationToken>());
    }
}
