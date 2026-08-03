using MongoDB.Bson.Serialization.Attributes;
using GOpsHub.Domain.Common;

namespace GOpsHub.Domain.Entities;

/// <summary>
/// Admin user entity — singleton (only one admin in the system).
/// Stores Google OAuth tokens and personal settings.
/// </summary>
public class AdminUser : BaseEntity
{
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [BsonElement("googleId")]
    public string GoogleId { get; set; } = string.Empty;

    [BsonElement("googleAccessToken")]
    public string? GoogleAccessToken { get; set; }

    [BsonElement("googleRefreshToken")]
    public string? GoogleRefreshToken { get; set; }

    [BsonElement("googleTokenExpiresAt")]
    public DateTime? GoogleTokenExpiresAt { get; set; }

    [BsonElement("settings")]
    public UserSettings Settings { get; set; } = new();

    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Embedded document within AdminUser for user preferences.
/// </summary>
public class UserSettings
{
    [BsonElement("cleanupEnabled")]
    public bool CleanupEnabled { get; set; } = true;

    [BsonElement("cleanupSchedule")]
    public string CleanupSchedule { get; set; } = "0 0 * * *"; // Daily at midnight

    [BsonElement("aiDraftEnabled")]
    public bool AIDraftEnabled { get; set; } = true;

    [BsonElement("aiDraftLanguage")]
    public string AIDraftLanguage { get; set; } = "vi";

    [BsonElement("scheduleExtractionEnabled")]
    public bool ScheduleExtractionEnabled { get; set; } = true;

    [BsonElement("transactionLoggingEnabled")]
    public bool TransactionLoggingEnabled { get; set; } = true;

    [BsonElement("transactionSmallThreshold")]
    public decimal TransactionSmallThreshold { get; set; } = 500_000; // VND

    [BsonElement("timezone")]
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";

    [BsonElement("language")]
    public string Language { get; set; } = "vi";

    [BsonElement("notificationChannels")]
    public NotificationChannels NotificationChannels { get; set; } = new();
}

/// <summary>
/// Notification channel configuration.
/// </summary>
public class NotificationChannels
{
    [BsonElement("dashboard")]
    public bool Dashboard { get; set; } = true;

    [BsonElement("email")]
    public bool Email { get; set; } = false;

    [BsonElement("discordWebhookUrl")]
    public string? DiscordWebhookUrl { get; set; }
}
