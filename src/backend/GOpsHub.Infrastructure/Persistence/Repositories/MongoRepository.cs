using System.Linq.Expressions;
using GOpsHub.Domain.Common;
using GOpsHub.Domain.Interfaces;
using MongoDB.Driver;

namespace GOpsHub.Infrastructure.Persistence.Repositories;

public class MongoRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly IMongoCollection<T> _collection;

    public MongoRepository(MongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<T>();
    }

    public async Task<T?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _collection.Find(_ => true).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _collection.Find(predicate).ToListAsync(ct);
    }

    public async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _collection.Find(predicate).FirstOrDefaultAsync(ct);
    }

    public async Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
    {
        if (predicate == null)
            return await _collection.CountDocumentsAsync(_ => true, cancellationToken: ct);
        return await _collection.CountDocumentsAsync(predicate, cancellationToken: ct);
    }

    public async Task<T> CreateAsync(T entity, CancellationToken ct = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _collection.InsertOneAsync(entity, cancellationToken: ct);
        return entity;
    }

    public async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _collection.DeleteOneAsync(x => x.Id == id, ct);
    }

    public async Task DeleteManyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        await _collection.DeleteManyAsync(predicate, ct);
    }

    public async Task<(IReadOnlyList<T> Items, long TotalCount)> GetPagedAsync(
        Expression<Func<T, bool>>? filter,
        int page,
        int pageSize,
        Expression<Func<T, object>>? sortBy = null,
        bool sortDescending = true,
        CancellationToken ct = default)
    {
        var filterDefinition = filter ?? (_ => true);
        var totalCount = await _collection.CountDocumentsAsync(filterDefinition, cancellationToken: ct);

        var query = _collection.Find(filterDefinition);

        if (sortBy != null)
        {
            query = sortDescending ? query.SortByDescending(sortBy) : query.SortBy(sortBy);
        }
        else
        {
            query = query.SortByDescending(x => x.CreatedAt);
        }

        var items = await query.Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync(ct);

        return (items, totalCount);
    }
}
