using FluentAssertions;
using GOpsHub.Application.Common;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class AdminAccountTests
{
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();

    [Fact]
    public void ResolveEmail_WhenConfigIsBlank_ReturnsDefaultEmail()
    {
        // Arrange
        _configuration["ADMIN_EMAIL"].Returns((string?)null);
        _configuration["Security:AdminEmail"].Returns((string?)null);

        // Act
        var email = AdminAccount.ResolveEmail(_configuration);

        // Assert
        email.Should().Be(AdminAccount.DefaultEmail);
    }

    [Fact]
    public void ResolveEmail_WhenConfigIsWhitespace_ReturnsDefaultEmail()
    {
        // Arrange
        _configuration["ADMIN_EMAIL"].Returns("   ");
        _configuration["Security:AdminEmail"].Returns("   ");

        // Act
        var email = AdminAccount.ResolveEmail(_configuration);

        // Assert
        email.Should().Be(AdminAccount.DefaultEmail);
    }

    [Fact]
    public void ResolveEmail_WhenAdminEmailConfiguredWithMixedCaseAndWhitespace_ReturnsTrimmedLowercase()
    {
        // Arrange
        _configuration["ADMIN_EMAIL"].Returns("   HnT.Vn.Vn@Gmail.COM   ");

        // Act
        var email = AdminAccount.ResolveEmail(_configuration);

        // Assert
        email.Should().Be("hnt.vn.vn@gmail.com");
    }

    [Fact]
    public void ResolveEmail_WhenAdminEmailBlankButSecurityAdminEmailConfigured_ReturnsSecurityAdminEmail()
    {
        // Arrange
        _configuration["ADMIN_EMAIL"].Returns(string.Empty);
        _configuration["Security:AdminEmail"].Returns("  custom.admin@corp.io  ");

        // Act
        var email = AdminAccount.ResolveEmail(_configuration);

        // Assert
        email.Should().Be("custom.admin@corp.io");
    }

    [Fact]
    public void IsAdmin_WhenEmailMatchesWithDifferentCasing_ReturnsTrue()
    {
        // Arrange
        _configuration["ADMIN_EMAIL"].Returns("Admin@Example.com");

        // Act & Assert
        AdminAccount.IsAdmin("admin@example.com", _configuration).Should().BeTrue();
        AdminAccount.IsAdmin("ADMIN@EXAMPLE.COM", _configuration).Should().BeTrue();
        AdminAccount.IsAdmin("  AdMiN@example.com  ", _configuration).Should().BeTrue();
    }

    [Fact]
    public void IsAdmin_WhenEmailDoesNotMatchOrIsBlank_ReturnsFalse()
    {
        // Arrange
        _configuration["ADMIN_EMAIL"].Returns("admin@example.com");

        // Act & Assert
        AdminAccount.IsAdmin("other@example.com", _configuration).Should().BeFalse();
        AdminAccount.IsAdmin(null, _configuration).Should().BeFalse();
        AdminAccount.IsAdmin("   ", _configuration).Should().BeFalse();
    }
}
