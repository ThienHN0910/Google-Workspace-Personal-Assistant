using System.Security.Claims;
using GOpsHub.Domain.Entities;

namespace GOpsHub.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateToken(AdminUser user);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

public interface ITokenEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public interface IGoogleAuthService
{
    Task<GooglePayloadInfo?> VerifyIdTokenAsync(string idToken, CancellationToken ct = default);
}

public class GooglePayloadInfo
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty; // Google User ID
}
