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
    string? BodyRegex = null,
    string? SenderRegex = null
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
        if (command.Action != CleanupAction.Trash)
            throw new InvalidOperationException("Cleanup rules only support Trash.");
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
            SenderRegex = command.SenderRegex,
            IsActive = false,
            ApprovalStatus = CleanupRuleApprovalStatus.Draft
        };

        return await _ruleRepo.CreateAsync(rule, ct);
    }
}

public record RunCleanupCommand(string? RuleId = null, bool RunAsync = false) : ICommand<CleanupLogResult>;

public class CleanupLogResult
{
    public int RulesExecuted { get; set; }
    public int TotalProcessed { get; set; }
    public int TotalTrashed { get; set; }
    public int TotalArchived { get; set; }
    public int TotalSkipped { get; set; }
    public long TotalDurationMs { get; set; }
    public string? Details { get; set; }
}

public class RunCleanupCommandHandler : ICommandHandler<RunCleanupCommand, CleanupLogResult>
{
    private readonly EmailCleanupBackgroundJob _cleanupJob;
    private readonly Hangfire.IRecurringJobManager? _recurringJobManager;

    public RunCleanupCommandHandler(
        EmailCleanupBackgroundJob cleanupJob,
        Hangfire.IRecurringJobManager? recurringJobManager = null)
    {
        _cleanupJob = cleanupJob;
        _recurringJobManager = recurringJobManager;
    }

    public async Task<CleanupLogResult> HandleAsync(RunCleanupCommand command, CancellationToken ct = default)
    {
        if (command.RunAsync && _recurringJobManager != null)
        {
            _recurringJobManager.Trigger("email-cleanup");
            return new CleanupLogResult
            {
                TotalProcessed = 0,
                TotalTrashed = 0,
                Details = "BackgroundJobTriggered: email-cleanup"
            };
        }

        return await _cleanupJob.RunAutoCleanupAsync(ct);
    }
}
