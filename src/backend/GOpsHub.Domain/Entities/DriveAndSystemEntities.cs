using MongoDB.Bson.Serialization.Attributes;
using GOpsHub.Domain.Common;
using GOpsHub.Domain.Enums;

namespace GOpsHub.Domain.Entities;

/// <summary>
/// Google Drive folder being monitored for changes (UC05).
/// </summary>
public class MonitoredFolder : BaseEntity
{
    [BsonElement("googleFolderId")]
    public string GoogleFolderId { get; set; } = string.Empty;

    [BsonElement("folderName")]
    public string FolderName { get; set; } = string.Empty;

    [BsonElement("folderPath")]
    public string? FolderPath { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("alertOnBulkDelete")]
    public bool AlertOnBulkDelete { get; set; } = true;

    [BsonElement("bulkDeleteThreshold")]
    public int BulkDeleteThreshold { get; set; } = 5;

    [BsonElement("watchChannelId")]
    public string? WatchChannelId { get; set; }

    [BsonElement("watchExpiration")]
    public DateTime? WatchExpiration { get; set; }
}

/// <summary>
/// Audit log entry for Drive file changes (UC05).
/// </summary>
public class DriveAuditLog : BaseEntity
{
    [BsonElement("monitoredFolderId")]
    public string MonitoredFolderId { get; set; } = string.Empty;

    [BsonElement("googleFileId")]
    public string GoogleFileId { get; set; } = string.Empty;

    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;

    [BsonElement("fileType")]
    public string? FileType { get; set; }

    [BsonElement("action")]
    public DriveAction Action { get; set; }

    [BsonElement("actorEmail")]
    public string? ActorEmail { get; set; }

    [BsonElement("actorName")]
    public string? ActorName { get; set; }

    [BsonElement("actionTimestamp")]
    public DateTime ActionTimestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("details")]
    public string? Details { get; set; }
}

/// <summary>
/// Security alert for suspicious activity on Drive (UC06).
/// </summary>
public class SecurityAlert : BaseEntity
{
    [BsonElement("severity")]
    public AlertSeverity Severity { get; set; }

    [BsonElement("alertType")]
    public AlertType AlertType { get; set; }

    [BsonElement("fileId")]
    public string? FileId { get; set; }

    [BsonElement("fileName")]
    public string? FileName { get; set; }

    [BsonElement("filePath")]
    public string? FilePath { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("isResolved")]
    public bool IsResolved { get; set; }

    [BsonElement("resolvedAt")]
    public DateTime? ResolvedAt { get; set; }

    [BsonElement("resolutionNote")]
    public string? ResolutionNote { get; set; }
}

/// <summary>
/// Dashboard notification entry.
/// </summary>
public class Notification : BaseEntity
{
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("severity")]
    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty; // email, calendar, finance, drive, system

    [BsonElement("actionUrl")]
    public string? ActionUrl { get; set; }

    [BsonElement("isRead")]
    public bool IsRead { get; set; }

    [BsonElement("readAt")]
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// Backup execution record (UC10).
/// </summary>
public class BackupRecord : BaseEntity
{
    [BsonElement("backupType")]
    public BackupType BackupType { get; set; }

    [BsonElement("googleFileId")]
    public string? GoogleFileId { get; set; }

    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;

    [BsonElement("fileSizeBytes")]
    public long FileSizeBytes { get; set; }

    [BsonElement("driveFolder")]
    public string? DriveFolder { get; set; }

    [BsonElement("status")]
    public BackupStatus Status { get; set; } = BackupStatus.InProgress;

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }
}
