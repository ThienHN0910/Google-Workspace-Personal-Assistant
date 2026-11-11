using GOpsHub.Application.Common;
using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Common.Models;
using GOpsHub.Application.Features.Auth.Commands;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GOpsHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string Jwt, DateTime ExpiresAt)> _exchangeCodes = new();
    private static readonly HttpClient _httpClient = new();
    private readonly IDispatcher _dispatcher;
    private readonly IGoogleTokenService _tokenService;
    private readonly IRepository<AdminUser> _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ITokenEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;

    public AuthController(
        IDispatcher dispatcher,
        IGoogleTokenService tokenService,
        IRepository<AdminUser> userRepository,
        IJwtService jwtService,
        ITokenEncryptionService encryptionService,
        IConfiguration configuration)
    {
        _dispatcher = dispatcher;
        _tokenService = tokenService;
        _userRepository = userRepository;
        _jwtService = jwtService;
        _encryptionService = encryptionService;
        _configuration = configuration;
    }

    /// <summary>
    /// Authenticate Admin user via Google OAuth ID Token (legacy/fallback)
    /// </summary>
    [HttpPost("google-login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<GoogleLoginResult>>> GoogleLogin([FromBody] GoogleLoginCommand command)
    {
        var result = await _dispatcher.SendAsync(command);
        return Ok(ApiResponse<GoogleLoginResult>.Ok(result, "Đăng nhập thành công."));
    }

    /// <summary>
    /// Step 1: Redirect user to Google OAuth consent screen
    /// </summary>
    [HttpGet("google-redirect")]
    [AllowAnonymous]
    public IActionResult GoogleRedirect([FromQuery] string? returnUrl)
    {
        var redirectUri = GetOAuthRedirectUri();
        string? state = null;
        if (!string.IsNullOrEmpty(returnUrl) && Uri.TryCreate(returnUrl, UriKind.Absolute, out _))
        {
            state = returnUrl;
        }
        else
        {
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
            {
                state = $"{uri.Scheme}://{uri.Authority}";
            }
        }

        var url = _tokenService.GetAuthorizationUrl(redirectUri, state);
        return Redirect(url);
    }

    /// <summary>
    /// Step 2: Google calls back with authorization code → exchange for tokens → redirect to frontend
    /// </summary>
    [HttpGet("google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken ct)
    {
        var frontendUrl = _configuration["FRONTEND_URL"] ?? "http://localhost:5173";
        if (!string.IsNullOrEmpty(state) && Uri.TryCreate(state, UriKind.Absolute, out var stateUri))
        {
            frontendUrl = $"{stateUri.Scheme}://{stateUri.Authority}";
        }

        if (!string.IsNullOrEmpty(error))
        {
            return Redirect($"{frontendUrl}/login?error={Uri.EscapeDataString(error)}");
        }

        try
        {
            var redirectUri = GetOAuthRedirectUri();
            var tokenResult = await _tokenService.ExchangeCodeAsync(code, redirectUri, ct);

            // Get user info from Google
            var userInfo = await GetGoogleUserInfoAsync(tokenResult.AccessToken, ct);
            if (userInfo == null)
            {
                return Redirect($"{frontendUrl}/login?error=Không thể lấy thông tin người dùng.");
            }

            var adminEmail = AdminAccount.ResolveEmail(_configuration);
            if (!AdminAccount.IsAdmin(userInfo.Email, _configuration))
            {
                return Redirect($"{frontendUrl}/login?error=Tài khoản {userInfo.Email} không có quyền truy cập.");
            }

            // Find or create admin user
            var user = await _userRepository.FindOneAsync(u => u.Email == adminEmail, ct);
            if (user == null)
            {
                user = new AdminUser
                {
                    Email = adminEmail,
                    DisplayName = userInfo.Name,
                    AvatarUrl = userInfo.Picture,
                    GoogleId = userInfo.Sub,
                };
                await _userRepository.CreateAsync(user, ct);
            }

            // Save encrypted tokens
            user.GoogleAccessToken = _encryptionService.Encrypt(tokenResult.AccessToken);
            if (!string.IsNullOrEmpty(tokenResult.RefreshToken))
            {
                user.GoogleRefreshToken = _encryptionService.Encrypt(tokenResult.RefreshToken);
            }
            user.GoogleTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresInSeconds);
            user.DisplayName = userInfo.Name;
            user.AvatarUrl = userInfo.Picture;
            user.LastLoginAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, ct);

            // Generate internal JWT
            var jwt = _jwtService.GenerateToken(user);

            // One-time 60s exchange code to avoid exposing raw JWT in redirect URL
            var exchangeCode = Guid.NewGuid().ToString("N");
            _exchangeCodes[exchangeCode] = (jwt, DateTime.UtcNow.AddSeconds(60));

            // Prune expired codes
            var now = DateTime.UtcNow;
            foreach (var kvp in _exchangeCodes)
            {
                if (kvp.Value.ExpiresAt <= now)
                {
                    _exchangeCodes.TryRemove(kvp.Key, out _);
                }
            }

            // Redirect to frontend with one-time exchange code
            return Redirect($"{frontendUrl}/login?code={exchangeCode}");
        }
        catch (Exception ex)
        {
            return Redirect($"{frontendUrl}/login?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    /// <summary>
    /// Exchange one-time auth code for JWT token
    /// </summary>
    [HttpPost("exchange")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<string>> ExchangeCode([FromBody] ExchangeCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Code))
        {
            return BadRequest(ApiResponse<string>.Fail("Mã xác thực không hợp lệ."));
        }

        if (_exchangeCodes.TryRemove(request.Code, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                return Ok(ApiResponse<string>.Ok(entry.Jwt, "Xác thực thành công."));
            }
        }

        return BadRequest(ApiResponse<string>.Fail("Mã xác thực không hợp lệ hoặc đã hết hạn."));
    }

    /// <summary>
    /// Check current authenticated Admin user
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Ok(ApiResponse<object>.Ok(new
        {
            Id = id,
            Email = email,
            DisplayName = name,
            Role = "Admin"
        }));
    }

    /// <summary>
    /// Check if Google tokens are connected
    /// </summary>
    [HttpGet("google-status")]
    [Authorize]
    public async Task<IActionResult> GetGoogleConnectionStatus(CancellationToken ct)
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
            return Ok(ApiResponse<object>.Ok(new { IsConnected = false }));

        var user = await _userRepository.FindOneAsync(u => u.Email == email, ct);
        var isConnected = user != null
            && !string.IsNullOrEmpty(user.GoogleAccessToken)
            && !string.IsNullOrEmpty(user.GoogleRefreshToken);

        return Ok(ApiResponse<object>.Ok(new
        {
            IsConnected = isConnected,
            TokenExpiresAt = user?.GoogleTokenExpiresAt
        }));
    }

    private string GetOAuthRedirectUri()
    {
        var backendUrl = _configuration["BACKEND_URL"]
            ?? _configuration["GOOGLE_REDIRECT_URI"]?.Replace("/api/v1/auth/google-callback", "")
            ?? "http://localhost:55763";
        return $"{backendUrl}/api/v1/auth/google-callback";
    }

    private async Task<GoogleUserInfo?> GetGoogleUserInfoAsync(string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        return System.Text.Json.JsonSerializer.Deserialize<GoogleUserInfo>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}

public record ExchangeCodeRequest(string Code);

internal class GoogleUserInfo
{
    public string Sub { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
}
