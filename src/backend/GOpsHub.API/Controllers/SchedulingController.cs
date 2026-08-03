using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Models;
using GOpsHub.Application.Features.Scheduling;
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

    public SchedulingController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
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
}
