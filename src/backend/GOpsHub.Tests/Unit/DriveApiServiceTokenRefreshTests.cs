using System.Linq.Expressions;
using System.Net;
using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using GOpsHub.Infrastructure.GoogleApis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class DriveApiServiceTokenRefreshTests
{
    private readonly IRepository<AdminUser> _userRepo = Substitute.For<IRepository<AdminUser>>();
    private readonly ITokenEncryptionService _encryptionService = Substitute.For<ITokenEncryptionService>();
    private readonly IGoogleTokenService _googleTokenService = Substitute.For<IGoogleTokenService>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly ILogger<DriveApiService> _logger = Substitute.For<ILogger<DriveApiService>>();

    public DriveApiServiceTokenRefreshTests()
    {
        _configuration["ADMIN_EMAIL"].Returns("hnt.vn.vn@gmail.com");
    }

    [Fact]
    public async Task WhenTokenIsExpired_EnsureFreshTokenAsync_ShouldAutomaticallyRefreshTokenAndPersist()
    {
        // Arrange
        var adminUser = new AdminUser
        {
            Id = "user-1",
            Email = "hnt.vn.vn@gmail.com",
            GoogleAccessToken = "encrypted-old-access-token",
            GoogleRefreshToken = "encrypted-refresh-token",
            GoogleTokenExpiresAt = DateTime.UtcNow.AddMinutes(-30) // Expired 30 mins ago
        };

        _userRepo.FindOneAsync(Arg.Any<Expression<Func<AdminUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(adminUser);

        _encryptionService.Decrypt("encrypted-old-access-token").Returns("old-raw-access-token");
        _encryptionService.Decrypt("encrypted-refresh-token").Returns("raw-refresh-token");
        _encryptionService.Encrypt("new-raw-access-token").Returns("encrypted-new-access-token");

        _googleTokenService.RefreshAccessTokenAsync("raw-refresh-token", Arg.Any<CancellationToken>())
            .Returns(new GoogleTokenResult
            {
                AccessToken = "new-raw-access-token",
                RefreshToken = "raw-refresh-token",
                ExpiresInSeconds = 3600
            });

        var driveService = new DriveApiService(
            _userRepo,
            _encryptionService,
            _googleTokenService,
            _configuration,
            _logger);

        // Act
        var token = await driveService.EnsureFreshTokenAsync(forceRefresh: false);

        // Assert
        token.Should().Be("new-raw-access-token");

        await _googleTokenService.Received(1)
            .RefreshAccessTokenAsync("raw-refresh-token", Arg.Any<CancellationToken>());

        adminUser.GoogleAccessToken.Should().Be("encrypted-new-access-token");
        adminUser.GoogleTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);

        await _userRepo.Received(1)
            .UpdateAsync(adminUser, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenTokenIsValid_EnsureFreshTokenAsync_ShouldNotRefreshToken()
    {
        // Arrange
        var adminUser = new AdminUser
        {
            Id = "user-1",
            Email = "hnt.vn.vn@gmail.com",
            GoogleAccessToken = "encrypted-valid-access-token",
            GoogleRefreshToken = "encrypted-refresh-token",
            GoogleTokenExpiresAt = DateTime.UtcNow.AddHours(1) // Still valid for 1 hour
        };

        _userRepo.FindOneAsync(Arg.Any<Expression<Func<AdminUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(adminUser);

        _encryptionService.Decrypt("encrypted-valid-access-token").Returns("valid-raw-access-token");

        var driveService = new DriveApiService(
            _userRepo,
            _encryptionService,
            _googleTokenService,
            _configuration,
            _logger);

        // Act
        var token = await driveService.EnsureFreshTokenAsync(forceRefresh: false);

        // Assert
        token.Should().Be("valid-raw-access-token");

        await _googleTokenService.DidNotReceive()
            .RefreshAccessTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _userRepo.DidNotReceive()
            .UpdateAsync(Arg.Any<AdminUser>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenForceRefreshIsTrue_EnsureFreshTokenAsync_ShouldRefreshEvenIfTokenIsValid()
    {
        // Arrange
        var adminUser = new AdminUser
        {
            Id = "user-1",
            Email = "hnt.vn.vn@gmail.com",
            GoogleAccessToken = "encrypted-valid-access-token",
            GoogleRefreshToken = "encrypted-refresh-token",
            GoogleTokenExpiresAt = DateTime.UtcNow.AddHours(1) // Token not expired
        };

        _userRepo.FindOneAsync(Arg.Any<Expression<Func<AdminUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(adminUser);

        _encryptionService.Decrypt("encrypted-valid-access-token").Returns("valid-raw-access-token");
        _encryptionService.Decrypt("encrypted-refresh-token").Returns("raw-refresh-token");
        _encryptionService.Encrypt("forced-refreshed-token").Returns("encrypted-forced-token");

        _googleTokenService.RefreshAccessTokenAsync("raw-refresh-token", Arg.Any<CancellationToken>())
            .Returns(new GoogleTokenResult
            {
                AccessToken = "forced-refreshed-token",
                RefreshToken = "raw-refresh-token",
                ExpiresInSeconds = 3600
            });

        var driveService = new DriveApiService(
            _userRepo,
            _encryptionService,
            _googleTokenService,
            _configuration,
            _logger);

        // Act
        var token = await driveService.EnsureFreshTokenAsync(forceRefresh: true);

        // Assert
        token.Should().Be("forced-refreshed-token");

        await _googleTokenService.Received(1)
            .RefreshAccessTokenAsync("raw-refresh-token", Arg.Any<CancellationToken>());

        adminUser.GoogleAccessToken.Should().Be("encrypted-forced-token");
        await _userRepo.Received(1)
            .UpdateAsync(adminUser, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenExecuteWithRetry_Encounter401_ShouldForceRefreshTokenAndRetrySuccessfully()
    {
        // Arrange
        var adminUser = new AdminUser
        {
            Id = "user-1",
            Email = "hnt.vn.vn@gmail.com",
            GoogleAccessToken = "encrypted-initial-token",
            GoogleRefreshToken = "encrypted-refresh-token",
            GoogleTokenExpiresAt = DateTime.UtcNow.AddMinutes(20) // Token thought to be valid, but 401s
        };

        _userRepo.FindOneAsync(Arg.Any<Expression<Func<AdminUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(adminUser);

        _encryptionService.Decrypt("encrypted-initial-token").Returns("initial-raw-token");
        _encryptionService.Decrypt("encrypted-refresh-token").Returns("raw-refresh-token");
        _encryptionService.Encrypt("fresh-raw-token").Returns("encrypted-fresh-token");

        _googleTokenService.RefreshAccessTokenAsync("raw-refresh-token", Arg.Any<CancellationToken>())
            .Returns(new GoogleTokenResult
            {
                AccessToken = "fresh-raw-token",
                RefreshToken = "raw-refresh-token",
                ExpiresInSeconds = 3600
            });

        var driveService = new DriveApiService(
            _userRepo,
            _encryptionService,
            _googleTokenService,
            _configuration,
            _logger);

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
        await _googleTokenService.Received(1)
            .RefreshAccessTokenAsync("raw-refresh-token", Arg.Any<CancellationToken>());
        adminUser.GoogleAccessToken.Should().Be("encrypted-fresh-token");
    }
}
