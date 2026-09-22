namespace GOpsHub.Infrastructure.AI;

/// <summary>
/// Thread-safe rate limiter enforcing 14 RPM, 498 RPD, and 240,000 Input Tokens Per Minute (TPM)
/// with real-time peak threshold detection (200k TPM) for Gemini AI API.
/// </summary>
public class GeminiRateLimiter
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _tokenLock = new();
    private readonly Queue<DateTime> _minuteRequestTimestamps = new();
    private readonly Queue<(DateTime Timestamp, long InputTokens)> _minuteTokenEntries = new();
    private DateTime _currentDay = DateTime.UtcNow.Date;
    private int _dailyRequestCount = 0;
    private DateTime _lastPeakWarningSentAt = DateTime.MinValue;
    private static readonly TimeSpan PeakWarningCooldown = TimeSpan.FromMinutes(5);

    public const int MaxRequestsPerMinute = 14;
    public const int MaxRequestsPerDay = 498;
    public const int MaxInputTokensPerMinute = 240_000;
    public const int InputTokenPeakWarningThreshold = 200_000;

    public async Task WaitForSlotAsync(long estimatedInputTokens = 0, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var now = DateTime.UtcNow;

            // 1. Reset daily counter if a new UTC day has started
            if (now.Date > _currentDay)
            {
                _currentDay = now.Date;
                _dailyRequestCount = 0;
            }

            // 2. Check daily quota (498 RPD)
            if (_dailyRequestCount >= MaxRequestsPerDay)
            {
                throw new InvalidOperationException(
                    $"Gemini AI daily quota limit reached ({MaxRequestsPerDay} RPD). Operation halted to avoid charges.");
            }

            // 3. Check and clean up sliding minute window for requests (14 RPM)
            while (_minuteRequestTimestamps.Count > 0 && 
                   (now - _minuteRequestTimestamps.Peek()).TotalSeconds >= 60)
            {
                _minuteRequestTimestamps.Dequeue();
            }

            // If 14 requests have been made in the last 60 seconds, wait until the oldest one expires
            if (_minuteRequestTimestamps.Count >= MaxRequestsPerMinute)
            {
                var oldest = _minuteRequestTimestamps.Peek();
                var waitTime = TimeSpan.FromSeconds(60) - (now - oldest) + TimeSpan.FromMilliseconds(100);
                if (waitTime > TimeSpan.Zero)
                {
                    await Task.Delay(waitTime, ct);
                }

                // Clean again after waiting
                now = DateTime.UtcNow;
                while (_minuteRequestTimestamps.Count > 0 && 
                       (now - _minuteRequestTimestamps.Peek()).TotalSeconds >= 60)
                {
                    _minuteRequestTimestamps.Dequeue();
                }
            }

            // 4. Check and clean up sliding minute window for input tokens (240,000 TPM)
            lock (_tokenLock)
            {
                CleanOldTokenEntries(now);
                while (_minuteTokenEntries.Count > 0 && 
                       (_minuteTokenEntries.Sum(e => e.InputTokens) + estimatedInputTokens) > MaxInputTokensPerMinute)
                {
                    var oldest = _minuteTokenEntries.Peek();
                    var waitTime = TimeSpan.FromSeconds(60) - (now - oldest.Timestamp) + TimeSpan.FromMilliseconds(100);
                    if (waitTime > TimeSpan.Zero)
                    {
                        Task.Delay(waitTime, ct).GetAwaiter().GetResult();
                    }

                    now = DateTime.UtcNow;
                    CleanOldTokenEntries(now);
                }
            }

            _minuteRequestTimestamps.Enqueue(DateTime.UtcNow);
            _dailyRequestCount++;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Records actual input tokens from a completed API call and returns true if current 1-minute input TPM >= 200,000 (with 5-minute cooldown).
    /// </summary>
    public bool RecordInputTokens(long inputTokens)
    {
        lock (_tokenLock)
        {
            var now = DateTime.UtcNow;
            CleanOldTokenEntries(now);
            _minuteTokenEntries.Enqueue((now, inputTokens));

            long currentTpm = _minuteTokenEntries.Sum(e => e.InputTokens);
            if (currentTpm >= InputTokenPeakWarningThreshold && (now - _lastPeakWarningSentAt) >= PeakWarningCooldown)
            {
                _lastPeakWarningSentAt = now;
                return true;
            }

            return false;
        }
    }

    private void CleanOldTokenEntries(DateTime now)
    {
        while (_minuteTokenEntries.Count > 0 && 
               (now - _minuteTokenEntries.Peek().Timestamp).TotalSeconds >= 60)
        {
            _minuteTokenEntries.Dequeue();
        }
    }

    public (int RequestsThisMinute, long InputTokensThisMinute, int RequestsToday, int RemainingToday) GetStatus()
    {
        var now = DateTime.UtcNow;
        var recentCount = _minuteRequestTimestamps.Count(t => (now - t).TotalSeconds < 60);
        long currentTpm;
        lock (_tokenLock)
        {
            CleanOldTokenEntries(now);
            currentTpm = _minuteTokenEntries.Sum(e => e.InputTokens);
        }

        return (recentCount, currentTpm, _dailyRequestCount, Math.Max(0, MaxRequestsPerDay - _dailyRequestCount));
    }
}
