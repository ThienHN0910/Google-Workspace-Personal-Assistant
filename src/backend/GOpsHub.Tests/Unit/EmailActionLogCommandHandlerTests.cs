using FluentAssertions;
using GOpsHub.Application.Features.EmailOps.Commands;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class EmailActionLogCommandHandlerTests
{
    private readonly IRepository<EmailActionLog> _actionLogRepo = Substitute.For<IRepository<EmailActionLog>>();

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

}
