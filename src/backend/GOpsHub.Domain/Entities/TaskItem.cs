using MongoDB.Bson.Serialization.Attributes;
using GOpsHub.Domain.Common;

namespace GOpsHub.Domain.Entities;

/// <summary>
/// Cached Google Task item in the local database.
/// </summary>
public class TaskItem : BaseEntity
{
    [BsonElement("googleTaskId")]
    public string GoogleTaskId { get; set; } = string.Empty;

    [BsonElement("googleTaskListId")]
    public string GoogleTaskListId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("due")]
    public DateTime? Due { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "needsAction"; // needsAction or completed

    [BsonElement("parentTaskId")]
    public string? ParentTaskId { get; set; }

    [BsonElement("isStarred")]
    public bool IsStarred { get; set; }

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("sourceEmailId")]
    public string? SourceEmailId { get; set; }
}
