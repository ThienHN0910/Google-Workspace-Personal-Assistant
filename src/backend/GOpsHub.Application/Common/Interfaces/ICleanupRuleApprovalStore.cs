using GOpsHub.Domain.Entities;

namespace GOpsHub.Application.Common.Interfaces;

public interface ICleanupRuleApprovalStore
{
    Task<CleanupRule?> TryApproveAsync(CleanupRule validated, CancellationToken ct = default);
}
