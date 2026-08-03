using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.GoogleApis;

public class GmailApiService : IGmailService
{
    private readonly IRepository<AdminUser> _userRepo;
    private readonly ITokenEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GmailApiService> _logger;
    private const string AdminEmail = "hnt.vn.vn@gmail.com";

    public GmailApiService(
        IRepository<AdminUser> userRepo,
        ITokenEncryptionService encryptionService,
        IConfiguration configuration,
        ILogger<GmailApiService> logger)
    {
        _userRepo = userRepo;
        _encryptionService = encryptionService;
        _configuration = configuration;
        _logger = logger;
    }

    private async Task<GmailService?> GetGmailClientAsync(CancellationToken ct = default)
    {
        var user = await _userRepo.FindOneAsync(u => u.Email == AdminEmail, ct);
        if (user == null || string.IsNullOrEmpty(user.GoogleAccessToken))
        {
            _logger.LogWarning("Admin user token not found for Gmail API calls.");
            return null;
        }

        var accessToken = _encryptionService.Decrypt(user.GoogleAccessToken);
        var credential = GoogleCredential.FromAccessToken(accessToken);

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

    public async Task TrashEmailAsync(string messageId, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return;

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

    public async Task<string> CreateDraftAsync(string to, string subject, string body, string? threadId = null, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return string.Empty;

        var rawMessage = $"To: {to}\r\nSubject: {subject}\r\nContent-Type: text/plain; charset=utf-8\r\n\r\n{body}";
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

    public async Task<EmailMessage?> GetEmailByIdAsync(string messageId, CancellationToken ct = default)
    {
        var service = await GetGmailClientAsync(ct);
        if (service == null) return null;

        var msg = await service.Users.Messages.Get("me", messageId).ExecuteAsync(ct);
        return msg == null ? null : MapToEmailMessage(msg);
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
            Body = msg.Snippet, // Simple fallback for body snippet
            ReceivedAt = date,
            IsRead = msg.LabelIds == null || !msg.LabelIds.Contains("UNREAD"),
            Labels = msg.LabelIds?.ToList() ?? new List<string>()
        };
    }
}
