using GOpsHub.Application.Common.CQRS;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;

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

