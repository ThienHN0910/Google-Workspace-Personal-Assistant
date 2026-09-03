using FluentAssertions;
using GOpsHub.Infrastructure.AI;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class GeminiRateLimiterTests
{
    [Fact]
    public async Task WaitForSlotAsync_ShouldTrackMinuteAndDailyCountsCorrectly()
    {
        // Arrange
        var limiter = new GeminiRateLimiter();

        // Act
        await limiter.WaitForSlotAsync();
        var (minuteCount, dailyCount, remainingDaily) = limiter.GetStatus();

        // Assert
        minuteCount.Should().Be(1);
        dailyCount.Should().Be(1);
        remainingDaily.Should().Be(499);
    }

    [Fact]
    public async Task WaitForSlotAsync_MultipleRequests_ShouldRespectMinuteQuota()
    {
        // Arrange
        var limiter = new GeminiRateLimiter();

        // Act - run 5 rapid requests
        for (int i = 0; i < 5; i++)
        {
            await limiter.WaitForSlotAsync();
        }

        var (minuteCount, dailyCount, remainingDaily) = limiter.GetStatus();

        // Assert
        minuteCount.Should().Be(5);
        dailyCount.Should().Be(5);
        remainingDaily.Should().Be(495);
    }
}
