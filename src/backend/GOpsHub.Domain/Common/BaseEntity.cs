using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GOpsHub.Domain.Common;

/// <summary>
/// Base entity with MongoDB ObjectId and audit timestamps.
/// All domain entities inherit from this class.
/// </summary>
[BsonIgnoreExtraElements]
public abstract class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
