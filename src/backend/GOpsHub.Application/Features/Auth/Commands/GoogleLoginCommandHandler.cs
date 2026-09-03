using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GOpsHub.Application.Features.Auth.Commands;

public class GoogleLoginCommandHandler : ICommandHandler<GoogleLoginCommand, GoogleLoginResult>
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IRepository<AdminUser> _userRepository;
    private readonly IJwtService _jwtService;
    private readonly string _allowedAdminEmail;

    public GoogleLoginCommandHandler(
        IGoogleAuthService googleAuthService,
        IRepository<AdminUser> userRepository,
        IJwtService jwtService,
        IConfiguration configuration)
    {
        _googleAuthService = googleAuthService;
        _userRepository = userRepository;
        _jwtService = jwtService;
        var email = configuration["ADMIN_EMAIL"] ?? configuration["Security:AdminEmail"];
        _allowedAdminEmail = string.IsNullOrWhiteSpace(email) ? "hnt.vn.vn@gmail.com" : email;
    }

    public async Task<GoogleLoginResult> HandleAsync(GoogleLoginCommand command, CancellationToken ct = default)
    {
        var payload = await _googleAuthService.VerifyIdTokenAsync(command.IdToken, ct);
        if (payload == null)
        {
            throw new UnauthorizedAccessException("Xác thực Google ID Token không thành công.");
        }

        if (!string.Equals(payload.Email, _allowedAdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Tài khoản {payload.Email} không có quyền truy cập hệ thống này.");
        }

        var user = await _userRepository.FindOneAsync(u => u.Email == _allowedAdminEmail, ct);
        if (user == null)
        {
            user = new AdminUser
            {
                Email = payload.Email,
                DisplayName = payload.Name,
                AvatarUrl = payload.Picture,
                GoogleId = payload.Subject,
                LastLoginAt = DateTime.UtcNow
            };
            await _userRepository.CreateAsync(user, ct);
        }
        else
        {
            user.DisplayName = payload.Name;
            user.AvatarUrl = payload.Picture;
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, ct);
        }

        var token = _jwtService.GenerateToken(user);

        return new GoogleLoginResult
        {
            AccessToken = token,
            ExpiresIn = 3600, // 1 hour
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl
            }
        };
    }
}
