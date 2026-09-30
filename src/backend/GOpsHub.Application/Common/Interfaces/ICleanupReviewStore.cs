using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;

namespace GOpsHub.Application.Common.Interfaces;

public interface ICleanupReviewStore
{
    Task<bool> TryClaimTrashAsync(string reviewId, CancellationToken ct = default);
    Task<bool> TryClaimRecoveryAsync(string reviewId, DateTime olderThan, CancellationToken ct = default);
    Task<bool> TryKeepAsync(string reviewId, string reason, CancellationToken ct = default);
    Task<CleanupReview> CompleteAsync(string reviewId, CleanupReviewStatus status, string reason, CancellationToken ct = default);
    Task<CleanupReview?> GetByIdAsync(string reviewId, CancellationToken ct = default);
}
