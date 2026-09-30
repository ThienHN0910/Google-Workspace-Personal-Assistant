using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Models;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;

namespace GOpsHub.Application.Features.EmailOps.Queries;

public record GetPendingCleanupReviewsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<CleanupReview>>;

public class GetPendingCleanupReviewsQueryHandler : IQueryHandler<GetPendingCleanupReviewsQuery, PagedResult<CleanupReview>>
{
    private readonly IRepository<CleanupReview> _reviews;
    private readonly IRepository<CleanupFeedback> _feedback;
    public GetPendingCleanupReviewsQueryHandler(IRepository<CleanupReview> reviews,
        IRepository<CleanupFeedback> feedback)
    {
        _reviews = reviews;
        _feedback = feedback;
    }

    public async Task<PagedResult<CleanupReview>> HandleAsync(GetPendingCleanupReviewsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var candidates = await _reviews.FindAsync(x => x.Status == CleanupReviewStatus.Pending ||
            x.Status == CleanupReviewStatus.ProcessingTrash || x.Status == CleanupReviewStatus.Kept, ct);
        var keptReviewIds = (await _feedback.FindAsync(x => x.Decision == CleanupDecision.Keep &&
            x.ReviewId != null, ct)).Select(x => x.ReviewId).ToHashSet();
        var unresolved = candidates.Where(x => x.Status != CleanupReviewStatus.Kept ||
            !keptReviewIds.Contains(x.Id)).OrderByDescending(x => x.CreatedAt).ToList();
        return new PagedResult<CleanupReview> { Items = unresolved.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = unresolved.Count, Page = page, PageSize = pageSize };
    }
}
