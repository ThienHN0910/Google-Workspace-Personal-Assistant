using Google.Apis.Services;
using Google.Apis.Tasks.v1;
using Google.Apis.Tasks.v1.Data;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using Microsoft.Extensions.Logging;
using GoogleTask = Google.Apis.Tasks.v1.Data.Task;

namespace GOpsHub.Infrastructure.GoogleApis;

public class TasksApiService : ITasksService
{
    private readonly IAdminGoogleCredentialProvider _credentialProvider;
    private readonly ILogger<TasksApiService> _logger;

    public TasksApiService(
        IAdminGoogleCredentialProvider credentialProvider,
        ILogger<TasksApiService> logger)
    {
        _credentialProvider = credentialProvider;
        _logger = logger;
    }

    private async Task<TasksService?> GetTasksClientAsync(CancellationToken ct = default)
    {
        var credential = await _credentialProvider.GetCredentialAsync("Tasks", ct);
        if (credential == null) return null;

        return new TasksService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "G-Ops Hub"
        });
    }

    public async Task<string> GetDefaultTaskListAsync(CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return string.Empty;

        var request = service.Tasklists.List();
        var response = await request.ExecuteAsync(ct);
        var defaultList = response.Items?.FirstOrDefault();
        
        return defaultList?.Id ?? "@default";
    }

    public async Task<IReadOnlyList<TaskListInfo>> GetTaskListsAsync(CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return Array.Empty<TaskListInfo>();

        var request = service.Tasklists.List();
        var response = await request.ExecuteAsync(ct);
        if (response.Items == null) return Array.Empty<TaskListInfo>();

        return response.Items.Select(tl => new TaskListInfo
        {
            Id = tl.Id,
            Title = tl.Title,
            Updated = string.IsNullOrEmpty(tl.Updated) ? null : DateTime.Parse(tl.Updated).ToUniversalTime()
        }).ToList();
    }

    public async Task<string> CreateTaskListAsync(string title, CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return string.Empty;

        var taskList = new Google.Apis.Tasks.v1.Data.TaskList { Title = title };
        var created = await service.Tasklists.Insert(taskList).ExecuteAsync(ct);
        return created.Id;
    }

    public async System.Threading.Tasks.Task UpdateTaskListAsync(string taskListId, string title, CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return;

        var existing = await service.Tasklists.Get(taskListId).ExecuteAsync(ct);
        if (existing == null) return;

        existing.Title = title;
        await service.Tasklists.Update(existing, taskListId).ExecuteAsync(ct);
    }

    public async System.Threading.Tasks.Task DeleteTaskListAsync(string taskListId, CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return;

        await service.Tasklists.Delete(taskListId).ExecuteAsync(ct);
    }

    public async System.Threading.Tasks.Task ClearCompletedTasksAsync(string taskListId, CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return;

        await service.Tasks.Clear(taskListId).ExecuteAsync(ct);
    }

    public async Task<IReadOnlyList<TaskItem>> GetTasksAsync(string taskListId, CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return Array.Empty<TaskItem>();

        var request = service.Tasks.List(taskListId);
        request.ShowCompleted = true;
        request.ShowHidden = true;
        var response = await request.ExecuteAsync(ct);

        if (response.Items == null) return Array.Empty<TaskItem>();

        return response.Items.Select(t => new TaskItem
        {
            GoogleTaskId = t.Id,
            GoogleTaskListId = taskListId,
            Title = t.Title,
            Notes = t.Notes,
            Due = string.IsNullOrEmpty(t.Due) ? null : DateTime.Parse(t.Due).ToUniversalTime(),
            Status = t.Status,
            ParentTaskId = t.Parent,
            IsStarred = t.Notes != null && t.Notes.Contains("⭐ [Starred]"),
            CompletedAt = string.IsNullOrEmpty(t.Completed) ? null : DateTime.Parse(t.Completed).ToUniversalTime()
        }).ToList();
    }

    public async Task<string> CreateTaskAsync(string taskListId, string title, string? notes, DateTime? due, string? parentTaskId = null, bool isStarred = false, CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return string.Empty;

        var finalNotes = notes ?? string.Empty;
        if (isStarred && !finalNotes.Contains("⭐ [Starred]"))
        {
            finalNotes = string.IsNullOrWhiteSpace(finalNotes) ? "⭐ [Starred]" : "⭐ [Starred]\n" + finalNotes;
        }

        var task = new GoogleTask
        {
            Title = title,
            Notes = string.IsNullOrWhiteSpace(finalNotes) ? null : finalNotes
        };

        if (due.HasValue)
        {
            task.Due = due.Value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
        }

        var insertRequest = service.Tasks.Insert(task, taskListId);
        if (!string.IsNullOrEmpty(parentTaskId))
        {
            insertRequest.Parent = parentTaskId;
        }

        var created = await insertRequest.ExecuteAsync(ct);
        return created.Id;
    }

    public async System.Threading.Tasks.Task UpdateTaskAsync(string taskListId, string taskId, string title, string? notes, DateTime? due, string? status = null, bool? isStarred = null, CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return;

        var task = await service.Tasks.Get(taskListId, taskId).ExecuteAsync(ct);
        if (task == null) return;

        task.Title = title;
        var existingNotes = notes ?? task.Notes ?? string.Empty;
        if (isStarred.HasValue)
        {
            if (isStarred.Value && !existingNotes.Contains("⭐ [Starred]"))
            {
                existingNotes = string.IsNullOrWhiteSpace(existingNotes) ? "⭐ [Starred]" : "⭐ [Starred]\n" + existingNotes;
            }
            else if (!isStarred.Value && existingNotes.Contains("⭐ [Starred]"))
            {
                existingNotes = existingNotes.Replace("⭐ [Starred]\n", "").Replace("⭐ [Starred]", "").Trim();
            }
        }
        task.Notes = string.IsNullOrWhiteSpace(existingNotes) ? null : existingNotes;

        if (due.HasValue)
        {
            task.Due = due.Value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
        }
        if (!string.IsNullOrEmpty(status))
        {
            task.Status = status;
        }

        await service.Tasks.Update(task, taskListId, taskId).ExecuteAsync(ct);
    }

    public async System.Threading.Tasks.Task CompleteTaskAsync(string taskListId, string taskId, CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return;

        var task = await service.Tasks.Get(taskListId, taskId).ExecuteAsync(ct);
        task.Status = "completed";
        await service.Tasks.Update(task, taskListId, taskId).ExecuteAsync(ct);
    }

    public async System.Threading.Tasks.Task UncompleteTaskAsync(string taskListId, string taskId, CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return;

        var task = await service.Tasks.Get(taskListId, taskId).ExecuteAsync(ct);
        task.Status = "needsAction";
        await service.Tasks.Update(task, taskListId, taskId).ExecuteAsync(ct);
    }

    public async System.Threading.Tasks.Task DeleteTaskAsync(string taskListId, string taskId, CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return;

        await service.Tasks.Delete(taskListId, taskId).ExecuteAsync(ct);
    }
}
