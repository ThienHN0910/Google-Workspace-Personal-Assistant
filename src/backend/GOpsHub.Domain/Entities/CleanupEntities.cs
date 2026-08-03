using MongoDB.Bson.Serialization.Attributes;
using GOpsHub.Domain.Common;
using GOpsHub.Domain.Enums;

namespace GOpsHub.Domain.Entities;

/// <summary>
/// Email cleanup rule — defines criteria for auto-cleaning inbox.
/// </summary>
public class CleanupRule : BaseEntity
{
    [BsonElement("ruleName")]
    public string RuleName { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty; // promotions, social, updates, forums, custom

    [BsonElement("olderThanDays")]
    public int OlderThanDays { get; set; } = 7;

    [BsonElement("action")]
    public CleanupAction Action { get; set; } = CleanupAction.Trash;

    [BsonElement("whitelistDomains")]
    public List<string> WhitelistDomains { get; set; } = new();

    [BsonElement("customQuery")]
    public string? CustomQuery { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Log entry for each cleanup execution.
/// </summary>
public class CleanupLog : BaseEntity
{
    [BsonElement("ruleId")]
    public string RuleId { get; set; } = string.Empty;

    [BsonElement("ruleName")]
    public string RuleName { get; set; } = string.Empty;

    [BsonElement("executedAt")]
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("totalProcessed")]
    public int TotalProcessed { get; set; }

    [BsonElement("totalTrashed")]
    public int TotalTrashed { get; set; }

    [BsonElement("totalArchived")]
    public int TotalArchived { get; set; }

    [BsonElement("totalSkipped")]
    public int TotalSkipped { get; set; }

    [BsonElement("durationMs")]
    public long DurationMs { get; set; }

    [BsonElement("details")]
    public string? Details { get; set; }
}
