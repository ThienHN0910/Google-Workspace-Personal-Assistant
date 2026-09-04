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
    public async Task CanRunBackgroundAiAsync_ShouldReturnFalse_WhenQuotaExceeded()
    {
        // Arrange
        var record = new AiTokenUsageMonthly
        {
            YearMonth = DateTime.UtcNow.ToString("yyyy-MM"),
            TotalTokens = 250_001,
            MonthlyQuotaLimit = 250_000,
            WarningThreshold = 200_000
        };

        _usageRepo.FindOneAsync(Arg.Any<Expression<Func<AiTokenUsageMonthly, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(record);

        var tracker = new AiUsageTracker(_usageRepo, _notificationService, _logger, _configRepo);

        // Act
        var canRun = await tracker.CanRunBackgroundAiAsync();

        // Assert
        canRun.Should().BeFalse();
    }

    [Fact]
    public async Task RecordUsageAsync_ShouldTriggerWarning_WhenReaching200kTokens()
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
        result.WarningSent.Should().BeTrue();
        await _notificationService.Received(1).SendNotificationAsync(
            Arg.Is<string>(t => t.Contains("200k")),
            Arg.Any<string>(),
            "warning",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordUsageAsync_ShouldTriggerQuotaExceeded_WhenReaching250kTokens()
    {
        // Arrange
        var record = new AiTokenUsageMonthly
        {
            YearMonth = DateTime.UtcNow.ToString("yyyy-MM"),
            TotalTokens = 249_000,
            MonthlyQuotaLimit = 250_000,
            WarningThreshold = 200_000,
            WarningSent = true,
            QuotaExceededSent = false
        };

        _usageRepo.FindOneAsync(Arg.Any<Expression<Func<AiTokenUsageMonthly, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(record);

        var tracker = new AiUsageTracker(_usageRepo, _notificationService, _logger, _configRepo);

        // Act
        var result = await tracker.RecordUsageAsync("EmailCleanup", 1000, 500, 1500);

        // Assert
        result.TotalTokens.Should().Be(250_500);
        result.QuotaExceededSent.Should().BeTrue();
        await _notificationService.Received(1).SendNotificationAsync(
            Arg.Is<string>(t => t.Contains("250k")),
            Arg.Any<string>(),
            "critical",
            Arg.Any<CancellationToken>());
    }
}
