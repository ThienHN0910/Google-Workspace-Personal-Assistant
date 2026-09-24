using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Application.Features.EmailOps.Commands;

public record CreateCleanupRuleCommand(
    string RuleName,
    CleanupAction Action,
    List<string> WhitelistDomains,
    string? CustomQuery,
    bool UseAI = false,
    string? AIPrompt = null,
    string? SubjectRegex = null,
    string? BodyRegex = null
) : ICommand<CleanupRule>;

public class CreateCleanupRuleCommandHandler : ICommandHandler<CreateCleanupRuleCommand, CleanupRule>
{
    private readonly IRepository<CleanupRule> _ruleRepo;

    public CreateCleanupRuleCommandHandler(IRepository<CleanupRule> ruleRepo)
    {
        _ruleRepo = ruleRepo;
    }

    public async Task<CleanupRule> HandleAsync(CreateCleanupRuleCommand command, CancellationToken ct = default)
    {
        var rule = new CleanupRule
        {
            RuleName = command.RuleName,
            Action = command.Action,
            WhitelistDomains = command.WhitelistDomains ?? new List<string>(),
            CustomQuery = command.CustomQuery,
            UseAI = command.UseAI,
            AIPrompt = command.AIPrompt,
            SubjectRegex = command.SubjectRegex,
            BodyRegex = command.BodyRegex,
            IsActive = true
        };

        return await _ruleRepo.CreateAsync(rule, ct);
    }
}

public record RunCleanupCommand(string? RuleId = null) : ICommand<CleanupLogResult>;

public class CleanupLogResult
{
    public int RulesExecuted { get; set; }
    public int TotalProcessed { get; set; }
    public int TotalTrashed { get; set; }
    public int TotalArchived { get; set; }
    public int TotalSkipped { get; set; }
    public long TotalDurationMs { get; set; }
}

public class RunCleanupCommandHandler : ICommandHandler<RunCleanupCommand, CleanupLogResult>
{
    private readonly EmailCleanupBackgroundJob _cleanupJob;

    public RunCleanupCommandHandler(EmailCleanupBackgroundJob cleanupJob)
    {
        _cleanupJob = cleanupJob;
    }

    public async Task<CleanupLogResult> HandleAsync(RunCleanupCommand command, CancellationToken ct = default)
    {
        return await _cleanupJob.RunAutoCleanupAsync(ct);
    }
}
