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
    private readonly IRepository<CleanupRule> _ruleRepo;
    private readonly IRepository<CleanupLog> _logRepo;
    private readonly IRepository<EmailActionLog> _actionLogRepo;
    private readonly IGmailService _gmailService;
    private readonly IAIService _aiService;
    private readonly ILogger<RunCleanupCommandHandler> _logger;

    public RunCleanupCommandHandler(
        IRepository<CleanupRule> ruleRepo,
        IRepository<CleanupLog> logRepo,
        IRepository<EmailActionLog> actionLogRepo,
        IGmailService gmailService,
        IAIService aiService,
        ILogger<RunCleanupCommandHandler> logger)
    {
        _ruleRepo = ruleRepo;
        _logRepo = logRepo;
        _actionLogRepo = actionLogRepo;
        _gmailService = gmailService;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<CleanupLogResult> HandleAsync(RunCleanupCommand command, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var sessionId = Guid.NewGuid().ToString("N")[..8];
        var rules = string.IsNullOrEmpty(command.RuleId)
            ? await _ruleRepo.FindAsync(r => r.IsActive, ct)
            : await _ruleRepo.FindAsync(r => r.Id == command.RuleId && r.IsActive, ct);

        var result = new CleanupLogResult();

        foreach (var rule in rules)
        {
            var query = BuildGmailQuery(rule);
            var emails = await _gmailService.GetEmailsAsync(query, 100, ct);

            int trashed = 0, archived = 0, skipped = 0;

            foreach (var email in emails)
            {
                // Unified safety check (Bank protection, starred protection, read-email retention, whitelist)
                if (!EmailSafetyRules.IsSafeToClean(email, rule.WhitelistDomains))
                {
                    skipped++;
                    continue;
                }

                // ReDoS-protected regex matching
                bool hasRegex = !string.IsNullOrEmpty(rule.SubjectRegex) ||
                                !string.IsNullOrEmpty(rule.SenderRegex) ||
                                !string.IsNullOrEmpty(rule.BodyRegex);

                if (hasRegex && !EmailSafetyRules.IsEmailMatchingRegex(email, rule))
                {
                    skipped++;
                    continue;
                }

                // AI matching (protected by underlying GeminiRateLimiter)
                if (rule.UseAI && !string.IsNullOrEmpty(rule.AIPrompt))
                {
                    var isMatch = await _aiService.CheckCleanupConditionAsync(email.Snippet ?? email.Body ?? "", rule.AIPrompt, ct);
                    if (!isMatch)
                    {
                        skipped++;
                        continue;
                    }
                }

                if (rule.Action == CleanupAction.Trash)
                {
                    await _gmailService.TrashEmailAsync(email.Id, ct);
                    trashed++;
                    await _actionLogRepo.CreateAsync(new EmailActionLog
                    {
                        EmailId = email.Id,
                        Subject = email.Subject,
                        Sender = email.From,
                        Action = "Trashed",
                        SourceJob = "ManualCleanup",
                        SessionId = sessionId,
                        Reason = $"ManualRule: {rule.RuleName}"
                    }, ct);
                }
                else if (rule.Action == CleanupAction.Archive)
                {
                    await _gmailService.ArchiveEmailAsync(email.Id, ct);
                    archived++;
                    await _actionLogRepo.CreateAsync(new EmailActionLog
                    {
                        EmailId = email.Id,
                        Subject = email.Subject,
                        Sender = email.From,
                        Action = "Archived",
                        SourceJob = "ManualCleanup",
                        SessionId = sessionId,
                        Reason = $"ManualRule: {rule.RuleName}"
                    }, ct);
                }
            }

            if (trashed > 0 || archived > 0)
            {
                var log = new CleanupLog
                {
                    RuleId = rule.Id,
                    RuleName = rule.RuleName,
                    SessionId = sessionId,
                    ExecutedAt = DateTime.UtcNow,
                    TotalProcessed = emails.Count,
                    TotalTrashed = trashed,
                    TotalArchived = archived,
                    TotalSkipped = skipped,
                    DurationMs = sw.ElapsedMilliseconds,
                    Details = $"Executed rule '{rule.RuleName}' on {emails.Count} emails."
                };

                await _logRepo.CreateAsync(log, ct);
            }

            result.RulesExecuted++;
            result.TotalProcessed += emails.Count;
            result.TotalTrashed += trashed;
            result.TotalArchived += archived;
            result.TotalSkipped += skipped;
        }

        sw.Stop();
        result.TotalDurationMs = sw.ElapsedMilliseconds;
        return result;
    }

    private static string BuildGmailQuery(CleanupRule rule)
    {
        return EmailSafetyRules.BuildDefaultQuery(rule.CustomQuery);
    }
}
