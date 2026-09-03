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
    private readonly IRepository<SecurityAlert> _alertRepo;
    private readonly IDriveService _driveService;
    private readonly ILogger<DriveGuardBackgroundJob> _logger;
    private readonly INotificationService _notificationService;

    private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".bat", ".vbs", ".sh", ".cmd", ".ps1", ".scr", ".msi", ".dll", ".7z", ".rar"
    };

    public DriveGuardBackgroundJob(
        IRepository<MonitoredFolder> folderRepo,
        IRepository<DriveAuditLog> logRepo,
        IRepository<SecurityAlert> alertRepo,
        IDriveService driveService,
        ILogger<DriveGuardBackgroundJob> logger,
        INotificationService notificationService)
    {
        _folderRepo = folderRepo;
        _logRepo = logRepo;
        _alertRepo = alertRepo;
        _driveService = driveService;
        _logger = logger;
        _notificationService = notificationService;
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
                var currentFileIds = files.Select(f => f.Id).ToHashSet();
                var knownFileIds = folder.KnownFileIds.ToHashSet();

                // 1. Tìm các file mới thêm (Có trong files, nhưng KHÔNG có trong knownFileIds)
                var newFiles = files.Where(f => !knownFileIds.Contains(f.Id)).ToList();
                foreach (var file in newFiles)
                {
                    var log = new DriveAuditLog
                    {
                        MonitoredFolderId = folder.Id,
                        GoogleFileId = file.Id,
                        FileName = file.Name,
                        FileType = file.MimeType,
                        Action = DriveAction.Created,
                        ActorEmail = file.LastModifyingUser,
                        ActionTimestamp = file.ModifiedTime ?? DateTime.UtcNow,
                        Details = $"File được thêm mới: {file.Name}"
                    };
                    await _logRepo.CreateAsync(log, ct);
                    _logger.LogInformation("Logged Drive activity for new file: {FileName}", file.Name);

                    // Kiểm tra file nguy hiểm (mở rộng danh sách đuôi)
                    var ext = Path.GetExtension(file.Name)?.ToLower();
                    if (!string.IsNullOrEmpty(ext) && DangerousExtensions.Contains(ext))
                    {
                        var alert = new SecurityAlert
                        {
                            Severity = AlertSeverity.High,
                            AlertType = AlertType.SuspiciousFile,
                            FileId = file.Id,
                            FileName = file.Name,
                            FilePath = folder.FolderName,
                            Description = $"Phát hiện file thực thi/nén nguy hiểm ({ext}) vừa được upload lên thư mục {folder.FolderName}. Cần người dùng xác nhận cách ly trên Dashboard.",
                            IsResolved = false
                        };
                        await _alertRepo.CreateAsync(alert, ct);
                        _logger.LogWarning("Security Alert: Suspicious file detected: {FileName}", file.Name);
                        await _notificationService.SendNotificationAsync(
                            "🚨 Cảnh báo an ninh Drive Guard",
                            $"Phát hiện file nguy hiểm ({ext}): {file.Name} tại thư mục {folder.FolderName}. Vui lòng vào Dashboard phê duyệt cách ly.",
                            "critical",
                            ct);
                    }
                }

                // 2. Tìm các file bị xóa (Có trong knownFileIds, nhưng KHÔNG có trong files)
                var deletedIds = knownFileIds.Except(currentFileIds).ToList();
                if (deletedIds.Count > 0)
                {
                    foreach (var delId in deletedIds)
                    {
                        var log = new DriveAuditLog
                        {
                            MonitoredFolderId = folder.Id,
                            GoogleFileId = delId,
                            FileName = "Unknown (Deleted)",
                            Action = DriveAction.Deleted,
                            ActionTimestamp = DateTime.UtcNow,
                            Details = $"File ID {delId} đã bị xóa khỏi thư mục."
                        };
                        await _logRepo.CreateAsync(log, ct);
                        _logger.LogInformation("Logged Drive activity for deleted file: {FileId}", delId);
                    }

                    // 3. Kiểm tra cảnh báo xóa hàng loạt (Bulk Delete Detection - UC05)
                    var threshold = folder.BulkDeleteThreshold > 0 ? folder.BulkDeleteThreshold : 5;
                    if (folder.AlertOnBulkDelete && deletedIds.Count >= threshold)
                    {
                        var bulkAlert = new SecurityAlert
                        {
                            Severity = AlertSeverity.Critical,
                            AlertType = AlertType.BulkDelete,
                            FilePath = folder.FolderName,
                            Description = $"CẢNH BÁO KHẨN CẤP: Phát hiện xóa hàng loạt {deletedIds.Count} file (ngưỡng: {threshold}) trong thư mục {folder.FolderName}!",
                            IsResolved = false
                        };
                        await _alertRepo.CreateAsync(bulkAlert, ct);
                        _logger.LogWarning("Security Alert: Bulk delete detected in folder {FolderName} ({Count} files)", folder.FolderName, deletedIds.Count);
                        await _notificationService.SendNotificationAsync(
                            "🚨 Cảnh báo Xóa hàng loạt Drive Guard",
                            $"Phát hiện {deletedIds.Count} file bị xóa đồng loạt tại thư mục {folder.FolderName} (ngưỡng: {threshold}). Vui lòng kiểm tra lại Google Drive!",
                            "critical",
                            ct);
                    }
                }

                // Cập nhật lại bộ nhớ
                folder.KnownFileIds = currentFileIds.ToList();
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
