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

    [Fact]
    public async Task PendingReviewWinsOverMatchingApprovedRegex()
    {
        var reviews = Substitute.For<IRepository<CleanupReview>>();
        reviews.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupReview, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupReview> { new() { EmailId = "m-pending", Status = CleanupReviewStatus.Pending } });
        var feedback = Substitute.For<IRepository<CleanupFeedback>>();
        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule> { new() { RuleName = "Promo", SenderRegex = "seller@example.com", SubjectRegex = "Sale",
                IsActive = true, ApprovalStatus = CleanupRuleApprovalStatus.Approved } });
        _gmailService.GetEmailsAsync(Arg.Any<string>(), 100, Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { new() { Id = "m-pending", From = "seller@example.com", Subject = "Sale" } });
        var job = new EmailCleanupBackgroundJob(_ruleRepo, _logRepo, _actionLogRepo, _gmailService,
            _aiService, _usageTracker, _notificationService, _logger, feedback, reviews);

        await job.RunAutoCleanupAsync();

        await _gmailService.DidNotReceive().TrashEmailAsync("m-pending", Arg.Any<CancellationToken>());
        await _aiService.DidNotReceiveWithAnyArgs().AnalyzeCleanupBatchAsync(default!, default!, default);
    }

    [Fact]
    public async Task NewAiDeletionTypeCreatesReviewInsteadOfTrashing()
    {
        var reviews = Substitute.For<IRepository<CleanupReview>>();
        reviews.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupReview, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupReview>());
        var feedback = Substitute.For<IRepository<CleanupFeedback>>();
        feedback.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<CleanupFeedback>());
        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule>());
        _gmailService.GetEmailsAsync(Arg.Any<string>(), 100, Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { new() { Id = "m-new", From = "seller@example.com", Subject = "Sale" } });
        _usageTracker.CanRunBackgroundAiAsync(Arg.Any<CancellationToken>()).Returns(true);
        _aiService.AnalyzeCleanupBatchAsync(Arg.Any<IReadOnlyList<EmailMessage>>(), Arg.Any<IReadOnlyList<CleanupFeedback>>(), Arg.Any<CancellationToken>())
            .Returns(new List<AICleanupDecision> { new() { EmailId = "m-new", Outcome = AICleanupOutcome.Trash, Reason = "promotion" } });
        var job = new EmailCleanupBackgroundJob(_ruleRepo, _logRepo, _actionLogRepo, _gmailService,
            _aiService, _usageTracker, _notificationService, _logger, feedback, reviews);

        await job.RunAutoCleanupAsync();

        await _gmailService.DidNotReceive().TrashEmailAsync("m-new", Arg.Any<CancellationToken>());
        await reviews.Received(1).CreateAsync(Arg.Is<CleanupReview>(x => x.EmailId == "m-new" && x.Status == CleanupReviewStatus.Pending), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task KnownExactPreferenceMayTrashButNeverArchive()
    {
        var reviews = Substitute.For<IRepository<CleanupReview>>();
        reviews.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupReview, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupReview>());
        var feedback = Substitute.For<IRepository<CleanupFeedback>>();
        feedback.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<CleanupFeedback>
        {
            new() { Id = "f1", Sender = "notifications@vercel.com", Subject = "Failed deployment for alpha",
                Decision = CleanupDecision.Trash, Reason = "I retest in production" }
        });
        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule>());
        _gmailService.GetEmailsAsync(Arg.Any<string>(), 100, Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { new() { Id = "m-known", From = "notifications@vercel.com", Subject = "Failed deployment for alpha" } });
        _usageTracker.CanRunBackgroundAiAsync(Arg.Any<CancellationToken>()).Returns(true);
        _aiService.AnalyzeCleanupBatchAsync(Arg.Any<IReadOnlyList<EmailMessage>>(), Arg.Any<IReadOnlyList<CleanupFeedback>>(), Arg.Any<CancellationToken>())
            .Returns(new List<AICleanupDecision> { new() { EmailId = "m-known", Outcome = AICleanupOutcome.Trash,
                Reason = "same notification", FeedbackIds = new List<string> { "f1" } } });
        var job = new EmailCleanupBackgroundJob(_ruleRepo, _logRepo, _actionLogRepo, _gmailService,
            _aiService, _usageTracker, _notificationService, _logger, feedback, reviews);

        await job.RunAutoCleanupAsync();

        await _gmailService.Received(1).TrashEmailAsync("m-known", Arg.Any<CancellationToken>());
        await _gmailService.DidNotReceiveWithAnyArgs().ArchiveEmailAsync(default!, default);
    }

    [Fact]
    public async Task VerifiedVercelFailureWithActionRequiredCanUseApprovedRule()
    {
        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule> { new() { SenderRegex = "^notifications@vercel\\.com$",
                SubjectRegex = "Failed deployment", IsActive = true,
                ApprovalStatus = CleanupRuleApprovalStatus.Approved } });
        _gmailService.GetEmailsAsync(Arg.Any<string>(), 100, Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { new() { Id = "vercel-failed", From = "notifications@vercel.com",
                Subject = "[Action Required] Failed deployment for alpha" } });

        await CreateJob().RunAutoCleanupAsync();

        await _gmailService.Received(1).TrashEmailAsync("vercel-failed", Arg.Any<CancellationToken>());
        await _aiService.DidNotReceiveWithAnyArgs().AnalyzeUrgentEmailAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task KeepPreferenceForSameSenderAndSubjectBlocksOlderApprovedRegex()
    {
        var feedback = Substitute.For<IRepository<CleanupFeedback>>();
        feedback.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<CleanupFeedback>
        {
            new() { EmailId = "old-mail", Sender = "team@example.com", Subject = "Weekly report",
                Decision = CleanupDecision.Keep, Reason = "Need to read it" }
        });
        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule> { new() { SenderRegex = "team@example.com", SubjectRegex = "Weekly report",
                IsActive = true, ApprovalStatus = CleanupRuleApprovalStatus.Approved } });
        _gmailService.GetEmailsAsync(Arg.Any<string>(), 100, Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { new() { Id = "new-mail", From = "team@example.com", Subject = "Weekly report" } });
        var job = new EmailCleanupBackgroundJob(_ruleRepo, _logRepo, _actionLogRepo, _gmailService,
            _aiService, _usageTracker, _notificationService, _logger, feedback);

        await job.RunAutoCleanupAsync();

        await _gmailService.DidNotReceive().TrashEmailAsync("new-mail", Arg.Any<CancellationToken>());
    }

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
            ApprovalStatus = CleanupRuleApprovalStatus.Approved,
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
    public async Task RunAutoCleanupAsync_WhenActionRequiredEmail_ShouldAnalyzeWithAI_SendTelegramAndNotTrash()
    {
        // Arrange
        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule>());

        var urgentEmail = new EmailMessage
        {
            Id = "urgent-email-1",
            From = "notifications@vercel.com",
            Subject = "[Action Required] Node.js 20 is being discontinued on October 1st, 2026",
            Snippet = "Please upgrade to Node.js 24 immediately",
            IsRead = false
        };

        _gmailService.GetEmailsAsync("is:unread in:inbox -is:starred", 100, Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { urgentEmail });

        _actionLogRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<EmailActionLog, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailActionLog>());

        _aiService.AnalyzeUrgentEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new UrgentEmailAnalysisResult
            {
                UrgencyLevel = "Khẩn cấp",
                ActionSummary = "Nâng cấp lên Node.js 24 trước 01/10/2026",
                Deadline = "01/10/2026"
            });

        var job = CreateJob();

        // Act
        var result = await job.RunAutoCleanupAsync();

        // Assert:
        // 1. Should never trash or archive urgent email
        await _gmailService.DidNotReceive().TrashEmailAsync("urgent-email-1", Arg.Any<CancellationToken>());
        await _gmailService.DidNotReceive().ArchiveEmailAsync("urgent-email-1", Arg.Any<CancellationToken>());

        // 2. Should call AI analysis round
        await _aiService.Received(1).AnalyzeUrgentEmailAsync(urgentEmail.Subject, urgentEmail.From, Arg.Any<string>(), Arg.Any<CancellationToken>());

        // 3. Should send Telegram alert
        await _notificationService.Received(1).SendNotificationAsync(
            Arg.Is<string>(s => s.Contains("CẦN HÀNH ĐỘNG")),
            Arg.Is<string>(s => s.Contains("Node.js 20 is being discontinued") && s.Contains("Khẩn cấp")),
            "warning",
            Arg.Any<CancellationToken>());

        // 4. Should log action as UrgentNotified
        await _actionLogRepo.Received(1).CreateAsync(
            Arg.Is<EmailActionLog>(l => l.EmailId == "urgent-email-1" && l.Action == "UrgentNotified"),
            Arg.Any<CancellationToken>());
    }

}
