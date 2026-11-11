using FluentAssertions;
using Google.Apis.Auth.OAuth2;
using GOpsHub.Infrastructure.GoogleApis;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class SheetsApiServiceTokenRefreshTests
{
    private readonly IAdminGoogleCredentialProvider _credentialProvider = Substitute.For<IAdminGoogleCredentialProvider>();
    private readonly ILogger<SheetsApiService> _logger = Substitute.For<ILogger<SheetsApiService>>();

    [Fact]
    public async Task WhenCredentialProviderReturnsNull_GetRangeAsync_ShouldReturnEmptyList()
    {
        // Arrange
        _credentialProvider.GetCredentialAsync("Sheets", Arg.Any<CancellationToken>())
            .Returns((GoogleCredential?)null);

        var sheetsService = new SheetsApiService(_credentialProvider, _logger);

        // Act
        var result = await sheetsService.GetRangeAsync("dummy-sheet-id", "Sheet1!A1:B2");

        // Assert
        result.Should().BeEmpty();
        await _credentialProvider.Received(1)
            .GetCredentialAsync("Sheets", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenCredentialProviderReturnsNull_CreateSheetTabAsync_ShouldReturnEmptyString()
    {
        // Arrange
        _credentialProvider.GetCredentialAsync("Sheets", Arg.Any<CancellationToken>())
            .Returns((GoogleCredential?)null);

        var sheetsService = new SheetsApiService(_credentialProvider, _logger);

        // Act
        var result = await sheetsService.CreateSheetTabAsync("dummy-sheet-id", "NewTab");

        // Assert
        result.Should().BeEmpty();
        await _credentialProvider.Received(1)
            .GetCredentialAsync("Sheets", Arg.Any<CancellationToken>());
    }
}
