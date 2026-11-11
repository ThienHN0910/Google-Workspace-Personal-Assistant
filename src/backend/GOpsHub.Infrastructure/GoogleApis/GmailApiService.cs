using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using System.Net;
using Google;
using GOpsHub.Application.Common;
using GOpsHub.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.GoogleApis;

public class GmailApiService : IGmailService
{
    private readonly IAdminGoogleCredentialProvider _credentialProvider;
    private readonly ILogger<GmailApiService> _logger;

    public GmailApiService(
        IAdminGoogleCredentialProvider credentialProvider,
        ILogger<GmailApiService> logger)
    {
        _credentialProvider = credentialProvider;
        _logger = logger;
    }

    private async Task<GmailService?> GetGmailClientAsync(CancellationToken ct = default)
    {
        var credential = await _credentialProvider.GetCredentialAsync("Gmail", ct);
        if (credential == null) return null;

        return new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "G-Ops Hub"
        });
    }

    public async Task<IReadOnlyList<EmailMessage>> GetEmailsAsync(string query, int maxResults = 100, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return Array.Empty<EmailMessage>();

        try
        {
            var request = service.Users.Messages.List("me");
            request.Q = query;
            request.MaxResults = maxResults;

            var response = await request.ExecuteAsync(ct);
            if (response.Messages == null || !response.Messages.Any())
                return Array.Empty<EmailMessage>();

            var emailMessages = new List<EmailMessage>();
            foreach (var msgSummary in response.Messages)
            {
                var msgRequest = service.Users.Messages.Get("me", msgSummary.Id);
                msgRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                var msg = await msgRequest.ExecuteAsync(ct);

                if (msg != null)
                {
                    emailMessages.Add(MapToEmailMessage(msg));
                }
            }

            return emailMessages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get emails with query {Query}", query);
            return Array.Empty<EmailMessage>();
        }
    }

    public async Task<int> GetUnreadEmailCountAsync(CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return 0;

        try
        {
            var request = service.Users.Messages.List("me");
            request.Q = "is:unread";
            request.MaxResults = 1; // Only need the resultSizeEstimate
            
            var response = await request.ExecuteAsync(ct);
            return (int)(response.ResultSizeEstimate ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unread email count.");
            return 0;
        }
    }

    public async Task<(IReadOnlyList<EmailMessage> Emails, string? NextPageToken)> GetPagedEmailsAsync(string query, int maxResults = 10, string? pageToken = null, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return (Array.Empty<EmailMessage>(), null);

        try
        {
            var request = service.Users.Messages.List("me");
            request.Q = query;
            request.MaxResults = maxResults;
            request.PageToken = pageToken;

            var response = await request.ExecuteAsync(ct);
            if (response.Messages == null || !response.Messages.Any())
                return (Array.Empty<EmailMessage>(), response.NextPageToken);

            var emailMessages = new List<EmailMessage>();
            foreach (var msgSummary in response.Messages)
            {
                var msgRequest = service.Users.Messages.Get("me", msgSummary.Id);
                msgRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                var msg = await msgRequest.ExecuteAsync(ct);

                if (msg != null)
                {
                    emailMessages.Add(MapToEmailMessage(msg));
                }
            }

            return (emailMessages, response.NextPageToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get paged emails from Gmail.");
            return (Array.Empty<EmailMessage>(), null);
        }
    }

    public async Task TrashEmailAsync(string messageId, CancellationToken ct = default)
    {
        // Throw instead of silently returning: callers record the email as trashed afterwards.
        var service = await GetGmailClientAsync(ct) ?? throw new GoogleAccountNotConnectedException("Gmail");

        await service.Users.Messages.Trash("me", messageId).ExecuteAsync(ct);
    }

    public async Task ArchiveEmailAsync(string messageId, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return;

        var mods = new ModifyMessageRequest
        {
            RemoveLabelIds = new List<string> { "INBOX" }
        };
        await service.Users.Messages.Modify(mods, "me", messageId).ExecuteAsync(ct);
    }

    public async Task MarkAsReadAsync(string messageId, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return;

        var mods = new ModifyMessageRequest
        {
            RemoveLabelIds = new List<string> { "UNREAD" }
        };
        await service.Users.Messages.Modify(mods, "me", messageId).ExecuteAsync(ct);
    }

    public async Task MarkAsUnreadAsync(string messageId, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return;

        try
        {
            var request = service.Users.Messages.Modify(new ModifyMessageRequest
            {
                AddLabelIds = new List<string> { "UNREAD" }
            }, "me", messageId);
            
            await request.ExecuteAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark email {Id} as unread.", messageId);
        }
    }

    public async Task StarEmailAsync(string messageId, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return;

        var mods = new ModifyMessageRequest
        {
            AddLabelIds = new List<string> { "STARRED" }
        };
        await service.Users.Messages.Modify(mods, "me", messageId).ExecuteAsync(ct);
    }

    public async Task UnstarEmailAsync(string messageId, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return;

        var mods = new ModifyMessageRequest
        {
            RemoveLabelIds = new List<string> { "STARRED" }
        };
        await service.Users.Messages.Modify(mods, "me", messageId).ExecuteAsync(ct);
    }

    public async Task<string> CreateDraftAsync(string to, string subject, string body, string? threadId = null, string? cc = null, string? bcc = null, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"To: {to}");
        if (!string.IsNullOrWhiteSpace(cc)) sb.AppendLine($"Cc: {cc}");
        if (!string.IsNullOrWhiteSpace(bcc)) sb.AppendLine($"Bcc: {bcc}");
        sb.AppendLine($"Subject: {subject}");
        sb.AppendLine("Content-Type: text/html; charset=utf-8");
        sb.AppendLine();
        sb.Append(body);

        var rawMessage = sb.ToString();
        var encodedMessage = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(rawMessage))
            .Replace('+', '-').Replace('/', '_').Replace("=", "");

        var draft = new Draft
        {
            Message = new Message
            {
                Raw = encodedMessage,
                ThreadId = threadId
            }
        };

        var createdDraft = await service.Users.Drafts.Create(draft, "me").ExecuteAsync(ct);
        return createdDraft.Id;
    }

    public async Task SendDraftAsync(string draftId, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return;

        var draft = new Draft { Id = draftId };
        await service.Users.Drafts.Send(draft, "me").ExecuteAsync(ct);
    }

    public async Task DeleteDraftAsync(string draftId, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return;

        await service.Users.Drafts.Delete("me", draftId).ExecuteAsync(ct);
    }

    /// <summary>Returns null only when the message no longer exists in Gmail.</summary>
    public async Task<EmailMessage?> GetEmailByIdAsync(string messageId, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct) ?? throw new GoogleAccountNotConnectedException("Gmail");

        try
        {
            var msg = await service.Users.Messages.Get("me", messageId).ExecuteAsync(ct);
            return msg == null ? null : MapToEmailMessage(msg);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private static EmailMessage MapToEmailMessage(Message msg)
    {
        var headers = msg.Payload?.Headers;
        var subject = headers?.FirstOrDefault(h => h.Name.Equals("Subject", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        var from = headers?.FirstOrDefault(h => h.Name.Equals("From", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        var to = headers?.FirstOrDefault(h => h.Name.Equals("To", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        var dateStr = headers?.FirstOrDefault(h => h.Name.Equals("Date", StringComparison.OrdinalIgnoreCase))?.Value;

        var date = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var parsedDate))
        {
            date = parsedDate.ToUniversalTime();
        }

        return new EmailMessage
        {
            Id = msg.Id,
            ThreadId = msg.ThreadId,
            From = from,
            To = to,
            Subject = subject,
            Snippet = msg.Snippet ?? string.Empty,
            Body = ExtractEmailBody(msg.Payload),
            ReceivedAt = date,
            IsRead = msg.LabelIds == null || !msg.LabelIds.Contains("UNREAD"),
            Labels = msg.LabelIds?.ToList() ?? new List<string>()
        };
    }

    private static string ExtractEmailBody(MessagePart payload)
    {
        if (payload == null) return string.Empty;

        if (payload.Parts == null || !payload.Parts.Any())
        {
            return DecodeBase64Url(payload.Body?.Data);
        }

        var htmlPart = FindPart(payload.Parts, "text/html");
        if (htmlPart != null)
        {
            return DecodeBase64Url(htmlPart.Body?.Data);
        }

        var textPart = FindPart(payload.Parts, "text/plain");
        if (textPart != null)
        {
            return DecodeBase64Url(textPart.Body?.Data);
        }

        return string.Empty;
    }

    private static MessagePart? FindPart(IList<MessagePart> parts, string mimeType)
    {
        if (parts == null) return null;
        foreach (var part in parts)
        {
            if (part.MimeType == mimeType && !string.IsNullOrEmpty(part.Body?.Data))
                return part;

            if (part.Parts != null && part.Parts.Any())
            {
                var found = FindPart(part.Parts, mimeType);
                if (found != null)
                    return found;
            }
        }
        return null;
    }

    private static string DecodeBase64Url(string? base64Url)
    {
        if (string.IsNullOrEmpty(base64Url)) return string.Empty;
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        try
        {
            var bytes = Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}
