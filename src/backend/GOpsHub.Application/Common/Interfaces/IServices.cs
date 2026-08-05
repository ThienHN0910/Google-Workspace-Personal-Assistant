namespace GOpsHub.Application.Common.Interfaces;

/// <summary>
/// AI service abstraction for Gemini integration.
/// </summary>
public interface IAIService
{
    Task<AIReplyResult> GenerateEmailReplyAsync(string emailContent, string language = "vi", string? templateHint = null, CancellationToken ct = default);
    Task<AIScheduleResult?> ExtractScheduleFromEmailAsync(string emailContent, CancellationToken ct = default);
    Task<AITransactionResult?> ParseTransactionEmailAsync(string emailContent, string bankName, CancellationToken ct = default);
    Task<string> SummarizeEmailThreadAsync(string threadContent, CancellationToken ct = default);
    Task<int> ScoreEmailPriorityAsync(string from, string subject, string snippet, CancellationToken ct = default);
    Task<List<string>> ExtractTasksFromEmailAsync(string emailContent, CancellationToken ct = default);
    Task<string> GenerateExecutiveReportAsync(string periodStats, CancellationToken ct = default);
    Task<bool> CheckCleanupConditionAsync(string emailContent, string prompt, CancellationToken ct = default);
    Task<List<AIBatchTransactionResult>> ParseBatchTransactionEmailsAsync(string batchContent, string bankName, CancellationToken ct = default);
}

public class AIReplyResult
{
    public string DraftContent { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string DetectedLanguage { get; set; } = "vi";
}

public class AIScheduleResult
{
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string EventType { get; set; } = "other";
    public double ConfidenceScore { get; set; }
}

public class AITransactionResult
{
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty; // credit / debit
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "other";
    public decimal? BalanceAfter { get; set; }
}

public class AIBatchTransactionResult : AITransactionResult
{
    public string EmailId { get; set; } = string.Empty;
}

/// <summary>
/// Google Calendar API service abstraction.
/// </summary>
public interface ICalendarService
{
    Task<string> CreateEventAsync(string title, DateTime start, DateTime? end, string? location, string? description, CancellationToken ct = default);
    Task DeleteEventAsync(string eventId, CancellationToken ct = default);
    Task<IReadOnlyList<CalendarEvent>> GetUpcomingEventsAsync(int days = 7, CancellationToken ct = default);
    Task<IReadOnlyList<CalendarBusySlot>> GetBusySlotsAsync(DateTime start, DateTime end, CancellationToken ct = default);
}

public class CalendarEvent
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public string? Location { get; set; }
    public string HtmlLink { get; set; } = string.Empty;
    public string Visibility { get; set; } = "default";
}

public class CalendarBusySlot
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

/// <summary>
/// Google Drive API service abstraction.
/// </summary>
public interface IDriveService
{
    Task<IReadOnlyList<DriveFileInfo>> ListFilesInFolderAsync(string folderId, CancellationToken ct = default);
    Task<DriveFileInfo?> GetFileInfoAsync(string fileId, CancellationToken ct = default);
    Task<string> UploadFileAsync(string folderId, string fileName, Stream content, string mimeType, CancellationToken ct = default);
    Task MoveFileAsync(string fileId, string targetFolderId, CancellationToken ct = default);
    Task<string> CreateFolderAsync(string parentFolderId, string folderName, CancellationToken ct = default);
    Task<IReadOnlyList<DrivePermission>> GetFilePermissionsAsync(string fileId, CancellationToken ct = default);
    Task RevokePermissionAsync(string fileId, string permissionId, CancellationToken ct = default);
    Task<string?> SetupWatchAsync(string folderId, string webhookUrl, CancellationToken ct = default);
}

public class DriveFileInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long? Size { get; set; }
    public DateTime? ModifiedTime { get; set; }
    public string? LastModifyingUser { get; set; }
}

public class DrivePermission
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // user, group, domain, anyone
    public string Role { get; set; } = string.Empty; // owner, organizer, writer, reader
    public string? EmailAddress { get; set; }
}

/// <summary>
/// Google Sheets API service abstraction.
/// </summary>
public interface ISheetsService
{
    Task AppendRowAsync(string spreadsheetId, string sheetName, IList<object> values, CancellationToken ct = default);
    Task<IList<IList<object>>> GetRangeAsync(string spreadsheetId, string range, CancellationToken ct = default);
    Task<string> CreateSheetTabAsync(string spreadsheetId, string sheetName, CancellationToken ct = default);
}

/// <summary>
/// Notification service abstraction for real-time alerts.
/// </summary>
public interface INotificationService
{
    Task SendNotificationAsync(string title, string message, string type = "info", CancellationToken ct = default);
}
