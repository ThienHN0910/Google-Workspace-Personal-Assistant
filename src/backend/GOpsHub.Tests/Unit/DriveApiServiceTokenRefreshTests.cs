using System.Net;
using FluentAssertions;
using Google.Apis.Auth.OAuth2;
using GOpsHub.Infrastructure.GoogleApis;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class DriveApiServiceTokenRefreshTests
{
    private readonly IAdminGoogleCredentialProvider _credentialProvider = Substitute.For<IAdminGoogleCredentialProvider>();
    private readonly ILogger<DriveApiService> _logger = Substitute.For<ILogger<DriveApiService>>();

    [Fact]
    public async Task WhenExecuteWithRetry_Encounter401_ShouldForceRefreshTokenAndRetrySuccessfully()
    {
        // Arrange
        var credential = GoogleCredential.FromAccessToken("dummy-token");
        _credentialProvider.GetCredentialAsync("Drive", Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(credential);

        var driveService = new DriveApiService(_credentialProvider, _logger);

        int callCount = 0;

        // Act - 1st invocation fails with 401, 2nd invocation succeeds after retry
        var result = await driveService.ExecuteWithRetryAsync<string>(service =>
        {
            callCount++;
            if (callCount == 1)
            {
                var error = new Google.Apis.Requests.RequestError { Code = 401, Message = "Invalid Credentials" };
                throw new Google.GoogleApiException("Drive", "Unauthorized")
                {
                    HttpStatusCode = HttpStatusCode.Unauthorized,
                    Error = error
                };
            }

            return Task.FromResult("success-file-data");
        }, defaultValue: "failed");

        // Assert
        result.Should().Be("success-file-data");
        callCount.Should().Be(2);

        // Forced refresh should have been called on 401
        await _credentialProvider.Received(1)
            .GetCredentialAsync("Drive", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenCredentialProviderReturnsNull_ExecuteWithRetry_ShouldReturnDefaultValue()
    {
        // Arrange
        _credentialProvider.GetCredentialAsync("Drive", Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((GoogleCredential?)null);

        var driveService = new DriveApiService(_credentialProvider, _logger);

        // Act
        var result = await driveService.ExecuteWithRetryAsync(service => Task.FromResult("success"), "default-value");

        // Assert
        result.Should().Be("default-value");
    }

    [Fact]
    public async Task WhenActionSucceeds_ExecuteWithRetry_ShouldNotForceRefreshToken()
    {
        // Arrange
        var credential = GoogleCredential.FromAccessToken("dummy-token");
        _credentialProvider.GetCredentialAsync("Drive", Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(credential);

        var driveService = new DriveApiService(_credentialProvider, _logger);

        // Act
        var result = await driveService.ExecuteWithRetryAsync(service => Task.FromResult("ok"), "failed");

        // Assert
        result.Should().Be("ok");
        await _credentialProvider.DidNotReceive()
            .GetCredentialAsync("Drive", true, Arg.Any<CancellationToken>());
    }
}
