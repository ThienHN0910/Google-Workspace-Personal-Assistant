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

    public TasksController(ITasksService tasksService)
    {
        _tasksService = tasksService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskItem>>>> GetTasks(CancellationToken ct)
    {
        var listId = await _tasksService.GetDefaultTaskListAsync(ct);
        if (string.IsNullOrEmpty(listId))
            return BadRequest(ApiResponse<IReadOnlyList<TaskItem>>.Fail("Không tìm thấy Task List mặc định. Vui lòng kết nối Google."));

        var tasks = await _tasksService.GetTasksAsync(listId, ct);
        return Ok(ApiResponse<IReadOnlyList<TaskItem>>.Ok(tasks));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> CreateTask([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var listId = await _tasksService.GetDefaultTaskListAsync(ct);
        if (string.IsNullOrEmpty(listId))
            return BadRequest(ApiResponse<string>.Fail("Không tìm thấy Task List."));

        var taskId = await _tasksService.CreateTaskAsync(listId, request.Title, request.Notes, request.Due, ct);
        return Ok(ApiResponse<string>.Ok(taskId, "Đã tạo Task thành công."));
    }

    [HttpPatch("{id}/complete")]
    public async Task<ActionResult<ApiResponse<bool>>> CompleteTask(string id, CancellationToken ct)
    {
        var listId = await _tasksService.GetDefaultTaskListAsync(ct);
        await _tasksService.CompleteTaskAsync(listId, id, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã hoàn thành Task."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTask(string id, CancellationToken ct)
    {
        var listId = await _tasksService.GetDefaultTaskListAsync(ct);
        await _tasksService.DeleteTaskAsync(listId, id, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã xóa Task."));
    }
}

public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? Due { get; set; }
}
