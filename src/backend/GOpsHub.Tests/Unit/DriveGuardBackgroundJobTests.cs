using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.DriveGuard;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class DriveGuardBackgroundJobTests
{
    private readonly IRepository<MonitoredFolder> _folderRepo = Substitute.For<IRepository<MonitoredFolder>>();
    private readonly IRepository<DriveAuditLog> _logRepo = Substitute.For<IRepository<DriveAuditLog>>();
    private readonly IRepository<SecurityAlert> _alertRepo = Substitute.For<IRepository<SecurityAlert>>();
    private readonly IDriveService _driveService = Substitute.For<IDriveService>();
    private readonly ILogger<DriveGuardBackgroundJob> _logger = Substitute.For<ILogger<DriveGuardBackgroundJob>>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly DriveGuardBackgroundJob _job;

    public DriveGuardBackgroundJobTests()
    {
        _job = new DriveGuardBackgroundJob(
            _folderRepo,
            _logRepo,
            _alertRepo,
            _driveService,
            _logger,
            _notificationService);
    }

    [Fact]
    public async Task RunAuditAsync_WhenBulkDeleteDetected_ShouldTriggerCriticalAlert()
    {
        // Arrange
        var folder = new MonitoredFolder
        {
            Id = "folder-1",
            FolderName = "Confidential",
            GoogleFolderId = "g-folder-1",
            IsActive = true,
            AlertOnBulkDelete = true,
            BulkDeleteThreshold = 3,
            KnownFileIds = new List<string> { "f1", "f2", "f3", "f4" },
            LastCheckedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        _folderRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<MonitoredFolder, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<MonitoredFolder> { folder });

        // Only 1 file remains, meaning 3 were deleted (f2, f3, f4)
        _driveService.ListFilesInFolderAsync(folder.GoogleFolderId, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileInfo>
            {
                new() { Id = "f1", Name = "doc.pdf", MimeType = "application/pdf" }
            });

        // Act
        await _job.RunAuditAsync(CancellationToken.None);

        // Assert
        await _alertRepo.Received(1).CreateAsync(
            Arg.Is<SecurityAlert>(a => a.AlertType == AlertType.BulkDelete && a.Severity == AlertSeverity.Critical),
            Arg.Any<CancellationToken>());

        await _notificationService.Received(1).SendNotificationAsync(
            Arg.Is<string>(s => s.Contains("Xóa hàng loạt")),
            Arg.Any<string>(),
            "critical",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAuditAsync_WhenDangerousFileUploaded_ShouldTriggerSuspiciousFileAlert()
    {
        // Arrange
        var folder = new MonitoredFolder
        {
            Id = "folder-2",
            FolderName = "Shared Team",
            GoogleFolderId = "g-folder-2",
            IsActive = true,
            KnownFileIds = new List<string>(),
            LastCheckedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        _folderRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<MonitoredFolder, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<MonitoredFolder> { folder });

        _driveService.ListFilesInFolderAsync(folder.GoogleFolderId, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileInfo>
            {
                new() { Id = "payload-1", Name = "exploit.ps1", MimeType = "text/plain" }
            });

        // Act
        await _job.RunAuditAsync(CancellationToken.None);

        // Assert
        await _alertRepo.Received(1).CreateAsync(
            Arg.Is<SecurityAlert>(a => a.AlertType == AlertType.SuspiciousFile && a.Severity == AlertSeverity.High),
            Arg.Any<CancellationToken>());

        await _notificationService.Received(1).SendNotificationAsync(
            Arg.Is<string>(s => s.Contains("Drive Guard")),
            Arg.Is<string>(s => s.Contains(".ps1")),
            "critical",
            Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task RunAuditAsync_FirstScanOfNewFolder_RecordsBaselineWithoutAlerts()
    {
        var folder = new MonitoredFolder
        {
            Id = "folder-3",
            FolderName = "Archive",
            GoogleFolderId = "g-folder-3",
            IsActive = true,
            LastCheckedAt = null
        };
        _folderRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<MonitoredFolder, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<MonitoredFolder> { folder });
        _driveService.ListFilesInFolderAsync(folder.GoogleFolderId, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileInfo>
            {
                new() { Id = "old-1", Name = "backup.zip", MimeType = "application/zip" },
                new() { Id = "old-2", Name = "notes.txt", MimeType = "text/plain" }
            });

        await _job.RunAuditAsync(CancellationToken.None);

        folder.KnownFileIds.Should().BeEquivalentTo(new[] { "old-1", "old-2" });
        folder.LastCheckedAt.Should().NotBeNull();
        await _folderRepo.Received(1).UpdateAsync(folder, Arg.Any<CancellationToken>());
        await _logRepo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await _alertRepo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await _notificationService.DidNotReceiveWithAnyArgs().SendNotificationAsync(default!, default!, default!, default);
    }
}
