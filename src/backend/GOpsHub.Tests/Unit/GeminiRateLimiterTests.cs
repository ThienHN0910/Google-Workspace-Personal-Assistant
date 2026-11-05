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

    [Fact]
    public async Task WaitForSlotAsync_ConcurrentCallsDoNotDeadlock()
    {
        var limiter = new GeminiRateLimiter();

        // Run 5 concurrent requests with simulated input tokens
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            await limiter.WaitForSlotAsync(estimatedInputTokens: 1000);
            limiter.RecordInputTokens(1000);
        });

        var timeoutTask = Task.Delay(5000);
        var completed = await Task.WhenAny(Task.WhenAll(tasks), timeoutTask);

        completed.Should().NotBe(timeoutTask, "Concurrent requests should complete within 5 seconds without deadlocking");
        var (minuteCount, tpm, dailyCount, _) = limiter.GetStatus();
        minuteCount.Should().Be(5);
        dailyCount.Should().Be(5);
        tpm.Should().Be(5000);
    }
}
