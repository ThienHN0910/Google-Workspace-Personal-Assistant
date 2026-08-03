using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Models;
using GOpsHub.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOpsHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public AuthController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Authenticate Admin user via Google OAuth ID Token
    /// </summary>
    [HttpPost("google-login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<GoogleLoginResult>>> GoogleLogin([FromBody] GoogleLoginCommand command)
    {
        var result = await _dispatcher.SendAsync(command);
        return Ok(ApiResponse<GoogleLoginResult>.Ok(result, "Đăng nhập thành công."));
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
}
