using FluentAssertions;
using GOpsHub.Application.Common;
using System.Reflection;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class CronScheduleHelperTests
{
    private static readonly MethodInfo? ValidateCronMethod = typeof(Hangfire.RecurringJobManager)
        .GetMethod("ValidateCronExpression", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

    private static void AssertValidHangfireCron(string expression)
    {
        ValidateCronMethod.Should().NotBeNull();
        Action act = () => ValidateCronMethod!.Invoke(null, new object[] { expression });
        act.Should().NotThrow($"Cron expression '{expression}' should be accepted by Hangfire");
    }

    [Theory]
    [InlineData(1, "*/1 * * * *")]
    [InlineData(5, "*/5 * * * *")]
    [InlineData(30, "*/30 * * * *")]
    [InlineData(50, "*/50 * * * *")]
    [InlineData(59, "*/59 * * * *")]
    public void FromMinutes_SubHourIntervals_ShouldGenerateValidMinuteCron(int minutes, string expected)
    {
        var cron = CronScheduleHelper.FromMinutes(minutes);
        cron.Should().Be(expected);
        AssertValidHangfireCron(cron);
    }

    [Fact]
    public void FromMinutes_SixtyMinutes_ShouldConvertToOneHour()
    {
        // 60 minutes must NOT generate */60 * * * * (which causes CronFormatException)
        var cron = CronScheduleHelper.FromMinutes(60);
        cron.Should().Be("0 * * * *");
        AssertValidHangfireCron(cron);
    }

    [Theory]
    [InlineData(120, "0 */2 * * *")]
    [InlineData(180, "0 */3 * * *")]
    [InlineData(720, "0 */12 * * *")]
    [InlineData(1380, "0 */23 * * *")]
    public void FromMinutes_MultiHourIntervals_ShouldConvertToValidHourCron(int minutes, string expected)
    {
        var cron = CronScheduleHelper.FromMinutes(minutes);
        cron.Should().Be(expected);
        AssertValidHangfireCron(cron);
    }

    [Fact]
    public void FromMinutes_TwentyFourHours_ShouldConvertToDaily()
    {
        var cron = CronScheduleHelper.FromMinutes(1440); // 24 hours
        cron.Should().Be("0 0 * * *");
        AssertValidHangfireCron(cron);
    }

    [Theory]
    [InlineData(0, 30, "*/30 * * * *")]
    [InlineData(-5, 50, "*/50 * * * *")]
    [InlineData(-1, 0, "*/30 * * * *")]
    public void FromMinutes_InvalidOrNonPositive_ShouldUseDefaultFallback(int minutes, int defaultMinutes, string expected)
    {
        var cron = CronScheduleHelper.FromMinutes(minutes, defaultMinutes);
        cron.Should().Be(expected);
        AssertValidHangfireCron(cron);
    }

    [Theory]
    [InlineData(1, "0 * * * *")]
    [InlineData(2, "0 */2 * * *")]
    [InlineData(6, "0 */6 * * *")]
    [InlineData(12, "0 */12 * * *")]
    [InlineData(23, "0 */23 * * *")]
    public void FromHours_ValidHours_ShouldGenerateValidHourCron(int hours, string expected)
    {
        var cron = CronScheduleHelper.FromHours(hours);
        cron.Should().Be(expected);
        AssertValidHangfireCron(cron);
    }

    [Fact]
    public void FromHours_TwentyFourHours_ShouldConvertToDaily()
    {
        // 24 hours must NOT generate 0 */24 * * * (which causes CronFormatException)
        var cron = CronScheduleHelper.FromHours(24);
        cron.Should().Be("0 0 * * *");
        AssertValidHangfireCron(cron);
    }

    [Theory]
    [InlineData(48, "0 0 */2 * *")]
    [InlineData(72, "0 0 */3 * *")]
    public void FromHours_MultiDayIntervals_ShouldConvertToDayCron(int hours, string expected)
    {
        var cron = CronScheduleHelper.FromHours(hours);
        cron.Should().Be(expected);
        AssertValidHangfireCron(cron);
    }

    [Theory]
    [InlineData(0, 12, "0 */12 * * *")]
    [InlineData(-10, 2, "0 */2 * * *")]
    public void FromHours_InvalidOrNonPositive_ShouldUseDefaultFallback(int hours, int defaultHours, string expected)
    {
        var cron = CronScheduleHelper.FromHours(hours, defaultHours);
        cron.Should().Be(expected);
        AssertValidHangfireCron(cron);
    }
}
