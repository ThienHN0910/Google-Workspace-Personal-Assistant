using System.Linq.Expressions;
using FluentAssertions;
using Google.Apis.Auth.OAuth2;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using GOpsHub.Infrastructure.GoogleApis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class AdminGoogleCredentialProviderTests
{
    private readonly IRepository<AdminUser> _userRepo = Substitute.For<IRepository<AdminUser>>();
    private readonly ITokenEncryptionService _encryptionService = Substitute.For<ITokenEncryptionService>();
    private readonly IGoogleTokenService _googleTokenService = Substitute.For<IGoogleTokenService>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly ILogger<AdminGoogleCredentialProvider> _logger = Substitute.For<ILogger<AdminGoogleCredentialProvider>>();
    private readonly AdminGoogleCredentialProvider _provider;

    public AdminGoogleCredentialProviderTests()
    {
        _configuration["ADMIN_EMAIL"].Returns("hnt.vn.vn@gmail.com");
        _provider = new AdminGoogleCredentialProvider(
            _userRepo,
            _encryptionService,
            _googleTokenService,
            _configuration,
            _logger);
    }

    [Fact]
    public async Task WhenTokenIsExpired_GetCredentialAsync_ShouldAutomaticallyRefreshTokenAndPersist()
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

        // Act
        var credential = await _provider.GetCredentialAsync("Drive");

        // Assert
        credential.Should().NotBeNull();

        await _googleTokenService.Received(1)
            .RefreshAccessTokenAsync("raw-refresh-token", Arg.Any<CancellationToken>());

        adminUser.GoogleAccessToken.Should().Be("encrypted-new-access-token");
        adminUser.GoogleTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);

        await _userRepo.Received(1)
            .UpdateAsync(adminUser, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenTokenIsValid_GetCredentialAsync_ShouldNotRefreshToken()
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

        // Act
        var credential = await _provider.GetCredentialAsync("Drive");

        // Assert
        credential.Should().NotBeNull();

        await _googleTokenService.DidNotReceive()
            .RefreshAccessTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _userRepo.DidNotReceive()
            .UpdateAsync(Arg.Any<AdminUser>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenForceRefreshIsTrue_GetCredentialAsync_ShouldRefreshEvenIfTokenIsValid()
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

        // Act
        var credential = await _provider.GetCredentialAsync("Drive", forceRefresh: true);

        // Assert
        credential.Should().NotBeNull();

        await _googleTokenService.Received(1)
            .RefreshAccessTokenAsync("raw-refresh-token", Arg.Any<CancellationToken>());

        adminUser.GoogleAccessToken.Should().Be("encrypted-forced-token");
        await _userRepo.Received(1)
            .UpdateAsync(adminUser, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenUserNotFound_GetCredentialAsync_ShouldReturnNull()
    {
        // Arrange
        _userRepo.FindOneAsync(Arg.Any<Expression<Func<AdminUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((AdminUser?)null);

        // Act
        var credential = await _provider.GetCredentialAsync("Drive");

        // Assert
        credential.Should().BeNull();
        await _googleTokenService.DidNotReceive()
            .RefreshAccessTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenTokenRefreshFails_GetCredentialAsync_ShouldReturnNull()
    {
        // Arrange
        var adminUser = new AdminUser
        {
            Id = "user-1",
            Email = "hnt.vn.vn@gmail.com",
            GoogleAccessToken = "encrypted-old-token",
            GoogleRefreshToken = "encrypted-refresh-token",
            GoogleTokenExpiresAt = DateTime.UtcNow.AddMinutes(-10)
        };

        _userRepo.FindOneAsync(Arg.Any<Expression<Func<AdminUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(adminUser);

        _encryptionService.Decrypt("encrypted-old-token").Returns("old-raw-token");
        _encryptionService.Decrypt("encrypted-refresh-token").Returns("raw-refresh-token");

        _googleTokenService.RefreshAccessTokenAsync("raw-refresh-token", Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Network failure"));

        // Act
        var credential = await _provider.GetCredentialAsync("Drive");

        // Assert
        credential.Should().BeNull();
    }
}
