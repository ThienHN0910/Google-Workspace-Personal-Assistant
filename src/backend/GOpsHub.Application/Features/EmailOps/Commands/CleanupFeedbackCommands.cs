using GOpsHub.Application.Common.CQRS;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;

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
    private readonly IRepository<CleanupReview> _reviews;
    private readonly ICommandHandler<ResolveCleanupReviewCommand, CleanupReview> _resolver;

    public SubmitCleanupFeedbackCommandHandler(
        IRepository<CleanupFeedback> feedbackRepo,
        IRepository<CleanupReview> reviews,
        ICommandHandler<ResolveCleanupReviewCommand, CleanupReview> resolver)
    {
        _feedbackRepo = feedbackRepo;
        _reviews = reviews;
        _resolver = resolver;
    }

    public async Task<CleanupFeedback> HandleAsync(SubmitCleanupFeedbackCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new ArgumentException("A written deletion reason is required.", nameof(command));

        if (string.IsNullOrWhiteSpace(command.EmailId))
            throw new ArgumentException("Email ID is required.", nameof(command));

        var review = await _reviews.FindOneAsync(x => x.EmailId == command.EmailId, ct);
        if (review == null)
        {
            try
            {
                review = await _reviews.CreateAsync(new CleanupReview
                {
                    EmailId = command.EmailId,
                    Sender = command.Sender,
                    Subject = command.Subject,
                    Snippet = command.Snippet is { Length: > 300 } ? command.Snippet[..300] : command.Snippet,
                    AiReason = "Manual Trash with reason",
                    Status = CleanupReviewStatus.Pending
                }, ct);
            }
            catch
            {
                review = await _reviews.FindOneAsync(x => x.EmailId == command.EmailId, ct);
                if (review == null) throw;
            }
        }

        if (review.Status == CleanupReviewStatus.Kept)
            throw new InvalidOperationException("This email was kept; resolve the existing preference first.");
        if (review.Status != CleanupReviewStatus.Trashed)
            await _resolver.HandleAsync(new ResolveCleanupReviewCommand(review.Id,
                CleanupDecision.Trash, command.Reason.Trim(), command.Tags), ct);

        return await _feedbackRepo.FindOneAsync(x => x.ReviewId == review.Id &&
            x.Decision == CleanupDecision.Trash, ct)
            ?? throw new InvalidOperationException("Trash completed but feedback is unavailable; retry later.");
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
