namespace GOpsHub.Infrastructure.AI;

/// <summary>
/// Thread-safe rate limiter enforcing 15 RPM and 500 RPD for Gemini AI API.
/// </summary>
public class GeminiRateLimiter
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Queue<DateTime> _minuteRequestTimestamps = new();
    private DateTime _currentDay = DateTime.UtcNow.Date;
    private int _dailyRequestCount = 0;

    private const int MaxRequestsPerMinute = 15;
    private const int MaxRequestsPerDay = 500;

    public async Task WaitForSlotAsync(CancellationToken ct = default)
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

            // 2. Check daily quota (500 RPD)
            if (_dailyRequestCount >= MaxRequestsPerDay)
            {
                throw new InvalidOperationException(
                    $"Gemini AI daily quota limit reached ({MaxRequestsPerDay} RPD). Operation halted to avoid charges.");
            }

            // 3. Check and clean up sliding minute window (15 RPM)
            while (_minuteRequestTimestamps.Count > 0 && 
                   (now - _minuteRequestTimestamps.Peek()).TotalSeconds >= 60)
            {
                _minuteRequestTimestamps.Dequeue();
            }

            // If 15 requests have been made in the last 60 seconds, wait until the oldest one expires
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

            _minuteRequestTimestamps.Enqueue(DateTime.UtcNow);
            _dailyRequestCount++;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public (int RequestsThisMinute, int RequestsToday, int RemainingToday) GetStatus()
    {
        var now = DateTime.UtcNow;
        var recentCount = _minuteRequestTimestamps.Count(t => (now - t).TotalSeconds < 60);
        return (recentCount, _dailyRequestCount, Math.Max(0, MaxRequestsPerDay - _dailyRequestCount));
    }
}
