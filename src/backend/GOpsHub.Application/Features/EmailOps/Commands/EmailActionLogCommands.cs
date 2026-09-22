using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Application.Features.EmailOps.Commands;

public record DeleteEmailActionLogCommand(string Id) : ICommand<bool>;

public class DeleteEmailActionLogCommandHandler : ICommandHandler<DeleteEmailActionLogCommand, bool>
{
    private readonly IRepository<EmailActionLog> _actionLogRepo;

    public DeleteEmailActionLogCommandHandler(IRepository<EmailActionLog> actionLogRepo)
    {
        _actionLogRepo = actionLogRepo;
    }

    public async Task<bool> HandleAsync(DeleteEmailActionLogCommand command, CancellationToken ct = default)
    {
        await _actionLogRepo.DeleteAsync(command.Id, ct);
        return true;
    }
}

public record DeleteBatchEmailActionLogsCommand(
    List<string>? Ids = null,
    bool DeleteAll = false,
    int? OlderThanDays = null,
    string? Action = null
) : ICommand<bool>;

public class DeleteBatchEmailActionLogsCommandHandler : ICommandHandler<DeleteBatchEmailActionLogsCommand, bool>
{
    private readonly IRepository<EmailActionLog> _actionLogRepo;

    public DeleteBatchEmailActionLogsCommandHandler(IRepository<EmailActionLog> actionLogRepo)
    {
        _actionLogRepo = actionLogRepo;
    }

    public async Task<bool> HandleAsync(DeleteBatchEmailActionLogsCommand command, CancellationToken ct = default)
    {
        if (command.DeleteAll)
        {
            if (!string.IsNullOrWhiteSpace(command.Action))
            {
                await _actionLogRepo.DeleteManyAsync(x => x.Action == command.Action, ct);
            }
            else
            {
                await _actionLogRepo.DeleteManyAsync(_ => true, ct);
            }
            return true;
        }

        if (command.OlderThanDays.HasValue && command.OlderThanDays > 0)
        {
            var cutoff = DateTime.UtcNow.AddDays(-command.OlderThanDays.Value);
            if (!string.IsNullOrWhiteSpace(command.Action))
            {
                await _actionLogRepo.DeleteManyAsync(x => x.ExecutedAt < cutoff && x.Action == command.Action, ct);
            }
            else
            {
                await _actionLogRepo.DeleteManyAsync(x => x.ExecutedAt < cutoff, ct);
            }
            return true;
        }

        if (command.Ids != null && command.Ids.Any())
        {
            var ids = command.Ids.ToHashSet();
            await _actionLogRepo.DeleteManyAsync(x => ids.Contains(x.Id), ct);
            return true;
        }

        return false;
    }
}

public record ApproveEmailActionCommand(string LogId, string TargetAction = "Trash") : ICommand<EmailActionLog>;

public class ApproveEmailActionCommandHandler : ICommandHandler<ApproveEmailActionCommand, EmailActionLog>
{
    private readonly IRepository<EmailActionLog> _actionLogRepo;
    private readonly IGmailService _gmailService;
    private readonly ILogger<ApproveEmailActionCommandHandler> _logger;

    public ApproveEmailActionCommandHandler(
        IRepository<EmailActionLog> actionLogRepo,
        IGmailService gmailService,
        ILogger<ApproveEmailActionCommandHandler> logger)
    {
        _actionLogRepo = actionLogRepo;
        _gmailService = gmailService;
        _logger = logger;
    }

    public async Task<EmailActionLog> HandleAsync(ApproveEmailActionCommand command, CancellationToken ct = default)
    {
        var log = await _actionLogRepo.GetByIdAsync(command.LogId, ct);
        if (log == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhật ký hành động email với ID: {command.LogId}");
        }

        if (command.TargetAction.Equals("Archive", StringComparison.OrdinalIgnoreCase))
        {
            await _gmailService.ArchiveEmailAsync(log.EmailId, ct);
            log.Action = "Archived";
        }
        else
        {
            await _gmailService.TrashEmailAsync(log.EmailId, ct);
            log.Action = "Trashed";
        }

        log.Reason = $"[Người dùng duyệt] {log.Reason}";
        log.ExecutedAt = DateTime.UtcNow;
        await _actionLogRepo.UpdateAsync(log, ct);

        _logger.LogInformation("Đã phê duyệt xử lý email {EmailId} ({Action}) thành công.", log.EmailId, log.Action);
        return log;
    }
}

