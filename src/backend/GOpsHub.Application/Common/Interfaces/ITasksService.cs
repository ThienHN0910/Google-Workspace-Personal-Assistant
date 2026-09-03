using GOpsHub.Domain.Entities;

namespace GOpsHub.Application.Common.Interfaces;

public class TaskListInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime? Updated { get; set; }
}

public interface ITasksService
{
    Task<string> GetDefaultTaskListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TaskListInfo>> GetTaskListsAsync(CancellationToken ct = default);
    Task<string> CreateTaskListAsync(string title, CancellationToken ct = default);
    Task UpdateTaskListAsync(string taskListId, string title, CancellationToken ct = default);
    Task DeleteTaskListAsync(string taskListId, CancellationToken ct = default);
    Task ClearCompletedTasksAsync(string taskListId, CancellationToken ct = default);

    Task<IReadOnlyList<TaskItem>> GetTasksAsync(string taskListId, CancellationToken ct = default);
    Task<string> CreateTaskAsync(string taskListId, string title, string? notes, DateTime? due, string? parentTaskId = null, bool isStarred = false, CancellationToken ct = default);
    Task UpdateTaskAsync(string taskListId, string taskId, string title, string? notes, DateTime? due, string? status = null, bool? isStarred = null, CancellationToken ct = default);
    Task CompleteTaskAsync(string taskListId, string taskId, CancellationToken ct = default);
    Task UncompleteTaskAsync(string taskListId, string taskId, CancellationToken ct = default);
    Task DeleteTaskAsync(string taskListId, string taskId, CancellationToken ct = default);
}
