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
        var (minuteCount, currentTpm, dailyCount, remainingDaily) = limiter.GetStatus();

        // Assert: 498 RPD - 1 = 497
        minuteCount.Should().Be(1);
        dailyCount.Should().Be(1);
        remainingDaily.Should().Be(497);
        currentTpm.Should().Be(0);
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

        var (minuteCount, currentTpm, dailyCount, remainingDaily) = limiter.GetStatus();

        // Assert: 498 RPD - 5 = 493
        minuteCount.Should().Be(5);
        dailyCount.Should().Be(5);
        remainingDaily.Should().Be(493);
    }

    [Fact]
    public void RecordInputTokens_WhenUnder200k_ShouldReturnFalse()
    {
        var limiter = new GeminiRateLimiter();

        bool isPeak = limiter.RecordInputTokens(150_000);

        isPeak.Should().BeFalse();
        var (_, tpm, _, _) = limiter.GetStatus();
        tpm.Should().Be(150_000);
    }

    [Fact]
    public void RecordInputTokens_WhenReaching200k_ShouldTriggerPeakWarning()
    {
        var limiter = new GeminiRateLimiter();

        bool isPeak = limiter.RecordInputTokens(200_000);

        isPeak.Should().BeTrue();
        var (_, tpm, _, _) = limiter.GetStatus();
        tpm.Should().Be(200_000);
    }

    [Fact]
    public void RecordInputTokens_WithinCooldown_ShouldNotTriggerPeakWarningTwice()
    {
        var limiter = new GeminiRateLimiter();

        bool firstPeak = limiter.RecordInputTokens(205_000);
        bool secondPeak = limiter.RecordInputTokens(10_000);

        firstPeak.Should().BeTrue();
        secondPeak.Should().BeFalse(); // Suppressed by 5-minute cooldown
    }
}
