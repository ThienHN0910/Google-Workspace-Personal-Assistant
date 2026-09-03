using FluentAssertions;
using GOpsHub.Application.Features.DriveGuard;
using GOpsHub.Application.Features.Settings;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class SettingsCommandHandlerTests
{
    private readonly IRepository<AppConfiguration> _configRepo = Substitute.For<IRepository<AppConfiguration>>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly IRecurringJobManager _recurringJobManager = Substitute.For<IRecurringJobManager>();
    private readonly ILogger<UpdateSystemSettingsCommandHandler> _logger = Substitute.For<ILogger<UpdateSystemSettingsCommandHandler>>();

    [Fact]
    public async Task GetSystemSettingsQuery_ShouldReturnPopulatedDto()
    {
        // Arrange
        _configRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AppConfiguration>
            {
                new() { Key = "DriveGuardIntervalMinutes", Value = "10" },
                new() { Key = "EnableTelegram", Value = "true" },
                new() { Key = "TelegramChatId", Value = "123456" }
            });

        var handler = new GetSystemSettingsQueryHandler(_configRepo, _configuration);

        // Act
        var result = await handler.HandleAsync(new GetSystemSettingsQuery());

        // Assert
        result.DriveGuardIntervalMinutes.Should().Be(10);
        result.EnableTelegram.Should().BeTrue();
        result.TelegramChatId.Should().Be("123456");
    }

    [Fact]
    public async Task UpdateSystemSettingsCommand_ShouldRescheduleHangfireJobs()
    {
        // Arrange
        var handler = new UpdateSystemSettingsCommandHandler(_configRepo, _recurringJobManager, _logger);
        var settings = new SystemSettingsDto
        {
            DriveGuardIntervalMinutes = 7,
            BankTelemetryIntervalMinutes = 20,
            EmailCleanupIntervalHours = 12,
            CalendarExtractorIntervalHours = 2
        };

        // Act
        var result = await handler.HandleAsync(new UpdateSystemSettingsCommand(settings));

        // Assert
        result.Should().BeTrue();
        _recurringJobManager.Received().AddOrUpdate(
            "drive-guard-audit",
            Arg.Any<Hangfire.Common.Job>(),
            "*/7 * * * *",
            Arg.Any<RecurringJobOptions>());
    }
}
