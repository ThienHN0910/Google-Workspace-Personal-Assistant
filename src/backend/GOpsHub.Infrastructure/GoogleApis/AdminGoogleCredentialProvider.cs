using Google.Apis.Auth.OAuth2;
using GOpsHub.Application.Common;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.GoogleApis;

public interface IAdminGoogleCredentialProvider
{
    Task<GoogleCredential?> GetCredentialAsync(string apiName, CancellationToken ct = default);
    Task<GoogleCredential?> GetCredentialAsync(string apiName, bool forceRefresh, CancellationToken ct = default);
}

public class AdminGoogleCredentialProvider : IAdminGoogleCredentialProvider
{
    private readonly IRepository<AdminUser> _userRepo;
    private readonly ITokenEncryptionService _encryptionService;
    private readonly IGoogleTokenService _googleTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminGoogleCredentialProvider> _logger;

    public AdminGoogleCredentialProvider(
        IRepository<AdminUser> userRepo,
        ITokenEncryptionService encryptionService,
        IGoogleTokenService googleTokenService,
        IConfiguration configuration,
        ILogger<AdminGoogleCredentialProvider> logger)
    {
        _userRepo = userRepo;
        _encryptionService = encryptionService;
        _googleTokenService = googleTokenService;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<GoogleCredential?> GetCredentialAsync(string apiName, CancellationToken ct = default)
        => GetCredentialAsync(apiName, forceRefresh: false, ct);

    public async Task<GoogleCredential?> GetCredentialAsync(string apiName, bool forceRefresh, CancellationToken ct = default)
    {
        var adminEmail = AdminAccount.ResolveEmail(_configuration);
        var user = await _userRepo.FindOneAsync(u => u.Email == adminEmail, ct);
        if (user == null || string.IsNullOrEmpty(user.GoogleAccessToken))
        {
            _logger.LogWarning("Admin user token not found for {ApiName} API calls.", apiName);
            return null;
        }

        var accessToken = _encryptionService.Decrypt(user.GoogleAccessToken);

        // Auto-refresh token if expired, expiring within 5 minutes, or forced
        var isExpiringSoon = !user.GoogleTokenExpiresAt.HasValue || user.GoogleTokenExpiresAt.Value <= DateTime.UtcNow.AddMinutes(5);
        if (forceRefresh || isExpiringSoon)
        {
            if (!string.IsNullOrEmpty(user.GoogleRefreshToken))
            {
                try
                {
                    var refreshToken = _encryptionService.Decrypt(user.GoogleRefreshToken);
                    var newTokens = await _googleTokenService.RefreshAccessTokenAsync(refreshToken, ct);

                    accessToken = newTokens.AccessToken;
                    user.GoogleAccessToken = _encryptionService.Encrypt(newTokens.AccessToken);
                    user.GoogleTokenExpiresAt = DateTime.UtcNow.AddSeconds(newTokens.ExpiresInSeconds);
                    await _userRepo.UpdateAsync(user, ct);

                    _logger.LogInformation("Successfully refreshed Google access token for {ApiName} API.", apiName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh Google access token for {ApiName}.", apiName);
                    if (forceRefresh) return null;
                    if (user.GoogleTokenExpiresAt.HasValue && user.GoogleTokenExpiresAt.Value <= DateTime.UtcNow)
                    {
                        return null;
                    }
                }
            }
            else
            {
                _logger.LogWarning("Google access token expired and no refresh token available for {ApiName}.", apiName);
                if (forceRefresh || (user.GoogleTokenExpiresAt.HasValue && user.GoogleTokenExpiresAt.Value <= DateTime.UtcNow))
                {
                    return null;
                }
            }
        }

        return GoogleCredential.FromAccessToken(accessToken);
    }
}
