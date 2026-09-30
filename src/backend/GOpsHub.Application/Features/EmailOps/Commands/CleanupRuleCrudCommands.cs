using GOpsHub.Application.Common.CQRS;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using GOpsHub.Application.Features.EmailOps.Queries;

namespace GOpsHub.Application.Features.EmailOps.Commands;

// ============================================
// Update Cleanup Rule
// ============================================

public record UpdateCleanupRuleCommand(
    string RuleId,
    string RuleName,
    CleanupAction Action,
    List<string> WhitelistDomains,
    string? CustomQuery,
    bool UseAI = false,
    string? AIPrompt = null,
    string? SubjectRegex = null,
    string? BodyRegex = null,
    string? SenderRegex = null
) : ICommand<CleanupRule>;

public class UpdateCleanupRuleCommandHandler : ICommandHandler<UpdateCleanupRuleCommand, CleanupRule>
{
    private readonly IRepository<CleanupRule> _ruleRepo;

    public UpdateCleanupRuleCommandHandler(IRepository<CleanupRule> ruleRepo)
    {
        _ruleRepo = ruleRepo;
    }

    public async Task<CleanupRule> HandleAsync(UpdateCleanupRuleCommand command, CancellationToken ct = default)
    {
        if (command.Action != CleanupAction.Trash)
            throw new InvalidOperationException("Cleanup rules only support Trash.");
        var rule = await _ruleRepo.GetByIdAsync(command.RuleId, ct);
        if (rule == null)
            throw new KeyNotFoundException($"Cleanup rule {command.RuleId} not found.");

        rule.RuleName = command.RuleName;
        rule.Action = command.Action;
        rule.WhitelistDomains = command.WhitelistDomains ?? new List<string>();
        rule.CustomQuery = command.CustomQuery;
        rule.UseAI = command.UseAI;
        rule.AIPrompt = command.AIPrompt;
        rule.SubjectRegex = command.SubjectRegex;
        rule.BodyRegex = command.BodyRegex;
        rule.SenderRegex = command.SenderRegex;
        rule.IsActive = false;
        rule.ApprovalStatus = CleanupRuleApprovalStatus.Draft;

        await _ruleRepo.UpdateAsync(rule, ct);
        return rule;
    }
}

// ============================================
// Delete Cleanup Rule
// ============================================

public record DeleteCleanupRuleCommand(string RuleId) : ICommand<bool>;

public class DeleteCleanupRuleCommandHandler : ICommandHandler<DeleteCleanupRuleCommand, bool>
{
    private readonly IRepository<CleanupRule> _ruleRepo;

    public DeleteCleanupRuleCommandHandler(IRepository<CleanupRule> ruleRepo)
    {
        _ruleRepo = ruleRepo;
    }

    public async Task<bool> HandleAsync(DeleteCleanupRuleCommand command, CancellationToken ct = default)
    {
        var rule = await _ruleRepo.GetByIdAsync(command.RuleId, ct);
        if (rule == null)
            throw new KeyNotFoundException($"Cleanup rule {command.RuleId} not found.");

        await _ruleRepo.DeleteAsync(command.RuleId, ct);
        return true;
    }
}

// ============================================
// Toggle Cleanup Rule Active/Inactive
// ============================================

public record ToggleCleanupRuleCommand(string RuleId) : ICommand<CleanupRule>;

public class ToggleCleanupRuleCommandHandler : ICommandHandler<ToggleCleanupRuleCommand, CleanupRule>
{
    private readonly IRepository<CleanupRule> _ruleRepo;
    private readonly ApproveCleanupRuleCommandHandler _approver;

    public ToggleCleanupRuleCommandHandler(IRepository<CleanupRule> ruleRepo,
        IRepository<CleanupFeedback> feedback, IRepository<CleanupReview> reviews,
        GOpsHub.Application.Common.Interfaces.IGmailService gmail,
        GOpsHub.Application.Common.Interfaces.ICleanupRuleApprovalStore approvalStore)
    {
        _ruleRepo = ruleRepo;
        _approver = new ApproveCleanupRuleCommandHandler(ruleRepo, feedback, reviews, gmail, approvalStore);
    }

    public async Task<CleanupRule> HandleAsync(ToggleCleanupRuleCommand command, CancellationToken ct = default)
    {
        var rule = await _ruleRepo.GetByIdAsync(command.RuleId, ct);
        if (rule == null)
            throw new KeyNotFoundException($"Cleanup rule {command.RuleId} not found.");

        if (!rule.IsActive)
            return await _approver.HandleAsync(new ApproveCleanupRuleCommand(rule.Id), ct);

        rule.IsActive = false;
        await _ruleRepo.UpdateAsync(rule, ct);
        return rule;
    }
}

public record ApproveCleanupRuleCommand(string RuleId) : ICommand<CleanupRule>;

public class ApproveCleanupRuleCommandHandler : ICommandHandler<ApproveCleanupRuleCommand, CleanupRule>
{
    private readonly IRepository<CleanupRule> _rules;
    private readonly PreviewCleanupRuleQueryHandler _preview;
    private readonly GOpsHub.Application.Common.Interfaces.ICleanupRuleApprovalStore _approvalStore;

    public ApproveCleanupRuleCommandHandler(IRepository<CleanupRule> rules,
        IRepository<CleanupFeedback> feedback, IRepository<CleanupReview> reviews,
        GOpsHub.Application.Common.Interfaces.IGmailService gmail,
        GOpsHub.Application.Common.Interfaces.ICleanupRuleApprovalStore approvalStore)
    {
        _rules = rules;
        _preview = new PreviewCleanupRuleQueryHandler(rules, feedback, reviews, gmail);
        _approvalStore = approvalStore;
    }

    public async Task<CleanupRule> HandleAsync(ApproveCleanupRuleCommand command,
        CancellationToken ct = default)
    {
        var preview = await _preview.HandleAsync(new PreviewCleanupRuleQuery(command.RuleId), ct);
        if (preview.Blockers.Count > 0)
            throw new InvalidOperationException(string.Join(" ", preview.Blockers));
        var rule = await _rules.GetByIdAsync(command.RuleId, ct)
            ?? throw new KeyNotFoundException($"Cleanup rule {command.RuleId} not found.");
        if (rule.UpdatedAt != preview.RuleUpdatedAt)
            throw new InvalidOperationException("Rule changed after preview; preview it again.");
        return await _approvalStore.TryApproveAsync(rule, ct)
            ?? throw new InvalidOperationException("Rule changed concurrently; preview it again.");
    }
}
