using GOpsHub.Domain.Entities;

namespace GOpsHub.Application.Common.Interfaces;

public interface ITasksService
{
    Task<string> GetDefaultTaskListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> GetTasksAsync(string taskListId, CancellationToken ct = default);
    Task<string> CreateTaskAsync(string taskListId, string title, string? notes, DateTime? due, CancellationToken ct = default);
    Task CompleteTaskAsync(string taskListId, string taskId, CancellationToken ct = default);
    Task DeleteTaskAsync(string taskListId, string taskId, CancellationToken ct = default);
}
