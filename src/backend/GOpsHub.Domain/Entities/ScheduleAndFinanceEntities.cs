using MongoDB.Bson.Serialization.Attributes;
using GOpsHub.Domain.Common;
using GOpsHub.Domain.Enums;

namespace GOpsHub.Domain.Entities;

/// <summary>
/// Schedule extracted from email by AI (UC03).
/// </summary>
public class ExtractedSchedule : BaseEntity
{
    [BsonElement("sourceEmailId")]
    public string SourceEmailId { get; set; } = string.Empty;

    [BsonElement("sourceEmailSubject")]
    public string SourceEmailSubject { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("startTime")]
    public DateTime StartTime { get; set; }

    [BsonElement("endTime")]
    public DateTime? EndTime { get; set; }

    [BsonElement("location")]
    public string? Location { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("eventType")]
    public EventType EventType { get; set; } = EventType.Other;

    [BsonElement("calendarEventId")]
    public string? CalendarEventId { get; set; }

    [BsonElement("confidenceScore")]
    public double ConfidenceScore { get; set; }

    [BsonElement("status")]
    public ScheduleStatus Status { get; set; } = ScheduleStatus.PendingConfirm;
}

/// <summary>
/// Financial transaction parsed from bank/wallet email (UC04).
/// </summary>
public class Transaction : BaseEntity
{
    [BsonElement("sourceEmailId")]
    public string SourceEmailId { get; set; } = string.Empty;

    [BsonElement("transactionDate")]
    public DateTime TransactionDate { get; set; }

    [BsonElement("bankName")]
    public string BankName { get; set; } = string.Empty;

    [BsonElement("transactionType")]
    public TransactionType TransactionType { get; set; }

    [BsonElement("amount")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Decimal128)]
    public decimal Amount { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "VND";

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = "other";

    [BsonElement("balanceAfter")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Decimal128)]
    public decimal? BalanceAfter { get; set; }

    [BsonElement("sheetRowRef")]
    public string? SheetRowRef { get; set; }

    [BsonElement("isAutoRead")]
    public bool IsAutoRead { get; set; }
}
