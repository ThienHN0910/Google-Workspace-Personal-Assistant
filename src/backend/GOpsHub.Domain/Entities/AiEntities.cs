using MongoDB.Bson.Serialization.Attributes;
using GOpsHub.Domain.Common;

namespace GOpsHub.Domain.Entities;

/// <summary>
/// Tracks monthly Gemini AI token usage and quota thresholds.
/// </summary>
public class AiTokenUsageMonthly : BaseEntity
{
    [BsonElement("yearMonth")]
    public string YearMonth { get; set; } = DateTime.UtcNow.ToString("yyyy-MM");

    [BsonElement("totalTokens")]
    public long TotalTokens { get; set; }

    [BsonElement("promptTokens")]
    public long PromptTokens { get; set; }

    [BsonElement("candidatesTokens")]
    public long CandidatesTokens { get; set; }

    [BsonElement("featureBreakdown")]
    public Dictionary<string, long> FeatureBreakdown { get; set; } = new();

    [BsonElement("callCount")]
    public int CallCount { get; set; }

    [BsonElement("monthlyQuotaLimit")]
    public long MonthlyQuotaLimit { get; set; } = 250_000;

    [BsonElement("warningThreshold")]
    public long WarningThreshold { get; set; } = 200_000;

    [BsonElement("warningSent")]
    public bool WarningSent { get; set; }

    [BsonElement("quotaExceededSent")]
    public bool QuotaExceededSent { get; set; }

    [BsonElement("lastCalledAt")]
    public DateTime LastCalledAt { get; set; } = DateTime.UtcNow;
}
