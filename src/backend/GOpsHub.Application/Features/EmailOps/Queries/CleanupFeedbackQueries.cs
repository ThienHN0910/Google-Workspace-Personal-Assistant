using GOpsHub.Application.Common.CQRS;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;

namespace GOpsHub.Application.Features.EmailOps.Queries;

public record GetCleanupFeedbacksQuery(int Limit = 50) : IQuery<List<CleanupFeedback>>;

public class GetCleanupFeedbacksQueryHandler : IQueryHandler<GetCleanupFeedbacksQuery, List<CleanupFeedback>>
{
    private readonly IRepository<CleanupFeedback> _feedbackRepo;

    public GetCleanupFeedbacksQueryHandler(IRepository<CleanupFeedback> feedbackRepo)
    {
        _feedbackRepo = feedbackRepo;
    }

    public async Task<List<CleanupFeedback>> HandleAsync(GetCleanupFeedbacksQuery query, CancellationToken ct = default)
    {
        var items = await _feedbackRepo.GetAllAsync(ct);
        return items
            .OrderByDescending(f => f.CreatedAt)
            .Take(query.Limit)
            .ToList();
    }
}
