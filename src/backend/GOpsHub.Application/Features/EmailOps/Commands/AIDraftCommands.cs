using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;

namespace GOpsHub.Application.Features.EmailOps.Commands;

public record ApproveDraftCommand(string DraftId, string? CustomContent = null) : ICommand<AIDraft>;

public class ApproveDraftCommandHandler : ICommandHandler<ApproveDraftCommand, AIDraft>
{
    private readonly IRepository<AIDraft> _draftRepo;
    private readonly IGmailService _gmailService;

    public ApproveDraftCommandHandler(IRepository<AIDraft> draftRepo, IGmailService gmailService)
    {
        _draftRepo = draftRepo;
        _gmailService = gmailService;
    }

    public async Task<AIDraft> HandleAsync(ApproveDraftCommand command, CancellationToken ct = default)
    {
        var draft = await _draftRepo.GetByIdAsync(command.DraftId, ct);
        if (draft == null)
            throw new KeyNotFoundException($"AI Draft with ID {command.DraftId} not found.");

        var finalContent = command.CustomContent ?? draft.DraftContent;
        draft.EditedContent = command.CustomContent;
        draft.Status = DraftStatus.Approved;
        draft.ProcessedAt = DateTime.UtcNow;

        // Optionally send or create draft in Gmail
        var gmailDraftId = await _gmailService.CreateDraftAsync(
            draft.OriginalEmail.From,
            $"Re: {draft.OriginalEmail.Subject}",
            finalContent,
            draft.OriginalEmail.GmailMessageId,
            null,
            null,
            ct);

        draft.GmailDraftId = gmailDraftId;
        await _draftRepo.UpdateAsync(draft, ct);

        return draft;
    }
}

public record RejectDraftCommand(string DraftId, string Reason) : ICommand<AIDraft>;

public class RejectDraftCommandHandler : ICommandHandler<RejectDraftCommand, AIDraft>
{
    private readonly IRepository<AIDraft> _draftRepo;

    public RejectDraftCommandHandler(IRepository<AIDraft> draftRepo)
    {
        _draftRepo = draftRepo;
    }

    public async Task<AIDraft> HandleAsync(RejectDraftCommand command, CancellationToken ct = default)
    {
        var draft = await _draftRepo.GetByIdAsync(command.DraftId, ct);
        if (draft == null)
            throw new KeyNotFoundException($"AI Draft with ID {command.DraftId} not found.");

        draft.Status = DraftStatus.Rejected;
        draft.UserFeedback = command.Reason;
        draft.ProcessedAt = DateTime.UtcNow;

        await _draftRepo.UpdateAsync(draft, ct);
        return draft;
    }
}

public record GenerateAIDraftCommand(string GmailMessageId) : ICommand<AIDraft>;

public class GenerateAIDraftCommandHandler : ICommandHandler<GenerateAIDraftCommand, AIDraft>
{
    private readonly IRepository<AIDraft> _draftRepo;
    private readonly IGmailService _gmailService;
    private readonly IAIService _aiService;

    public GenerateAIDraftCommandHandler(
        IRepository<AIDraft> draftRepo,
        IGmailService gmailService,
        IAIService aiService)
    {
        _draftRepo = draftRepo;
        _gmailService = gmailService;
        _aiService = aiService;
    }

    public async Task<AIDraft> HandleAsync(GenerateAIDraftCommand command, CancellationToken ct = default)
    {
        var email = await _gmailService.GetEmailByIdAsync(command.GmailMessageId, ct);
        if (email == null)
            throw new KeyNotFoundException($"Gmail message {command.GmailMessageId} not found.");

        var aiResult = await _aiService.GenerateEmailReplyAsync(email.Snippet, "vi", null, ct);

        var draft = new AIDraft
        {
            OriginalEmail = new OriginalEmailInfo
            {
                GmailMessageId = email.Id,
                From = email.From,
                Subject = email.Subject,
                Snippet = email.Snippet,
                ReceivedAt = email.ReceivedAt
            },
            DraftContent = aiResult.DraftContent,
            ConfidenceScore = aiResult.ConfidenceScore,
            Status = DraftStatus.Pending
        };

        return await _draftRepo.CreateAsync(draft, ct);
    }
}
