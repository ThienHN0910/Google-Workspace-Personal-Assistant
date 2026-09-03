using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Common.Models;
using GOpsHub.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOpsHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITasksService _tasksService;
    private readonly ICalendarService _calendarService;

    public TasksController(ITasksService tasksService, ICalendarService calendarService)
    {
        _tasksService = tasksService;
        _calendarService = calendarService;
    }

    [HttpGet("lists")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskListInfo>>>> GetTaskLists(CancellationToken ct)
    {
        var lists = await _tasksService.GetTaskListsAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<TaskListInfo>>.Ok(lists));
    }

    [HttpPost("lists")]
    public async Task<ActionResult<ApiResponse<string>>> CreateTaskList([FromBody] CreateTaskListRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(ApiResponse<string>.Fail("Tiêu đề danh sách không được để trống."));

        var id = await _tasksService.CreateTaskListAsync(request.Title, ct);
        return Ok(ApiResponse<string>.Ok(id, "Đã tạo danh sách công việc thành công."));
    }

    [HttpPut("lists/{listId}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateTaskList(string listId, [FromBody] UpdateTaskListRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(ApiResponse<bool>.Fail("Tiêu đề danh sách không được để trống."));

        await _tasksService.UpdateTaskListAsync(listId, request.Title, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã cập nhật tên danh sách công việc."));
    }

    [HttpDelete("lists/{listId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTaskList(string listId, CancellationToken ct)
    {
        await _tasksService.DeleteTaskListAsync(listId, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã xóa danh sách công việc."));
    }

    [HttpPost("lists/{listId}/clear-completed")]
    public async Task<ActionResult<ApiResponse<bool>>> ClearCompleted(string listId, CancellationToken ct)
    {
        await _tasksService.ClearCompletedTasksAsync(listId, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã xóa sạch các công việc đã hoàn thành trong danh sách."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskItem>>>> GetTasks([FromQuery] string? listId, CancellationToken ct)
    {
        var targetListId = string.IsNullOrWhiteSpace(listId) ? await _tasksService.GetDefaultTaskListAsync(ct) : listId;
        if (string.IsNullOrEmpty(targetListId))
            return BadRequest(ApiResponse<IReadOnlyList<TaskItem>>.Fail("Không tìm thấy Task List. Vui lòng kết nối Google."));

        var tasks = await _tasksService.GetTasksAsync(targetListId, ct);
        return Ok(ApiResponse<IReadOnlyList<TaskItem>>.Ok(tasks));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> CreateTask([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var listId = string.IsNullOrWhiteSpace(request.TaskListId) 
            ? await _tasksService.GetDefaultTaskListAsync(ct) 
            : request.TaskListId;

        if (string.IsNullOrEmpty(listId))
            return BadRequest(ApiResponse<string>.Fail("Không tìm thấy Task List."));

        var taskId = await _tasksService.CreateTaskAsync(listId, request.Title, request.Notes, request.Due, request.ParentTaskId, request.IsStarred, ct);

        if (request.SyncToCalendar && request.CalendarStartTime.HasValue)
        {
            var endTime = request.CalendarEndTime ?? request.CalendarStartTime.Value.AddMinutes(60);
            await _calendarService.CreateEventAsync(request.Title, request.CalendarStartTime.Value, endTime, null, request.Notes, request.IsPublic, "primary", false, null, null, false, null, ct);
        }

        return Ok(ApiResponse<string>.Ok(taskId, "Đã tạo Task thành công."));
    }

    [HttpPatch("{id}/complete")]
    public async Task<ActionResult<ApiResponse<bool>>> CompleteTask(string id, [FromQuery] string? listId, CancellationToken ct)
    {
        var targetListId = string.IsNullOrWhiteSpace(listId) ? await _tasksService.GetDefaultTaskListAsync(ct) : listId;
        await _tasksService.CompleteTaskAsync(targetListId, id, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã hoàn thành Task."));
    }

    [HttpPatch("{id}/uncomplete")]
    public async Task<ActionResult<ApiResponse<bool>>> UncompleteTask(string id, [FromQuery] string? listId, CancellationToken ct)
    {
        var targetListId = string.IsNullOrWhiteSpace(listId) ? await _tasksService.GetDefaultTaskListAsync(ct) : listId;
        await _tasksService.UncompleteTaskAsync(targetListId, id, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã đánh dấu Task chưa hoàn thành."));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateTask(string id, [FromBody] UpdateTaskRequest request, CancellationToken ct)
    {
        var listId = string.IsNullOrWhiteSpace(request.TaskListId) 
            ? await _tasksService.GetDefaultTaskListAsync(ct) 
            : request.TaskListId;

        if (string.IsNullOrEmpty(listId))
            return BadRequest(ApiResponse<bool>.Fail("Không tìm thấy Task List."));

        await _tasksService.UpdateTaskAsync(listId, id, request.Title, request.Notes, request.Due, request.Status, request.IsStarred, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã cập nhật Task thành công."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTask(string id, [FromQuery] string? listId, CancellationToken ct)
    {
        var targetListId = string.IsNullOrWhiteSpace(listId) ? await _tasksService.GetDefaultTaskListAsync(ct) : listId;
        await _tasksService.DeleteTaskAsync(targetListId, id, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã xóa Task."));
    }
}

public class CreateTaskListRequest
{
    public string Title { get; set; } = string.Empty;
}

public class UpdateTaskListRequest
{
    public string Title { get; set; } = string.Empty;
}

public class CreateTaskRequest
{
    public string? TaskListId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? Due { get; set; }
    public string? ParentTaskId { get; set; }
    public bool IsStarred { get; set; }
    
    // Calendar Sync properties
    public bool SyncToCalendar { get; set; }
    public DateTime? CalendarStartTime { get; set; }
    public DateTime? CalendarEndTime { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class UpdateTaskRequest
{
    public string? TaskListId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? Due { get; set; }
    public string? Status { get; set; }
    public bool? IsStarred { get; set; }
}
