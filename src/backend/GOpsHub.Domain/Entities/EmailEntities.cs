using MongoDB.Bson.Serialization.Attributes;
using GOpsHub.Domain.Common;
using GOpsHub.Domain.Enums;

namespace GOpsHub.Domain.Entities;

/// <summary>
/// AI-generated email draft awaiting human review (UC02).
/// </summary>
public class AIDraft : BaseEntity
{
    [BsonElement("originalEmail")]
    public OriginalEmailInfo OriginalEmail { get; set; } = new();

    [BsonElement("gmailDraftId")]
    public string? GmailDraftId { get; set; }

    [BsonElement("draftContent")]
    public string DraftContent { get; set; } = string.Empty;

    [BsonElement("confidenceScore")]
    public double ConfidenceScore { get; set; }

    [BsonElement("status")]
    public DraftStatus Status { get; set; } = DraftStatus.Pending;

    [BsonElement("userFeedback")]
    public string? UserFeedback { get; set; }

    [BsonElement("editedContent")]
    public string? EditedContent { get; set; }

    [BsonElement("processedAt")]
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// Embedded info about the original email that triggered the AI draft.
/// </summary>
public class OriginalEmailInfo
{
    [BsonElement("gmailMessageId")]
    public string GmailMessageId { get; set; } = string.Empty;

    [BsonElement("from")]
    public string From { get; set; } = string.Empty;

    [BsonElement("subject")]
    public string Subject { get; set; } = string.Empty;

    [BsonElement("snippet")]
    public string Snippet { get; set; } = string.Empty;

    [BsonElement("receivedAt")]
    public DateTime ReceivedAt { get; set; }
}

/// <summary>
/// Reusable email reply template for AI reference.
/// </summary>
public class ReplyTemplate : BaseEntity
{
    [BsonElement("templateName")]
    public string TemplateName { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty; // academic, work, personal, formal

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("language")]
    public string Language { get; set; } = "vi";

    [BsonElement("usageCount")]
    public int UsageCount { get; set; }
}
