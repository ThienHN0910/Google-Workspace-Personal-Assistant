using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Tasks.v1;
using Google.Apis.Tasks.v1.Data;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GoogleTask = Google.Apis.Tasks.v1.Data.Task;

namespace GOpsHub.Infrastructure.GoogleApis;

public class TasksApiService : ITasksService
{
    private readonly IRepository<AdminUser> _userRepo;
    private readonly ITokenEncryptionService _encryptionService;
    private readonly IGoogleTokenService _googleTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TasksApiService> _logger;
    private readonly string _adminEmail;

    public TasksApiService(
        IRepository<AdminUser> userRepo,
        ITokenEncryptionService encryptionService,
        IGoogleTokenService googleTokenService,
        IConfiguration configuration,
        ILogger<TasksApiService> logger)
    {
        _userRepo = userRepo;
        _encryptionService = encryptionService;
        _googleTokenService = googleTokenService;
        _configuration = configuration;
        _logger = logger;
        _adminEmail = _configuration["ADMIN_EMAIL"] ?? "hnt.vn.vn@gmail.com";
    }

    private async Task<TasksService?> GetTasksClientAsync(CancellationToken ct = default)
    {
        var user = await _userRepo.FindOneAsync(u => u.Email == _adminEmail, ct);
        if (user == null || string.IsNullOrEmpty(user.GoogleAccessToken))
        {
            _logger.LogWarning("Admin user token not found for Tasks API calls.");
            return null;
        }

        var accessToken = _encryptionService.Decrypt(user.GoogleAccessToken);

        // Auto-refresh token if expired or about to expire
        if (user.GoogleTokenExpiresAt.HasValue && user.GoogleTokenExpiresAt.Value <= DateTime.UtcNow.AddMinutes(5))
        {
            if (!string.IsNullOrEmpty(user.GoogleRefreshToken))
            {
                try
                {
                    var refreshToken = _encryptionService.Decrypt(user.GoogleRefreshToken);
                    var newTokens = await _googleTokenService.RefreshAccessTokenAsync(refreshToken, ct);

                    accessToken = newTokens.AccessToken;
                    user.GoogleAccessToken = _encryptionService.Encrypt(newTokens.AccessToken);
                    user.GoogleTokenExpiresAt = DateTime.UtcNow.AddSeconds(newTokens.ExpiresInSeconds);
                    await _userRepo.UpdateAsync(user, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh Google access token for Tasks.");
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        var credential = GoogleCredential.FromAccessToken(accessToken);

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
            Status = t.Status
        }).ToList();
    }

    public async Task<string> CreateTaskAsync(string taskListId, string title, string? notes, DateTime? due, CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return string.Empty;

        var task = new GoogleTask
        {
            Title = title,
            Notes = notes
        };

        if (due.HasValue)
        {
            // The Tasks API needs an RFC 3339 timestamp
            task.Due = due.Value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
        }

        var created = await service.Tasks.Insert(task, taskListId).ExecuteAsync(ct);
        return created.Id;
    }

    public async System.Threading.Tasks.Task UpdateTaskAsync(string taskListId, string taskId, string title, string? notes, DateTime? due, string? status = null, CancellationToken ct = default)
    {
        var service = await GetTasksClientAsync(ct);
        if (service == null) return;

        var task = await service.Tasks.Get(taskListId, taskId).ExecuteAsync(ct);
        if (task == null) return;

        task.Title = title;
        task.Notes = notes;
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
