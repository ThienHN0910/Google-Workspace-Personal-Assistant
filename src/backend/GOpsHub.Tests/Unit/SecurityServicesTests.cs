using FluentAssertions;
using GOpsHub.Domain.Entities;
using GOpsHub.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class SecurityServicesTests
{
    [Fact]
    public void JwtService_GenerateToken_ThrowsWhenSecretMissing()
    {
        var config = new ConfigurationBuilder().Build();
        var service = new JwtService(config);
        var user = new AdminUser { Id = "u1", Email = "admin@example.com", DisplayName = "Admin" };

        var act = () => service.GenerateToken(user);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT_SECRET is not configured*");
    }

    [Fact]
    public void JwtService_GenerateToken_SucceedsWhenSecretConfigured()
    {
        var inMemory = new Dictionary<string, string?>
        {
            { "JWT_SECRET", "ThisIsAValidSecretKeyForTestingJwtService32CharsLong!" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
        var service = new JwtService(config);
        var user = new AdminUser { Id = "u1", Email = "admin@example.com", DisplayName = "Admin" };

        var token = service.GenerateToken(user);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TokenEncryptionService_ThrowsWhenKeyMissing()
    {
        var config = new ConfigurationBuilder().Build();

        var act = () => new TokenEncryptionService(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TOKEN_ENCRYPTION_KEY is not configured*");
    }

    [Fact]
    public void TokenEncryptionService_ThrowsWhenKeyTooShort()
    {
        var inMemory = new Dictionary<string, string?>
        {
            { "TOKEN_ENCRYPTION_KEY", "ShortKey123" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

        var act = () => new TokenEncryptionService(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must be at least 32 characters long*");
    }

    [Fact]
    public void TokenEncryptionService_EncryptAndDecrypt_RoundTripsSuccessfully()
    {
        var inMemory = new Dictionary<string, string?>
        {
            { "TOKEN_ENCRYPTION_KEY", "Exact32CharacterKeyForTesting123" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
        var service = new TokenEncryptionService(config);

        var encrypted = service.Encrypt("my-secret-refresh-token");
        var decrypted = service.Decrypt(encrypted);

        decrypted.Should().Be("my-secret-refresh-token");
    }
}
