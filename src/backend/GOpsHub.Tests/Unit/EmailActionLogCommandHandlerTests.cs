using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.EmailOps.Commands;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class EmailActionLogCommandHandlerTests
{
    private readonly IRepository<EmailActionLog> _actionLogRepo = Substitute.For<IRepository<EmailActionLog>>();
    private readonly IGmailService _gmailService = Substitute.For<IGmailService>();
    private readonly ILogger<ApproveEmailActionCommandHandler> _approveLogger = Substitute.For<ILogger<ApproveEmailActionCommandHandler>>();
    private readonly ILogger<DismissEmailActionCommandHandler> _dismissLogger = Substitute.For<ILogger<DismissEmailActionCommandHandler>>();

    [Fact]
    public async Task DeleteEmailActionLogCommand_ShouldCallDeleteAsync()
    {
        var handler = new DeleteEmailActionLogCommandHandler(_actionLogRepo);
        var result = await handler.HandleAsync(new DeleteEmailActionLogCommand("log-123"));

        result.Should().BeTrue();
        await _actionLogRepo.Received(1).DeleteAsync("log-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBatchEmailActionLogsCommand_WithDeleteAll_ShouldCallDeleteManyAsync()
    {
        var handler = new DeleteBatchEmailActionLogsCommandHandler(_actionLogRepo);
        var result = await handler.HandleAsync(new DeleteBatchEmailActionLogsCommand(DeleteAll: true));

        result.Should().BeTrue();
        await _actionLogRepo.Received(1).DeleteManyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<EmailActionLog, bool>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveEmailActionCommand_ShouldTrashEmail_AndUpdateStatus()
    {
        var existingLog = new EmailActionLog
        {
            Id = "log-456",
            EmailId = "gmail-789",
            Subject = "Flash Sale Shopee 50%",
            Action = "PendingApproval",
            Reason = "AiUncertain: Sale off"
        };

        _actionLogRepo.GetByIdAsync("log-456", Arg.Any<CancellationToken>()).Returns(existingLog);

        var handler = new ApproveEmailActionCommandHandler(_actionLogRepo, _gmailService, _approveLogger);
        var result = await handler.HandleAsync(new ApproveEmailActionCommand("log-456", "Trash"));

        result.Action.Should().Be("Trashed");
        result.Reason.Should().Contain("[Người dùng duyệt]");
        await _gmailService.Received(1).TrashEmailAsync("gmail-789", Arg.Any<CancellationToken>());
        await _actionLogRepo.Received(1).UpdateAsync(existingLog, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DismissEmailActionCommand_ShouldUpdateStatusToDismissed()
    {
        var existingLog = new EmailActionLog
        {
            Id = "log-789",
            EmailId = "gmail-101",
            Subject = "Weekly Newsletter",
            Action = "PendingApproval",
            Reason = "AiUncertain: Newsletter"
        };

        _actionLogRepo.GetByIdAsync("log-789", Arg.Any<CancellationToken>()).Returns(existingLog);

        var handler = new DismissEmailActionCommandHandler(_actionLogRepo, _dismissLogger);
        var result = await handler.HandleAsync(new DismissEmailActionCommand("log-789"));

        result.Action.Should().Be("Dismissed");
        result.Reason.Should().Contain("[Người dùng bỏ qua]");
        await _actionLogRepo.Received(1).UpdateAsync(existingLog, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BatchPendingEmailActionCommand_ShouldProcessMultipleLogs_WithTrash()
    {
        var log1 = new EmailActionLog { Id = "l-1", EmailId = "g-1", Action = "PendingApproval" };
        var log2 = new EmailActionLog { Id = "l-2", EmailId = "g-2", Action = "PendingApproval" };

        _actionLogRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<EmailActionLog, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailActionLog> { log1, log2 });

        var logger = Substitute.For<ILogger<BatchPendingEmailActionCommandHandler>>();
        var handler = new BatchPendingEmailActionCommandHandler(_actionLogRepo, _gmailService, logger);

        var result = await handler.HandleAsync(new BatchPendingEmailActionCommand(new List<string> { "l-1", "l-2" }, "Trash"));

        result.TotalRequested.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        log1.Action.Should().Be("Trashed");
        log2.Action.Should().Be("Trashed");
        await _gmailService.Received(1).TrashEmailAsync("g-1", Arg.Any<CancellationToken>());
        await _gmailService.Received(1).TrashEmailAsync("g-2", Arg.Any<CancellationToken>());
    }
}
