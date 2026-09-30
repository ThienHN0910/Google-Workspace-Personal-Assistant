using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using MongoDB.Driver;

namespace GOpsHub.Infrastructure.Persistence.Repositories;

public class MongoCleanupReviewStore : ICleanupReviewStore
{
    private readonly IMongoCollection<CleanupReview> _collection;
    public MongoCleanupReviewStore(MongoDbContext context) => _collection = context.GetCollection<CleanupReview>();

    public async Task<CleanupReview?> GetByIdAsync(string reviewId, CancellationToken ct = default) =>
        await _collection.Find(x => x.Id == reviewId).FirstOrDefaultAsync(ct);

    public async Task<bool> TryClaimTrashAsync(string reviewId, CancellationToken ct = default)
    {
        var filter = Builders<CleanupReview>.Filter.Where(x => x.Id == reviewId && x.Status == CleanupReviewStatus.Pending);
        var update = Builders<CleanupReview>.Update.Set(x => x.Status, CleanupReviewStatus.ProcessingTrash)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        return await _collection.FindOneAndUpdateAsync(filter, update, cancellationToken: ct) != null;
    }

    public async Task<bool> TryClaimRecoveryAsync(string reviewId, DateTime olderThan, CancellationToken ct = default)
    {
        var filter = Builders<CleanupReview>.Filter.Where(x => x.Id == reviewId &&
            x.Status == CleanupReviewStatus.ProcessingTrash && x.UpdatedAt < olderThan);
        var update = Builders<CleanupReview>.Update.Set(x => x.UpdatedAt, DateTime.UtcNow);
        return await _collection.FindOneAndUpdateAsync(filter, update, cancellationToken: ct) != null;
    }

    public async Task<bool> TryKeepAsync(string reviewId, string reason, CancellationToken ct = default)
    {
        var filter = Builders<CleanupReview>.Filter.Where(x => x.Id == reviewId && x.Status == CleanupReviewStatus.Pending);
        var update = Builders<CleanupReview>.Update.Set(x => x.Status, CleanupReviewStatus.Kept)
            .Set(x => x.ResolvedReason, reason).Set(x => x.UpdatedAt, DateTime.UtcNow);
        return await _collection.FindOneAndUpdateAsync(filter, update, cancellationToken: ct) != null;
    }

    public async Task<CleanupReview> CompleteAsync(string reviewId, CleanupReviewStatus status, string reason, CancellationToken ct = default)
    {
        var filter = Builders<CleanupReview>.Filter.Where(x => x.Id == reviewId && x.Status == CleanupReviewStatus.ProcessingTrash);
        var update = Builders<CleanupReview>.Update.Set(x => x.Status, status)
            .Set(x => x.ResolvedReason, reason).Set(x => x.UpdatedAt, DateTime.UtcNow);
        return await _collection.FindOneAndUpdateAsync(filter, update,
            new FindOneAndUpdateOptions<CleanupReview> { ReturnDocument = ReturnDocument.After }, ct)
            ?? throw new InvalidOperationException("Review status changed concurrently.");
    }
}
