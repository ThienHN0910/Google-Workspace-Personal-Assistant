using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Common.Models;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;

namespace GOpsHub.Application.Features.DriveGuard;

public record AddMonitoredFolderCommand(string GoogleFolderId, string FolderName, string? FolderPath) : ICommand<MonitoredFolder>;

public class AddMonitoredFolderCommandHandler : ICommandHandler<AddMonitoredFolderCommand, MonitoredFolder>
{
    private readonly IRepository<MonitoredFolder> _folderRepo;

    public AddMonitoredFolderCommandHandler(IRepository<MonitoredFolder> folderRepo)
    {
        _folderRepo = folderRepo;
    }

    public async Task<MonitoredFolder> HandleAsync(AddMonitoredFolderCommand command, CancellationToken ct = default)
    {
        var folder = new MonitoredFolder
        {
            GoogleFolderId = command.GoogleFolderId,
            FolderName = command.FolderName,
            FolderPath = command.FolderPath,
            IsActive = true,
            AlertOnBulkDelete = true,
            BulkDeleteThreshold = 5
        };

        return await _folderRepo.CreateAsync(folder, ct);
    }
}

public record QuarantineFileCommand(string FileId, string QuarantineFolderId) : ICommand<bool>;

public class QuarantineFileCommandHandler : ICommandHandler<QuarantineFileCommand, bool>
{
    private readonly IDriveService _driveService;
    private readonly IRepository<SecurityAlert> _alertRepo;

    public QuarantineFileCommandHandler(IDriveService driveService, IRepository<SecurityAlert> alertRepo)
    {
        _driveService = driveService;
        _alertRepo = alertRepo;
    }

    public async Task<bool> HandleAsync(QuarantineFileCommand command, CancellationToken ct = default)
    {
        await _driveService.MoveFileAsync(command.FileId, command.QuarantineFolderId, ct);

        var alert = await _alertRepo.FindOneAsync(a => a.FileId == command.FileId, ct);
        if (alert != null)
        {
            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            alert.ResolutionNote = "File quarantined automatically.";
            await _alertRepo.UpdateAsync(alert, ct);
        }

        return true;
    }
}

public record GetAuditLogsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<DriveAuditLog>>;

public class GetAuditLogsQueryHandler : IQueryHandler<GetAuditLogsQuery, PagedResult<DriveAuditLog>>
{
    private readonly IRepository<DriveAuditLog> _logRepo;

    public GetAuditLogsQueryHandler(IRepository<DriveAuditLog> logRepo)
    {
        _logRepo = logRepo;
    }

    public async Task<PagedResult<DriveAuditLog>> HandleAsync(GetAuditLogsQuery query, CancellationToken ct = default)
    {
        var (items, total) = await _logRepo.GetPagedAsync(
            null,
            query.Page,
            query.PageSize,
            x => x.ActionTimestamp,
            true,
            ct);

        return new PagedResult<DriveAuditLog>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

public record GetSecurityAlertsQuery(int Page = 1, int PageSize = 10) : IQuery<PagedResult<SecurityAlert>>;

public class GetSecurityAlertsQueryHandler : IQueryHandler<GetSecurityAlertsQuery, PagedResult<SecurityAlert>>
{
    private readonly IRepository<SecurityAlert> _alertRepo;

    public GetSecurityAlertsQueryHandler(IRepository<SecurityAlert> alertRepo)
    {
        _alertRepo = alertRepo;
    }

    public async Task<PagedResult<SecurityAlert>> HandleAsync(GetSecurityAlertsQuery query, CancellationToken ct = default)
    {
        var (items, total) = await _alertRepo.GetPagedAsync(
            x => !x.IsResolved,
            query.Page,
            query.PageSize,
            x => x.CreatedAt,
            true,
            ct);

        return new PagedResult<SecurityAlert>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
