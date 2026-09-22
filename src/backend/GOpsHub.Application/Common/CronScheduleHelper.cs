namespace GOpsHub.Application.Common;

/// <summary>
/// Helper to convert interval numbers (minutes, hours) into standard, safe Hangfire/Cronos-compliant cron expressions.
/// Prevents CronFormatException caused by invalid step values (e.g., */60, 0 */24, or values <= 0).
/// </summary>
public static class CronScheduleHelper
{
    /// <summary>
    /// Converts a minute interval to a valid cron expression.
    /// Handles minute values from 1..59, multiples of 60 (converted to hours),
    /// values >= 60, and fallbacks for values <= 0.
    /// </summary>
    public static string FromMinutes(int minutes, int defaultMinutes = 30)
    {
        if (minutes <= 0)
        {
            minutes = defaultMinutes > 0 ? defaultMinutes : 30;
        }

        // Sub-hour interval: 1 to 59 minutes
        if (minutes < 60)
        {
            return $"*/{minutes} * * * *";
        }

        // Exactly 60 minutes or larger -> convert to hours
        int hours = minutes / 60;
        return FromHours(hours, defaultHours: 1);
    }

    /// <summary>
    /// Converts an hour interval to a valid cron expression.
    /// Handles hour values from 1..23, multiples of 24 (converted to days),
    /// values >= 24, and fallbacks for values <= 0.
    /// </summary>
    public static string FromHours(int hours, int defaultHours = 12)
    {
        if (hours <= 0)
        {
            hours = defaultHours > 0 ? defaultHours : 12;
        }

        if (hours == 1)
        {
            return "0 * * * *";
        }

        if (hours < 24)
        {
            return $"0 */{hours} * * *";
        }

        // 24 hours or larger -> convert to days
        int days = hours / 24;
        if (days <= 1)
        {
            return "0 0 * * *";
        }

        // Cron day-of-month step: 1..31
        days = Math.Clamp(days, 1, 31);
        return $"0 0 */{days} * *";
    }
}
