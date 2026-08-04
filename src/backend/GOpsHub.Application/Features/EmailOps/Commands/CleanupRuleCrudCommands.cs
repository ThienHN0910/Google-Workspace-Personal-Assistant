using GOpsHub.Application.Common.CQRS;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;

namespace GOpsHub.Application.Features.EmailOps.Commands;

// ============================================
// Update Cleanup Rule
// ============================================

public record UpdateCleanupRuleCommand(
    string RuleId,
    string RuleName,
    string Category,
    int OlderThanDays,
    CleanupAction Action,
    List<string> WhitelistDomains,
    string? CustomQuery
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
        var rule = await _ruleRepo.GetByIdAsync(command.RuleId, ct);
        if (rule == null)
            throw new KeyNotFoundException($"Cleanup rule {command.RuleId} not found.");

        rule.RuleName = command.RuleName;
        rule.Category = command.Category;
        rule.OlderThanDays = command.OlderThanDays;
        rule.Action = command.Action;
        rule.WhitelistDomains = command.WhitelistDomains ?? new List<string>();
        rule.CustomQuery = command.CustomQuery;

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

    public ToggleCleanupRuleCommandHandler(IRepository<CleanupRule> ruleRepo)
    {
        _ruleRepo = ruleRepo;
    }

    public async Task<CleanupRule> HandleAsync(ToggleCleanupRuleCommand command, CancellationToken ct = default)
    {
        var rule = await _ruleRepo.GetByIdAsync(command.RuleId, ct);
        if (rule == null)
            throw new KeyNotFoundException($"Cleanup rule {command.RuleId} not found.");

        rule.IsActive = !rule.IsActive;
        await _ruleRepo.UpdateAsync(rule, ct);
        return rule;
    }
}
