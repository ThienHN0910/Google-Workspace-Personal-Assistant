using System.Net.Http.Headers;
using System.Text.Json;
using GOpsHub.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.Security;

public class GoogleTokenService : IGoogleTokenService
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ILogger<GoogleTokenService> _logger;

    // Scopes required for Gmail, Calendar, Drive, Sheets, Tasks
    private static readonly string[] Scopes = new[]
    {
        "openid",
        "email",
        "profile",
        "https://www.googleapis.com/auth/gmail.modify",
        "https://www.googleapis.com/auth/gmail.compose",
        "https://www.googleapis.com/auth/calendar",
        "https://www.googleapis.com/auth/drive",
        "https://www.googleapis.com/auth/spreadsheets",
        "https://www.googleapis.com/auth/tasks"
    };

    public GoogleTokenService(IConfiguration configuration, ILogger<GoogleTokenService> logger)
    {
        _httpClient = new HttpClient();
        _clientId = configuration["GOOGLE_CLIENT_ID"] ?? configuration["Google:ClientId"] ?? "";
        _clientSecret = configuration["GOOGLE_CLIENT_SECRET"] ?? configuration["Google:ClientSecret"] ?? "";
        _logger = logger;
    }

    public string GetAuthorizationUrl(string redirectUri)
    {
        var scopeString = string.Join(" ", Scopes);
        var url = "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(_clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString(scopeString)}" +
            $"&access_type=offline" +
            $"&prompt=consent";

        return url;
    }

    public async Task<GoogleTokenResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Google token exchange failed: {StatusCode} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Failed to exchange authorization code: {body}");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        return new GoogleTokenResult
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? "",
            RefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
            ExpiresInSeconds = root.GetProperty("expires_in").GetInt32()
        };
    }

    public async Task<GoogleTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["grant_type"] = "refresh_token"
        });

        var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Google token refresh failed: {StatusCode} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Failed to refresh access token: {body}");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        return new GoogleTokenResult
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? "",
            RefreshToken = refreshToken, // Refresh token doesn't change on refresh
            ExpiresInSeconds = root.GetProperty("expires_in").GetInt32()
        };
    }
}
