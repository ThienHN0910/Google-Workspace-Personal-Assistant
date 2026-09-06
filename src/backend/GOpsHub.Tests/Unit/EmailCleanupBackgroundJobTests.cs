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
}
