namespace GOpsHub.Application.Common.Interfaces;

/// <summary>
/// Handles Google OAuth2 token exchange, refresh, and URL generation.
/// </summary>
public interface IGoogleTokenService
{
    /// <summary>
    /// Builds the Google OAuth2 consent URL for the user to authorize.
    /// </summary>
    string GetAuthorizationUrl(string redirectUri);

    /// <summary>
    /// Exchanges an authorization code for access and refresh tokens.
    /// Returns (accessToken, refreshToken, expiresInSeconds).
    /// </summary>
    Task<GoogleTokenResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default);

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// Returns new (accessToken, expiresInSeconds).
    /// </summary>
    Task<GoogleTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct = default);
}

public class GoogleTokenResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public int ExpiresInSeconds { get; set; }
}
