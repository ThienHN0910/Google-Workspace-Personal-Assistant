using GOpsHub.Application.Common.Interfaces;
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

    [HttpGet("calendar-status")]
    public async Task<IActionResult> GetPublicCalendarStatus(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromServices] ICalendarService calendarService,
        CancellationToken ct)
    {
        DateTime minDate = startDate ?? DateTime.UtcNow.AddDays(-30);
        DateTime maxDate = endDate ?? DateTime.UtcNow.AddDays(30);

        var upcomingEvents = await calendarService.GetEventsAsync(minDate, maxDate, ct);
        var maskedEvents = new List<object>();

        foreach (var ev in upcomingEvents)
        {
            if (ev.Visibility.Equals("public", StringComparison.OrdinalIgnoreCase))
            {
                maskedEvents.Add(new
                {
                    Title = ev.Title,
                    Start = ev.Start,
                    End = ev.End,
                    Location = ev.Location,
                    IsPublic = true
                });
            }
            else
            {
                maskedEvents.Add(new
                {
                    Title = "Lịch bận",
                    Start = ev.Start,
                    End = ev.End,
                    Location = (string?)null,
                    IsPublic = false
                });
            }
        }

        var isBusyNow = upcomingEvents.Any(e => e.Start <= DateTime.UtcNow && (e.End == null || e.End >= DateTime.UtcNow));

        return Ok(ApiResponse<object>.Ok(new
        {
            IsBusyNow = isBusyNow,
            Events = maskedEvents,
            Message = "Lịch làm việc cá nhân của Thien HN"
        }));
    }
}
