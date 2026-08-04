using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Application.Features.DriveGuard;

public class DriveGuardBackgroundJob
{
    private readonly IRepository<MonitoredFolder> _folderRepo;
    private readonly IRepository<DriveAuditLog> _logRepo;
    private readonly IDriveService _driveService;
    private readonly ILogger<DriveGuardBackgroundJob> _logger;

    public DriveGuardBackgroundJob(
        IRepository<MonitoredFolder> folderRepo,
        IRepository<DriveAuditLog> logRepo,
        IDriveService driveService,
        ILogger<DriveGuardBackgroundJob> logger)
    {
        _folderRepo = folderRepo;
        _logRepo = logRepo;
        _driveService = driveService;
        _logger = logger;
    }

    public async Task RunAuditAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting Drive Guard background audit...");
        var folders = await _folderRepo.FindAsync(f => f.IsActive, ct);

        foreach (var folder in folders)
        {
            try
            {
                var files = await _driveService.ListFilesInFolderAsync(folder.GoogleFolderId, ct);
                
                if (folder.LastCheckedAt.HasValue)
                {
                    var newOrModifiedFiles = files.Where(f => f.ModifiedTime > folder.LastCheckedAt.Value).ToList();
                    
                    foreach (var file in newOrModifiedFiles)
                    {
                        var log = new DriveAuditLog
                        {
                            MonitoredFolderId = folder.Id,
                            GoogleFileId = file.Id,
                            FileName = file.Name,
                            FileType = file.MimeType,
                            Action = DriveAction.Created, // Simplification: we log modified/created as Create for now or you can map it.
                            ActorEmail = file.LastModifyingUser,
                            ActionTimestamp = file.ModifiedTime ?? DateTime.UtcNow,
                            Details = $"File modified/created: {file.Name}"
                        };
                        
                        await _logRepo.CreateAsync(log, ct);
                        _logger.LogInformation("Logged Drive activity for file: {FileName}", file.Name);
                    }
                }

                folder.LastCheckedAt = DateTime.UtcNow;
                await _folderRepo.UpdateAsync(folder, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to audit folder {FolderName} ({FolderId})", folder.FolderName, folder.GoogleFolderId);
            }
        }
        
        _logger.LogInformation("Completed Drive Guard background audit.");
    }
}
