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
    Task<string> CreateDraftAsync(string to, string subject, string body, string? threadId = null, CancellationToken ct = default);
    Task SendDraftAsync(string draftId, CancellationToken ct = default);
    Task DeleteDraftAsync(string draftId, CancellationToken ct = default);
    Task<EmailMessage?> GetEmailByIdAsync(string messageId, CancellationToken ct = default);
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
    public string Subject { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public string? Body { get; set; }
    public DateTime ReceivedAt { get; set; }
    public bool IsRead { get; set; }
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
