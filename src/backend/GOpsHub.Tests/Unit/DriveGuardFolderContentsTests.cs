using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.DriveGuard;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class DriveGuardFolderContentsTests
{
    private readonly IDriveService _driveService = Substitute.For<IDriveService>();

    [Fact]
    public async Task HandleAsync_WhenFolderIdProvided_ShouldReturnFilesAndSubfolders()
    {
        // Arrange
        var folderId = "test-folder-123";
        var mockItems = new List<DriveFileInfo>
        {
            new()
            {
                Id = "subfolder-1",
                Name = "Child Directory",
                MimeType = "application/vnd.google-apps.folder",
                Size = null,
                ModifiedTime = DateTime.UtcNow,
                LastModifyingUser = "admin@example.com"
            },
            new()
            {
                Id = "file-1",
                Name = "Report.xlsx",
                MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Size = 1048576, // 1MB
                ModifiedTime = DateTime.UtcNow,
                LastModifyingUser = "member@example.com"
            }
        };

        _driveService.ListFilesInFolderAsync(folderId, Arg.Any<CancellationToken>())
            .Returns(mockItems);

        var handler = new GetFolderContentsQueryHandler(_driveService);

        // Act
        var result = await handler.HandleAsync(new GetFolderContentsQuery(folderId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(f => f.Name == "Child Directory" && f.MimeType == "application/vnd.google-apps.folder");
        result.Should().Contain(f => f.Name == "Report.xlsx" && f.Size == 1048576);
    }

    [Fact]
    public async Task HandleAsync_WhenFolderIdIsEmpty_ShouldReturnEmptyListWithoutCallingService()
    {
        // Arrange
        var handler = new GetFolderContentsQueryHandler(_driveService);

        // Act
        var result = await handler.HandleAsync(new GetFolderContentsQuery(""), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        await _driveService.DidNotReceive().ListFilesInFolderAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
