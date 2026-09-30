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
    public GetPendingCleanupReviewsQueryHandler(IRepository<CleanupReview> reviews) => _reviews = reviews;

    public async Task<PagedResult<CleanupReview>> HandleAsync(GetPendingCleanupReviewsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var (items, total) = await _reviews.GetPagedAsync(x => x.Status == CleanupReviewStatus.Pending,
            page, pageSize, x => x.CreatedAt, true, ct);
        return new PagedResult<CleanupReview> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }
}
