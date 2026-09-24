using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Application.Features.EmailOps.Commands;

public record SubmitCleanupFeedbackCommand(
    string EmailId,
    string Sender,
    string? Subject,
    string? Snippet,
    string Reason,
    List<string>? Tags
) : ICommand<CleanupFeedback>;

public class SubmitCleanupFeedbackCommandHandler : ICommandHandler<SubmitCleanupFeedbackCommand, CleanupFeedback>
{
    private readonly IRepository<CleanupFeedback> _feedbackRepo;
    private readonly IRepository<EmailActionLog> _actionLogRepo;
    private readonly IGmailService _gmailService;
    private readonly ILogger<SubmitCleanupFeedbackCommandHandler> _logger;

    public SubmitCleanupFeedbackCommandHandler(
        IRepository<CleanupFeedback> feedbackRepo,
        IRepository<EmailActionLog> actionLogRepo,
        IGmailService gmailService,
        ILogger<SubmitCleanupFeedbackCommandHandler> logger)
    {
        _feedbackRepo = feedbackRepo;
        _actionLogRepo = actionLogRepo;
        _gmailService = gmailService;
        _logger = logger;
    }

    public async Task<CleanupFeedback> HandleAsync(SubmitCleanupFeedbackCommand command, CancellationToken ct = default)
    {
        string? domain = ExtractDomain(command.Sender);

        var feedback = new CleanupFeedback
        {
            EmailId = command.EmailId,
            Sender = command.Sender,
            SenderDomain = domain,
            Subject = command.Subject,
            Snippet = command.Snippet,
            Reason = command.Reason,
            Tags = command.Tags ?? new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        var savedFeedback = await _feedbackRepo.CreateAsync(feedback, ct);

        // Trash email in Gmail
        try
        {
            await _gmailService.TrashEmailAsync(command.EmailId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trash email ID {EmailId} when submitting cleanup feedback", command.EmailId);
            throw;
        }

        // Record audit trail
        var tagsSummary = (feedback.Tags.Count > 0) ? $"[{string.Join(", ", feedback.Tags)}] " : "";
        await _actionLogRepo.CreateAsync(new EmailActionLog
        {
            EmailId = command.EmailId,
            Subject = command.Subject,
            Sender = command.Sender,
            Action = "UserTaughtTrash",
            SourceJob = "EmailCleanup",
            Reason = $"User feedback: {tagsSummary}{command.Reason}".Trim(),
            ExecutedAt = DateTime.UtcNow
        }, ct);

        return savedFeedback;
    }

    public static string? ExtractDomain(string? sender)
    {
        if (string.IsNullOrWhiteSpace(sender)) return null;
        var atIndex = sender.LastIndexOf('@');
        if (atIndex < 0) return null;
        var domain = sender[(atIndex + 1)..].Trim().TrimEnd('>');
        return string.IsNullOrWhiteSpace(domain) ? null : domain.ToLowerInvariant();
    }
}

public record DeleteCleanupFeedbackCommand(string Id) : ICommand<bool>;

public class DeleteCleanupFeedbackCommandHandler : ICommandHandler<DeleteCleanupFeedbackCommand, bool>
{
    private readonly IRepository<CleanupFeedback> _feedbackRepo;

    public DeleteCleanupFeedbackCommandHandler(IRepository<CleanupFeedback> feedbackRepo)
    {
        _feedbackRepo = feedbackRepo;
    }

    public async Task<bool> HandleAsync(DeleteCleanupFeedbackCommand command, CancellationToken ct = default)
    {
        var existing = await _feedbackRepo.GetByIdAsync(command.Id, ct);
        if (existing == null) return false;
        await _feedbackRepo.DeleteAsync(command.Id, ct);
        return true;
    }
}
