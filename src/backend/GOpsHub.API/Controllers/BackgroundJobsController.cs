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

[ApiController]
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
}
