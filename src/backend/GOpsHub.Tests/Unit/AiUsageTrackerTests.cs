using System.Linq.Expressions;
using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using GOpsHub.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class AiUsageTrackerTests
{
    private readonly IRepository<AiTokenUsageMonthly> _usageRepo = Substitute.For<IRepository<AiTokenUsageMonthly>>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly ILogger<AiUsageTracker> _logger = Substitute.For<ILogger<AiUsageTracker>>();
    private readonly IRepository<AppConfiguration> _configRepo = Substitute.For<IRepository<AppConfiguration>>();

    [Fact]
    public async Task RecordUsageAsync_ShouldAccumulateTokensAndFeatureBreakdown()
    {
        // Arrange
        var existingRecord = new AiTokenUsageMonthly
        {
            YearMonth = DateTime.UtcNow.ToString("yyyy-MM"),
            TotalTokens = 1000,
            PromptTokens = 800,
            CandidatesTokens = 200,
            MonthlyQuotaLimit = 250_000,
            WarningThreshold = 200_000,
            FeatureBreakdown = new Dictionary<string, long> { ["EmailReply"] = 1000 }
        };

        _usageRepo.FindOneAsync(Arg.Any<Expression<Func<AiTokenUsageMonthly, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(existingRecord);

        var tracker = new AiUsageTracker(_usageRepo, _notificationService, _logger, _configRepo);

        // Act
        var result = await tracker.RecordUsageAsync("BankTelemetry", 500, 250, 750);

        // Assert
        result.TotalTokens.Should().Be(1750);
        result.PromptTokens.Should().Be(1300);
        result.CandidatesTokens.Should().Be(450);
        result.FeatureBreakdown["BankTelemetry"].Should().Be(750);
        result.FeatureBreakdown["EmailReply"].Should().Be(1000);
        result.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task CanRunBackgroundAiAsync_ShouldAlwaysReturnTrue_AsMonthlyQuotaIsRemoved()
    {
        // Arrange: even if tokens exceed previous 250k limit
        var record = new AiTokenUsageMonthly
        {
            YearMonth = DateTime.UtcNow.ToString("yyyy-MM"),
            TotalTokens = 999_999,
            MonthlyQuotaLimit = 250_000
        };

        _usageRepo.FindOneAsync(Arg.Any<Expression<Func<AiTokenUsageMonthly, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(record);

        var tracker = new AiUsageTracker(_usageRepo, _notificationService, _logger, _configRepo);

        // Act
        var canRun = await tracker.CanRunBackgroundAiAsync();

        // Assert: Never blocked by monthly token count
        canRun.Should().BeTrue();
    }

    [Fact]
    public async Task RecordUsageAsync_ShouldNotTriggerMonthlyQuotaWarningNotification()
    {
        // Arrange
        var record = new AiTokenUsageMonthly
        {
            YearMonth = DateTime.UtcNow.ToString("yyyy-MM"),
            TotalTokens = 199_000,
            MonthlyQuotaLimit = 250_000,
            WarningThreshold = 200_000,
            WarningSent = false
        };

        _usageRepo.FindOneAsync(Arg.Any<Expression<Func<AiTokenUsageMonthly, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(record);

        var tracker = new AiUsageTracker(_usageRepo, _notificationService, _logger, _configRepo);

        // Act
        var result = await tracker.RecordUsageAsync("EmailCleanup", 1500, 500, 2000);

        // Assert
        result.TotalTokens.Should().Be(201_000);
        // Monthly warning alerts are removed; no notification should be sent from monthly tracker
        await _notificationService.DidNotReceiveWithAnyArgs().SendNotificationAsync(
            default!,
            default!,
            default!,
            default);
    }
}
