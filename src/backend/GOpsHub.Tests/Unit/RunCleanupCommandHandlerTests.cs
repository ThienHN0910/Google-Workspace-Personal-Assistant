using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.EmailOps.Commands;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class RunCleanupCommandHandlerTests
{
    private readonly IRepository<CleanupRule> _ruleRepo = Substitute.For<IRepository<CleanupRule>>();
    private readonly IRepository<CleanupLog> _logRepo = Substitute.For<IRepository<CleanupLog>>();
    private readonly IRepository<EmailActionLog> _actionLogRepo = Substitute.For<IRepository<EmailActionLog>>();
    private readonly IGmailService _gmailService = Substitute.For<IGmailService>();
    private readonly IAIService _aiService = Substitute.For<IAIService>();
    private readonly ILogger<RunCleanupCommandHandler> _logger = Substitute.For<ILogger<RunCleanupCommandHandler>>();

    private RunCleanupCommandHandler CreateHandler()
    {
        return new RunCleanupCommandHandler(
            _ruleRepo,
            _logRepo,
            _actionLogRepo,
            _gmailService,
            _aiService,
            _logger);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailFromBank_ShouldNeverTrashOrArchive()
    {
        var rule = new CleanupRule
        {
            Id = "r-1",
            RuleName = "Broad Promo Rule",
            SubjectRegex = ".*", // Match everything
            Action = CleanupAction.Trash,
            IsActive = true
        };

        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule> { rule });

        var bankEmail = new EmailMessage
        {
            Id = "bank-1",
            From = "customercare@vpb.com.vn",
            Subject = "Biến động số dư tài khoản",
            IsRead = false
        };

        _gmailService.GetEmailsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { bankEmail });

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new RunCleanupCommand());

        result.TotalTrashed.Should().Be(0);
        result.TotalSkipped.Should().Be(1);
        await _gmailService.DidNotReceive().TrashEmailAsync("bank-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsStarred_ShouldNeverTrashOrArchive()
    {
        var rule = new CleanupRule
        {
            Id = "r-2",
            RuleName = "Test Rule",
            SubjectRegex = "promo",
            Action = CleanupAction.Trash,
            IsActive = true
        };

        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule> { rule });

        var starredEmail = new EmailMessage
        {
            Id = "starred-1",
            From = "deals@shopee.vn",
            Subject = "Super promo discount",
            Labels = new List<string> { "STARRED" },
            IsRead = false
        };

        _gmailService.GetEmailsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { starredEmail });

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new RunCleanupCommand());

        result.TotalTrashed.Should().Be(0);
        result.TotalSkipped.Should().Be(1);
        await _gmailService.DidNotReceive().TrashEmailAsync("starred-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsRead_ShouldNeverTrashOrArchive()
    {
        var rule = new CleanupRule
        {
            Id = "r-3",
            RuleName = "Promo Rule",
            SubjectRegex = "sale",
            Action = CleanupAction.Trash,
            IsActive = true
        };

        _ruleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupRule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupRule> { rule });

        var readEmail = new EmailMessage
        {
            Id = "read-1",
            From = "newsletter@medium.com",
            Subject = "Flash sale course",
            Labels = new List<string>(),
            IsRead = true // Read email
        };

        _gmailService.GetEmailsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage> { readEmail });

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new RunCleanupCommand());

        result.TotalTrashed.Should().Be(0);
        result.TotalSkipped.Should().Be(1);
        await _gmailService.DidNotReceive().TrashEmailAsync("read-1", Arg.Any<CancellationToken>());
    }
}
