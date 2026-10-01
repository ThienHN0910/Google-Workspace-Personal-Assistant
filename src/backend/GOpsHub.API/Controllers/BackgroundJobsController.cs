using Hangfire;
using Hangfire.Storage;
using GOpsHub.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOpsHub.API.Controllers;

public class JobInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public DateTime? NextExecution { get; set; }
    public DateTime? LastExecution { get; set; }
    public string LastJobState { get; set; } = string.Empty;
}

public class JobExecutionHistoryDto
{
    public string JobId { get; set; } = string.Empty;
    public string JobKey { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string State { get; set; } = "Succeeded"; // Succeeded, Failed, Processing
    public DateTime? ExecutedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExceptionDetails { get; set; }
}

[ApiController]
[Route("api/v1/jobs")]
[Route("api/v1/[controller]")]
[Authorize]
public class BackgroundJobsController : ControllerBase
{
    private readonly IRecurringJobManager _recurringJobManager;

    public BackgroundJobsController(IRecurringJobManager recurringJobManager)
    {
        _recurringJobManager = recurringJobManager;
    }

    /// <summary>
    /// Get status and schedule of all active background recurring jobs
    /// </summary>
    [HttpGet]
    public ActionResult<ApiResponse<List<JobInfoDto>>> GetJobs()
    {
        var jobDescriptions = new Dictionary<string, (string Name, string Description)>
        {
            ["drive-guard-audit"] = ("Drive Guard Audit (UC05 & UC06)", "Quét biến động file Google Drive, phát hiện xóa hàng loạt và cảnh báo file nguy hiểm."),
            ["email-cleanup"] = ("Tự động dọn dẹp Inbox (UC01)", "Quét dọn thư rác, quảng cáo cũ theo quy tắc đã định, bảo vệ thư unread và gắn sao."),
            ["bank-telemetry"] = ("Đồng bộ Biến động số dư Ngân hàng (UC04)", "Quét email ngân hàng, bóc tách AI giao dịch và tự động ghi vào Google Sheets."),
            ["calendar-extractor"] = ("Trích xuất Lịch hẹn thông minh (UC03)", "Quét email mới tìm kiếm lịch hẹn/phỏng vấn và tạo danh sách chờ duyệt.")
        };

        var result = new List<JobInfoDto>();
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var recurringJobs = connection.GetRecurringJobs();

            foreach (var rj in recurringJobs)
            {
                var (name, desc) = jobDescriptions.TryGetValue(rj.Id, out var meta)
                    ? meta
                    : (rj.Id, "Tác vụ chạy ngầm hệ thống");

                result.Add(new JobInfoDto
                {
                    Id = rj.Id,
                    Name = name,
                    Description = desc,
                    Cron = rj.Cron,
                    NextExecution = rj.NextExecution,
                    LastExecution = rj.LastExecution,
                    LastJobState = rj.LastJobState ?? "Scheduled"
                });
            }
        }
        catch (Exception)
        {
            // Fallback default list if storage connection has delayed initialization
            foreach (var kvp in jobDescriptions)
            {
                result.Add(new JobInfoDto
                {
                    Id = kvp.Key,
                    Name = kvp.Value.Name,
                    Description = kvp.Value.Description,
                    Cron = "Active",
                    LastJobState = "Active"
                });
            }
        }

        return Ok(ApiResponse<List<JobInfoDto>>.Ok(result));
    }

    /// <summary>
    /// Get recent execution history for all background jobs (Succeeded, Failed, Processing)
    /// </summary>
    [HttpGet("history")]
    public ActionResult<ApiResponse<List<JobExecutionHistoryDto>>> GetHistory([FromQuery] int limit = 50)
    {
        var history = new List<JobExecutionHistoryDto>();
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            if (monitoringApi != null)
            {
                // 1. Succeeded jobs
                var succeeded = monitoringApi.SucceededJobs(0, limit);
                foreach (var sj in succeeded)
                {
                    var (key, name) = ResolveJobMetadata(sj.Value?.Job);
                    history.Add(new JobExecutionHistoryDto
                    {
                        JobId = sj.Key,
                        JobKey = key,
                        JobName = name,
                        State = "Succeeded",
                        ExecutedAt = sj.Value?.SucceededAt,
                        DurationMs = sj.Value?.TotalDuration,
                        ErrorMessage = null,
                        ExceptionDetails = null
                    });
                }

                // 2. Failed jobs
                var failed = monitoringApi.FailedJobs(0, limit);
                foreach (var fj in failed)
                {
                    var (key, name) = ResolveJobMetadata(fj.Value?.Job);
                    history.Add(new JobExecutionHistoryDto
                    {
                        JobId = fj.Key,
                        JobKey = key,
                        JobName = name,
                        State = "Failed",
                        ExecutedAt = fj.Value?.FailedAt,
                        DurationMs = null,
                        ErrorMessage = fj.Value?.ExceptionMessage,
                        ExceptionDetails = fj.Value?.ExceptionDetails
                    });
                }

                // 3. Processing jobs
                var processing = monitoringApi.ProcessingJobs(0, 10);
                foreach (var pj in processing)
                {
                    var (key, name) = ResolveJobMetadata(pj.Value?.Job);
                    history.Add(new JobExecutionHistoryDto
                    {
                        JobId = pj.Key,
                        JobKey = key,
                        JobName = name,
                        State = "Processing",
                        ExecutedAt = pj.Value?.StartedAt,
                        DurationMs = null,
                        ErrorMessage = null,
                        ExceptionDetails = null
                    });
                }
            }
        }
        catch (Exception)
        {
            // Graceful fallback when JobStorage is uninitialized (e.g. testing or storage startup delay)
        }

        var sorted = history
            .OrderByDescending(h => h.ExecutedAt ?? DateTime.MinValue)
            .Take(limit)
            .ToList();

        return Ok(ApiResponse<List<JobExecutionHistoryDto>>.Ok(sorted));
    }

    /// <summary>
    /// Trigger an immediate run for a specific background job
    /// </summary>
    [HttpPost("{id}/trigger")]
    public ActionResult<ApiResponse<bool>> TriggerJob(string id)
    {
        try
        {
            _recurringJobManager.Trigger(id);
            return Ok(ApiResponse<bool>.Ok(true, $"Đã gửi lệnh kích hoạt tác vụ chạy ngầm '{id}' thành công."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.Fail($"Không thể kích hoạt tác vụ '{id}': {ex.Message}"));
        }
    }

    private static (string JobKey, string JobName) ResolveJobMetadata(Hangfire.Common.Job? job)
    {
        if (job == null || job.Type == null)
        {
            return ("system-job", "Tác vụ chạy ngầm hệ thống");
        }

        var typeName = job.Type.Name;
        if (typeName.Contains("DriveGuard", StringComparison.OrdinalIgnoreCase))
        {
            return ("drive-guard-audit", "Drive Guard Audit (UC05 & UC06)");
        }
        if (typeName.Contains("EmailCleanup", StringComparison.OrdinalIgnoreCase))
        {
            return ("email-cleanup", "Tự động dọn dẹp Inbox (UC01)");
        }
        if (typeName.Contains("BankTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            return ("bank-telemetry", "Đồng bộ Biến động số dư Ngân hàng (UC04)");
        }
        if (typeName.Contains("CalendarSchedule", StringComparison.OrdinalIgnoreCase))
        {
            return ("calendar-extractor", "Trích xuất Lịch hẹn thông minh (UC03)");
        }

        return (typeName.ToLowerInvariant(), $"{typeName}.{job.Method?.Name ?? "Execute"}");
    }
}
