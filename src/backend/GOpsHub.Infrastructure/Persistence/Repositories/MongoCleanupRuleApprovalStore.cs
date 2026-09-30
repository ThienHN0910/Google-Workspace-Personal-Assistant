using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using MongoDB.Driver;

namespace GOpsHub.Infrastructure.Persistence.Repositories;

public class MongoCleanupRuleApprovalStore : ICleanupRuleApprovalStore
{
    private readonly IMongoCollection<CleanupRule> _rules;
    public MongoCleanupRuleApprovalStore(MongoDbContext context) => _rules = context.GetCollection<CleanupRule>();

    public async Task<CleanupRule?> TryApproveAsync(CleanupRule validated, CancellationToken ct = default)
    {
        var filter = Builders<CleanupRule>.Filter.Where(x => x.Id == validated.Id &&
            x.UpdatedAt == validated.UpdatedAt && x.SubjectRegex == validated.SubjectRegex &&
            x.SenderRegex == validated.SenderRegex && x.BodyRegex == validated.BodyRegex &&
            x.Action == validated.Action && x.IsActive == validated.IsActive &&
            x.ApprovalStatus == validated.ApprovalStatus);
        var update = Builders<CleanupRule>.Update
            .Set(x => x.ApprovalStatus, CleanupRuleApprovalStatus.Approved)
            .Set(x => x.IsActive, true)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        return await _rules.FindOneAndUpdateAsync(filter, update,
            new FindOneAndUpdateOptions<CleanupRule> { ReturnDocument = ReturnDocument.After }, ct);
    }
}
