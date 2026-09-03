namespace GOpsHub.Application.Common.Interfaces;

/// <summary>
/// Google Gmail API service abstraction.
/// </summary>
public interface IGmailService
{
    Task<IReadOnlyList<EmailMessage>> GetEmailsAsync(string query, int maxResults = 100, CancellationToken ct = default);
    Task TrashEmailAsync(string messageId, CancellationToken ct = default);
    Task ArchiveEmailAsync(string messageId, CancellationToken ct = default);
    Task MarkAsReadAsync(string messageId, CancellationToken ct = default);
    Task StarEmailAsync(string messageId, CancellationToken ct = default);
    Task UnstarEmailAsync(string messageId, CancellationToken ct = default);
    Task<string> CreateDraftAsync(string to, string subject, string body, string? threadId = null, string? cc = null, string? bcc = null, CancellationToken ct = default);
    Task SendDraftAsync(string draftId, CancellationToken ct = default);
    Task DeleteDraftAsync(string draftId, CancellationToken ct = default);
    Task<EmailMessage?> GetEmailByIdAsync(string messageId, CancellationToken ct = default);
    
    // Pagination & Unread
    Task<(IReadOnlyList<EmailMessage> Emails, string? NextPageToken)> GetPagedEmailsAsync(string query, int maxResults = 10, string? pageToken = null, CancellationToken ct = default);
    Task MarkAsUnreadAsync(string messageId, CancellationToken ct = default);
    Task<int> GetUnreadEmailCountAsync(CancellationToken ct = default);
}

/// <summary>
/// Simplified email message model.
/// </summary>
public class EmailMessage
{
    public string Id { get; set; } = string.Empty;
    public string ThreadId { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public string? Body { get; set; }
    public DateTime ReceivedAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsStarred => Labels.Contains("STARRED");
    public List<string> Labels { get; set; } = new();
    public List<EmailAttachment> Attachments { get; set; } = new();
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string AttachmentId { get; set; } = string.Empty;
}
