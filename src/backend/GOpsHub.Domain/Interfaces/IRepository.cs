using System.Linq.Expressions;
using GOpsHub.Domain.Common;

namespace GOpsHub.Domain.Interfaces;

/// <summary>
/// Generic repository interface for MongoDB collections.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    
    Task<T> CreateAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task DeleteManyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    // Pagination
    Task<(IReadOnlyList<T> Items, long TotalCount)> GetPagedAsync(
        Expression<Func<T, bool>>? filter,
        int page,
        int pageSize,
        Expression<Func<T, object>>? sortBy = null,
        bool sortDescending = true,
        CancellationToken ct = default);
}
