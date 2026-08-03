using GOpsHub.Application.Common.CQRS;

namespace GOpsHub.Application.Features.Auth.Commands;

/// <summary>
/// Command to handle Google OAuth login.
/// </summary>
public record GoogleLoginCommand(string IdToken) : ICommand<GoogleLoginResult>;

public class GoogleLoginResult
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
