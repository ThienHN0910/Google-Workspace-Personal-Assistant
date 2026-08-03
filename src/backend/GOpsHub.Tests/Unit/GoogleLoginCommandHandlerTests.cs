using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.Auth.Commands;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class GoogleLoginCommandHandlerTests
{
    private readonly IGoogleAuthService _googleAuthService = Substitute.For<IGoogleAuthService>();
    private readonly IRepository<AdminUser> _userRepository = Substitute.For<IRepository<AdminUser>>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly GoogleLoginCommandHandler _handler;

    public GoogleLoginCommandHandlerTests()
    {
        _handler = new GoogleLoginCommandHandler(_googleAuthService, _userRepository, _jwtService);
    }

    [Fact]
    public async Task HandleAsync_WithValidAdminToken_ShouldReturnJwtAndUser()
    {
        // Arrange
        var command = new GoogleLoginCommand("valid-token");
        var payload = new GooglePayloadInfo
        {
            Email = "hnt.vn.vn@gmail.com",
            Name = "Thien HN",
            Picture = "https://avatar.com/me.png",
            Subject = "google-id-123"
        };

        _googleAuthService.VerifyIdTokenAsync("valid-token").Returns(payload);
        _userRepository.FindOneAsync(Arg.Any<System.Linq.Expressions.Expression<Func<AdminUser, bool>>>())
            .Returns((AdminUser?)null);

        _jwtService.GenerateToken(Arg.Any<AdminUser>()).Returns("generated-jwt-token");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("generated-jwt-token");
        result.User.Email.Should().Be("hnt.vn.vn@gmail.com");
        result.User.DisplayName.Should().Be("Thien HN");
    }

    [Fact]
    public async Task HandleAsync_WithNonAdminEmail_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var command = new GoogleLoginCommand("valid-token");
        var payload = new GooglePayloadInfo
        {
            Email = "unauthorized@gmail.com",
            Name = "Stranger",
            Subject = "google-id-456"
        };

        _googleAuthService.VerifyIdTokenAsync("valid-token").Returns(payload);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*không có quyền truy cập*");
    }
}
