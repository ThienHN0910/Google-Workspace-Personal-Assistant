using GOpsHub.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOpsHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    /// <summary>
    /// System health check
    /// </summary>
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            Status = "Healthy",
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow
        }));
    }

    /// <summary>
    /// Public Calendar busy/free slots for public viewers
    /// </summary>
    [HttpGet("calendar-status")]
    public IActionResult GetPublicCalendarStatus()
    {
        // Public mockup or read-only status for non-logged-in users
        return Ok(ApiResponse<object>.Ok(new
        {
            IsBusyNow = false,
            NextBusySlot = (string?)null,
            Message = "Lịch làm việc cá nhân của Thien HN"
        }));
    }
}
