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

    [BsonElement("action")]
    public CleanupAction Action { get; set; } = CleanupAction.Trash;

    [BsonElement("whitelistDomains")]
    public List<string> WhitelistDomains { get; set; } = new();

    [BsonElement("customQuery")]
    public string? CustomQuery { get; set; }

    [BsonElement("useAI")]
    public bool UseAI { get; set; }

    [BsonElement("aiPrompt")]
    public string? AIPrompt { get; set; }

    [BsonElement("subjectRegex")]
    public string? SubjectRegex { get; set; }

    [BsonElement("senderRegex")]
    public string? SenderRegex { get; set; }

    [BsonElement("bodyRegex")]
    public string? BodyRegex { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("isAutoLearned")]
    public bool IsAutoLearned { get; set; }
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

/// <summary>
/// Detailed audit log for actions taken on emails (Read, Trash, Archive, Regex matched).
/// </summary>
public class EmailActionLog : BaseEntity
{
    [BsonElement("emailId")]
    public string EmailId { get; set; } = string.Empty;

    [BsonElement("subject")]
    public string? Subject { get; set; }

    [BsonElement("sender")]
    public string? Sender { get; set; }

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty; // "MarkedRead", "Trashed", "Archived"

    [BsonElement("sourceJob")]
    public string SourceJob { get; set; } = string.Empty; // "EmailCleanup", "BankTelemetry"

    [BsonElement("reason")]
    public string Reason { get; set; } = string.Empty;

    [BsonElement("executedAt")]
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

