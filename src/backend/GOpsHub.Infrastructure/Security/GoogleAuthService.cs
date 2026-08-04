using Google.Apis.Auth;
using GOpsHub.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GOpsHub.Infrastructure.Security;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly string? _clientId;

    public GoogleAuthService(IConfiguration configuration)
    {
        _clientId = configuration["GOOGLE_CLIENT_ID"] ?? configuration["Google:ClientId"];
    }

    public async Task<GooglePayloadInfo?> VerifyIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings();
            if (!string.IsNullOrEmpty(_clientId) && !_clientId.Contains("YOUR_GOOGLE_CLIENT_ID"))
            {
                settings.Audience = new[] { _clientId };
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            if (payload == null) return null;

            return new GooglePayloadInfo
            {
                Email = payload.Email,
                Name = payload.Name,
                Picture = payload.Picture,
                Subject = payload.Subject
            };
        }
        catch
        {
            // Invalid token or validation error
            return null;
        }
    }
}