public record DismissEmailActionCommand(string LogId) : ICommand<EmailActionLog>;

public class DismissEmailActionCommandHandler : ICommandHandler<DismissEmailActionCommand, EmailActionLog>
{
    private readonly IRepository<EmailActionLog> _actionLogRepo;
    private readonly ILogger<DismissEmailActionCommandHandler> _logger;

    public DismissEmailActionCommandHandler(
        IRepository<EmailActionLog> actionLogRepo,
        ILogger<DismissEmailActionCommandHandler> logger)
    {
        _actionLogRepo = actionLogRepo;
        _logger = logger;
    }

    public async Task<EmailActionLog> HandleAsync(DismissEmailActionCommand command, CancellationToken ct = default)
    {
        var log = await _actionLogRepo.GetByIdAsync(command.LogId, ct);
        if (log == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhật ký hành động email với ID: {command.LogId}");
        }

        log.Action = "Dismissed";
        log.Reason = $"[Người dùng bỏ qua] {log.Reason}";
        log.ExecutedAt = DateTime.UtcNow;
        await _actionLogRepo.UpdateAsync(log, ct);

        _logger.LogInformation("Đã bỏ qua email {EmailId} khỏi danh sách chờ duyệt.", log.EmailId);
        return log;
    }
}

public record BatchPendingEmailActionCommand(List<string> LogIds, string TargetAction = "Trash") : ICommand<BatchPendingEmailActionResult>;

public class BatchPendingEmailActionResult
{
    public int TotalRequested { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> ProcessedLogIds { get; set; } = new();
}

public class BatchPendingEmailActionCommandHandler : ICommandHandler<BatchPendingEmailActionCommand, BatchPendingEmailActionResult>
{
    private readonly IRepository<EmailActionLog> _actionLogRepo;
    private readonly IGmailService _gmailService;
    private readonly ILogger<BatchPendingEmailActionCommandHandler> _logger;

    public BatchPendingEmailActionCommandHandler(
        IRepository<EmailActionLog> actionLogRepo,
        IGmailService gmailService,
        ILogger<BatchPendingEmailActionCommandHandler> logger)
    {
        _actionLogRepo = actionLogRepo;
        _gmailService = gmailService;
        _logger = logger;
    }

    public async Task<BatchPendingEmailActionResult> HandleAsync(BatchPendingEmailActionCommand command, CancellationToken ct = default)
    {
        var result = new BatchPendingEmailActionResult
        {
            TotalRequested = command.LogIds?.Count ?? 0
        };

        if (command.LogIds == null || !command.LogIds.Any())
            return result;

        var logIdsSet = command.LogIds.ToHashSet();
        var logs = await _actionLogRepo.FindAsync(x => logIdsSet.Contains(x.Id), ct);

        foreach (var log in logs)
        {
            try
            {
                if (command.TargetAction.Equals("Archive", StringComparison.OrdinalIgnoreCase))
                {
                    await _gmailService.ArchiveEmailAsync(log.EmailId, ct);
                    log.Action = "Archived";
                    log.Reason = $"[Duyệt hàng loạt - Lưu trữ] {log.Reason}";
                }
                else if (command.TargetAction.Equals("Dismiss", StringComparison.OrdinalIgnoreCase) || 
                         command.TargetAction.Equals("Reject", StringComparison.OrdinalIgnoreCase))
                {
                    log.Action = "Dismissed";
                    log.Reason = $"[Duyệt hàng loạt - Bỏ qua] {log.Reason}";
                }
                else
                {
                    await _gmailService.TrashEmailAsync(log.EmailId, ct);
                    log.Action = "Trashed";
                    log.Reason = $"[Duyệt hàng loạt - Thùng rác] {log.Reason}";
                }

                log.ExecutedAt = DateTime.UtcNow;
                await _actionLogRepo.UpdateAsync(log, ct);
                result.SuccessCount++;
                result.ProcessedLogIds.Add(log.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi xử lý hàng loạt action log {LogId} cho email {EmailId}", log.Id, log.EmailId);
                result.FailedCount++;
            }
            finally
            {
                result.ProcessedCount++;
            }
        }

        return result;
    }
}

