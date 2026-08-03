namespace GOpsHub.Application.Common.CQRS;

/// <summary>
/// Handler for a query that returns a result.
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}
