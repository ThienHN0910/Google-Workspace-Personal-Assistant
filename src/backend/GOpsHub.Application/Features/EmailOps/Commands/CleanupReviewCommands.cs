using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Application.Features.EmailOps.Commands;

public record ResolveCleanupReviewCommand(string ReviewId, CleanupDecision Decision, string Reason) : ICommand<CleanupReview>;

public class ResolveCleanupReviewCommandHandler : ICommandHandler<ResolveCleanupReviewCommand, CleanupReview>
{
    private readonly ICleanupReviewStore _reviews;
    private readonly IRepository<CleanupFeedback> _feedback;
    private readonly IRepository<EmailActionLog> _logs;
    private readonly IGmailService _gmail;
    private readonly ILogger<ResolveCleanupReviewCommandHandler> _logger;

    public ResolveCleanupReviewCommandHandler(ICleanupReviewStore reviews, IRepository<CleanupFeedback> feedback,
        IRepository<EmailActionLog> logs, IGmailService gmail, ILogger<ResolveCleanupReviewCommandHandler> logger)
    {
        _reviews = reviews;
        _feedback = feedback;
        _logs = logs;
        _gmail = gmail;
        _logger = logger;
    }

    public async Task<CleanupReview> HandleAsync(ResolveCleanupReviewCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new ArgumentException("A written reason is required.", nameof(command));
        if (!Enum.IsDefined(command.Decision))
            throw new ArgumentOutOfRangeException(nameof(command));

        var review = await _reviews.GetByIdAsync(command.ReviewId, ct)
            ?? throw new KeyNotFoundException($"Cleanup review {command.ReviewId} was not found.");
        var reason = command.Reason.Trim();

        if (command.Decision == CleanupDecision.Keep)
        {
            if (review.Status == CleanupReviewStatus.Pending)
            {
                if (!await _reviews.TryKeepAsync(review.Id, reason, ct))
                    throw new InvalidOperationException("Review was resolved concurrently.");
            }
            else if (review.Status != CleanupReviewStatus.Kept)
                throw new InvalidOperationException("Review has a different decision.");

            await EnsureFeedbackAsync(review, CleanupDecision.Keep, reason, ct);
            await EnsureLogAsync(review, "Kept", reason, ct);
            return await _reviews.GetByIdAsync(review.Id, ct) ?? review;
        }

        if (review.Status == CleanupReviewStatus.Trashed)
            return review;

        if (review.Status == CleanupReviewStatus.Pending)
        {
            if (!await _reviews.TryClaimTrashAsync(review.Id, ct))
                throw new InvalidOperationException("Review was claimed concurrently.");

            try
            {
                await _gmail.TrashEmailAsync(review.EmailId, ct);
            }
            catch (Exception ex)
            {
                var gmailMessage = await _gmail.GetEmailByIdAsync(review.EmailId, ct);
                if (gmailMessage?.Labels.Contains("TRASH") != true)
                {
                    if (gmailMessage != null)
                        await _reviews.CompleteAsync(review.Id, CleanupReviewStatus.Pending, reason, ct);
                    _logger.LogWarning(ex, "Could not confirm Trash for cleanup review {ReviewId}", review.Id);
                    throw;
                }
            }
        }
        else if (review.Status == CleanupReviewStatus.ProcessingTrash)
        {
            if (review.UpdatedAt > DateTime.UtcNow.AddMinutes(-1))
                throw new InvalidOperationException("Trash operation is still in progress.");

            var gmailMessage = await _gmail.GetEmailByIdAsync(review.EmailId, ct);
            if (gmailMessage == null)
                throw new InvalidOperationException("Cannot verify Gmail status; review requires reconciliation.");
            if (!gmailMessage.Labels.Contains("TRASH"))
                await _gmail.TrashEmailAsync(review.EmailId, ct);
        }
        else
            throw new InvalidOperationException("Review has a different decision.");

        await EnsureFeedbackAsync(review, CleanupDecision.Trash, reason, ct);
        await EnsureLogAsync(review, "Trashed", reason, ct);
        return await _reviews.CompleteAsync(review.Id, CleanupReviewStatus.Trashed, reason, ct);
    }

    private async Task EnsureFeedbackAsync(CleanupReview review, CleanupDecision decision, string reason, CancellationToken ct)
    {
        if (await _feedback.FindOneAsync(x => x.ReviewId == review.Id, ct) != null) return;
        var senderDomain = SubmitCleanupFeedbackCommandHandler.ExtractDomain(review.Sender);
        await _feedback.CreateAsync(new CleanupFeedback
        {
            EmailId = review.EmailId,
            ReviewId = review.Id,
            Sender = review.Sender,
            SenderDomain = senderDomain,
            Subject = review.Subject,
            Snippet = review.Snippet,
            Decision = decision,
            Reason = reason
        }, ct);
    }

    private async Task EnsureLogAsync(CleanupReview review, string action, string reason, CancellationToken ct)
    {
        if (await _logs.FindOneAsync(x => x.EmailId == review.EmailId && x.SourceJob == "CleanupReview" && x.Action == action, ct) != null) return;
        await _logs.CreateAsync(new EmailActionLog
        {
            EmailId = review.EmailId,
            Subject = review.Subject,
            Sender = review.Sender,
            Action = action,
            SourceJob = "CleanupReview",
            Reason = reason
        }, ct);
    }
}
