using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Models;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;

namespace GOpsHub.Application.Features.EmailOps.Queries;

public record GetCleanupRulesQuery : IQuery<IReadOnlyList<CleanupRule>>;

public class GetCleanupRulesQueryHandler : IQueryHandler<GetCleanupRulesQuery, IReadOnlyList<CleanupRule>>
{
    private readonly IRepository<CleanupRule> _ruleRepo;

    public GetCleanupRulesQueryHandler(IRepository<CleanupRule> ruleRepo)
    {
        _ruleRepo = ruleRepo;
    }

    public async Task<IReadOnlyList<CleanupRule>> HandleAsync(GetCleanupRulesQuery query, CancellationToken ct = default)
    {
        return await _ruleRepo.GetAllAsync(ct);
    }
}

public record GetPendingDraftsQuery(int Page = 1, int PageSize = 10) : IQuery<PagedResult<AIDraft>>;

public class GetPendingDraftsQueryHandler : IQueryHandler<GetPendingDraftsQuery, PagedResult<AIDraft>>
{
    private readonly IRepository<AIDraft> _draftRepo;

    public GetPendingDraftsQueryHandler(IRepository<AIDraft> draftRepo)
    {
        _draftRepo = draftRepo;
    }

    public async Task<PagedResult<AIDraft>> HandleAsync(GetPendingDraftsQuery query, CancellationToken ct = default)
    {
        var (items, total) = await _draftRepo.GetPagedAsync(
            x => x.Status == DraftStatus.Pending,
            query.Page,
            query.PageSize,
            x => x.CreatedAt,
            true,
            ct);

        return new PagedResult<AIDraft>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

public record GetCleanupLogsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<CleanupLog>>;

public class GetCleanupLogsQueryHandler : IQueryHandler<GetCleanupLogsQuery, PagedResult<CleanupLog>>
{
    private readonly IRepository<CleanupLog> _logRepo;

    public GetCleanupLogsQueryHandler(IRepository<CleanupLog> logRepo)
    {
        _logRepo = logRepo;
    }

    public async Task<PagedResult<CleanupLog>> HandleAsync(GetCleanupLogsQuery query, CancellationToken ct = default)
    {
        var (items, total) = await _logRepo.GetPagedAsync(
            null,
            query.Page,
            query.PageSize,
            x => x.ExecutedAt,
            true,
            ct);

        return new PagedResult<CleanupLog>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
