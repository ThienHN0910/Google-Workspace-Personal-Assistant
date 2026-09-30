using FluentAssertions;
using GOpsHub.API.Controllers;
using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class LegacyCleanupActionEndpointsTests
{
    private static EmailOpsController Controller() => new(Substitute.For<IDispatcher>(),
        Substitute.For<IGmailService>(), Substitute.For<IAIService>());

    [Fact]
    public async Task OldApproveEndpointCannotArchiveOrTrashWithoutReason()
    {
        var result = await Controller().ApproveEmailAction("log-1", new ApproveEmailActionRequest { Action = "Archive" });
        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(410);
    }

    [Fact]
    public async Task OldBatchEndpointCannotBypassReview()
    {
        var result = await Controller().BatchPendingEmailActions(new { logIds = new[] { "log-1" }, targetAction = "Trash" });
        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(410);
    }
}
