using FluentAssertions;
using GOpsHub.Application.Features.Scheduling;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class ScheduleCommandHandlerTests
{
    private readonly IRepository<ExtractedSchedule> _scheduleRepo = Substitute.For<IRepository<ExtractedSchedule>>();

    [Fact]
    public async Task DeleteExtractedScheduleCommandHandler_ShouldDeleteSchedule_WhenExists()
    {
        // Arrange
        var scheduleId = "sched-123";
        var existing = new ExtractedSchedule
        {
            Id = scheduleId,
            Title = "Interview with Google",
            Status = ScheduleStatus.PendingConfirm
        };

        _scheduleRepo.GetByIdAsync(scheduleId, Arg.Any<CancellationToken>()).Returns(existing);

        var handler = new DeleteExtractedScheduleCommandHandler(_scheduleRepo);
        var command = new DeleteExtractedScheduleCommand(scheduleId);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Should().BeTrue();
        await _scheduleRepo.Received(1).DeleteAsync(scheduleId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteExtractedScheduleCommandHandler_ShouldThrowKeyNotFoundException_WhenNotExists()
    {
        // Arrange
        var scheduleId = "sched-non-existent";
        _scheduleRepo.GetByIdAsync(scheduleId, Arg.Any<CancellationToken>()).Returns((ExtractedSchedule?)null);

        var handler = new DeleteExtractedScheduleCommandHandler(_scheduleRepo);
        var command = new DeleteExtractedScheduleCommand(scheduleId);

        // Act
        var act = async () => await handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{scheduleId}*");
        await _scheduleRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
