using GOpsHub.Domain.Entities;

namespace GOpsHub.Application.Common.Interfaces;

public interface IAiUsageTracker
{
    Task<AiTokenUsageMonthly> RecordUsageAsync(string feature, long promptTokens, long candidatesTokens, long totalTokens, CancellationToken ct = default);
    Task<AiTokenUsageMonthly> GetCurrentMonthlyUsageAsync(CancellationToken ct = default);
    Task<bool> CanRunBackgroundAiAsync(CancellationToken ct = default);
    Task<long> GetRemainingTokensAsync(CancellationToken ct = default);
    Task ResetMonthlyUsageAsync(CancellationToken ct = default);
}
