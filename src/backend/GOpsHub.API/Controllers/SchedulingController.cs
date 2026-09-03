using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Models;
using GOpsHub.Application.Features.Scheduling;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOpsHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SchedulingController : ControllerBase
{
    private readonly IDispatcher _dispatcher;
    private readonly ICalendarService _calendarService;

    public SchedulingController(IDispatcher dispatcher, ICalendarService calendarService)
    {
        _dispatcher = dispatcher;
        _calendarService = calendarService;
    }

    /// <summary>
    /// List extracted schedules
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ExtractedSchedule>>>> GetSchedules([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _dispatcher.QueryAsync(new GetSchedulesQuery(page, pageSize));
        return Ok(ApiResponse<PagedResult<ExtractedSchedule>>.Ok(result));
    }

    /// <summary>
    /// Extract schedule from an email using Gemini AI (UC03)
    /// </summary>
    [HttpPost("extract")]
    public async Task<ActionResult<ApiResponse<ExtractedSchedule>>> ExtractSchedule([FromBody] ExtractScheduleFromEmailCommand command)
    {
        var schedule = await _dispatcher.SendAsync(command);
        if (schedule == null)
            return BadRequest(ApiResponse<object>.Fail("Không thể trích xuất lịch hẹn từ email này."));

        return Ok(ApiResponse<ExtractedSchedule>.Ok(schedule, "Đã trích xuất thông tin lịch hẹn."));
    }

    /// <summary>
    /// Confirm and push event to Google Calendar (UC03)
    /// </summary>
    [HttpPost("{id}/confirm")]
    public async Task<ActionResult<ApiResponse<ExtractedSchedule>>> ConfirmSchedule(string id)
    {
        var schedule = await _dispatcher.SendAsync(new ConfirmScheduleCommand(id));
        return Ok(ApiResponse<ExtractedSchedule>.Ok(schedule, "Đã tạo sự kiện trên Google Calendar."));
    }

    /// <summary>
    /// Get upcoming events from Google Calendar
    /// </summary>
    [HttpGet("upcoming")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CalendarEvent>>>> GetUpcomingEvents(
        [FromQuery] int days = 30,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken ct = default)
    {
        DateTime minDate = startDate ?? DateTime.UtcNow.AddDays(-30);
        DateTime maxDate = endDate ?? DateTime.UtcNow.AddDays(days);
        var events = await _calendarService.GetEventsAsync(minDate, maxDate, ct);
        return Ok(ApiResponse<IReadOnlyList<CalendarEvent>>.Ok(events));
    }

    /// <summary>
    /// Manually create a calendar event
    /// </summary>
    [HttpPost("manual")]
    public async Task<ActionResult<ApiResponse<string>>> CreateManualEvent([FromBody] CreateEventRequest request, [FromServices] ITasksService tasksService, CancellationToken ct)
    {
        var eventId = await _calendarService.CreateEventAsync(request.Title, request.Start, request.End, request.Location, request.Description, request.IsPublic, ct);
        if (string.IsNullOrEmpty(eventId))
            return BadRequest(ApiResponse<string>.Fail("Không thể tạo sự kiện. Hãy kiểm tra kết nối Google."));

        if (request.CreateTask)
        {
            try
            {
                var listId = await tasksService.GetDefaultTaskListAsync(ct);
                if (!string.IsNullOrEmpty(listId))
                {
                    await tasksService.CreateTaskAsync(listId, request.Title, request.Description, request.Start, ct);
                }
            }
            catch (Exception ex)
            {
                // Continue if task creation fails, event is already created
                Console.WriteLine($"Failed to create task for event: {ex.Message}");
            }
        }

        return Ok(ApiResponse<string>.Ok(eventId, "Đã tạo sự kiện thành công."));
    }

    /// <summary>
    /// Update an existing calendar event
    /// </summary>
    [HttpPut("events/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateEvent(string id, [FromBody] UpdateEventRequest request, CancellationToken ct)
    {
        await _calendarService.UpdateEventAsync(id, request.Title, request.Start, request.End, request.Location, request.Description, request.IsPublic, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã cập nhật sự kiện thành công."));
    }

    /// <summary>
    /// Delete a calendar event
    /// </summary>
    [HttpDelete("events/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteEvent(string id, CancellationToken ct)
    {
        await _calendarService.DeleteEventAsync(id, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã xóa sự kiện thành công."));
    }
}

public class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public bool CreateTask { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class UpdateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
}
