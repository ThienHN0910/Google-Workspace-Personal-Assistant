using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Common.Models;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOpsHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IRepository<AppConfiguration>? _configRepo;

    public PublicController(
        IConfiguration configuration,
        IRecurringJobManager recurringJobManager,
        IRepository<AppConfiguration>? configRepo = null)
    {
        _configuration = configuration;
        _recurringJobManager = recurringJobManager;
        _configRepo = configRepo;
    }

    /// <summary>
    /// System health check
    /// </summary>
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();

        return Ok(ApiResponse<object>.Ok(new
        {
            Status = "Healthy",
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow,
            Uptime = $"{uptime.Days}d {uptime.Hours:D2}h {uptime.Minutes:D2}m {uptime.Seconds:D2}s"
        }));
    }

    /// <summary>
    /// Keep-Alive anti-sleep heartbeat endpoint for free-tier IIS / MonsterASP
    /// Can be pinged every 10-14 minutes via Cron-job.org, UptimeRobot, or GitHub Actions
    /// </summary>
    [HttpGet("keep-alive")]
    public async Task<IActionResult> KeepAlive(
        [FromHeader(Name = "X-KeepAlive-Key")] string? headerKey,
        [FromQuery] string? key,
        [FromQuery] string? trigger,
        CancellationToken ct)
    {
        // 1. Validate Secret Key if configured
        var configuredKey = _configuration["KEEP_ALIVE_KEY"]
            ?? _configuration["KeepAlive:Key"];

        if (_configRepo != null && string.IsNullOrWhiteSpace(configuredKey))
        {
            var conf = await _configRepo.FindOneAsync(c => c.Key == "KeepAliveKey", ct);
            if (!string.IsNullOrWhiteSpace(conf?.Value))
                configuredKey = conf.Value;
        }

        var providedKey = headerKey ?? key;

        if (!string.IsNullOrEmpty(configuredKey) && !string.Equals(configuredKey, providedKey, StringComparison.Ordinal))
        {
            return StatusCode(401, ApiResponse<object>.Fail("Mã xác thực Keep-Alive không hợp lệ (Unauthorized)."));
        }

        // 2. Optional Manual Trigger for Background Jobs
        string? triggeredJobMessage = null;
        if (!string.IsNullOrWhiteSpace(trigger))
        {
            try
            {
                if (trigger.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    _recurringJobManager.Trigger("drive-guard-audit");
                    _recurringJobManager.Trigger("bank-telemetry");
                    _recurringJobManager.Trigger("email-cleanup");
                    _recurringJobManager.Trigger("calendar-extractor");
                    triggeredJobMessage = "Đã kích hoạt toàn bộ 4 tác vụ ngầm (all).";
                }
                else
                {
                    _recurringJobManager.Trigger(trigger);
                    triggeredJobMessage = $"Đã kích hoạt tác vụ ngầm '{trigger}'.";
                }
            }
            catch (Exception ex)
            {
                triggeredJobMessage = $"Lỗi khi kích hoạt '{trigger}': {ex.Message}";
            }
        }

        // 3. Hangfire Server Status Inspection
        int recurringJobsCount = 0;
        int activeServersCount = 0;
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            recurringJobsCount = connection.GetRecurringJobs().Count;
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            activeServersCount = monitoringApi.Servers().Count;
        }
        catch
        {
            // fallback if storage access has transient delay
        }

        var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();

        return Ok(ApiResponse<object>.Ok(new
        {
            Status = "Alive",
            Server = "G-Ops Hub Engine (MonsterASP Anti-Sleep)",
            ServerTimeUtc = DateTime.UtcNow,
            Uptime = $"{uptime.Days}d {uptime.Hours:D2}h {uptime.Minutes:D2}m {uptime.Seconds:D2}s",
            MemoryUsedMb = Math.Round((double)GC.GetTotalMemory(false) / (1024 * 1024), 2),
            Hangfire = new
            {
                Status = activeServersCount > 0 ? "Active" : "Standby",
                ActiveServers = activeServersCount,
                RecurringJobs = recurringJobsCount
            },
            TriggeredJob = triggeredJobMessage,
            Message = "IIS Application Pool được duy trì hoạt động thành công (Anti-Sleep Active)."
        }));
    }

    [HttpGet("calendar-status")]
    public async Task<IActionResult> GetPublicCalendarStatus(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromServices] ICalendarService calendarService,
        CancellationToken ct)
    {
        DateTime minDate = startDate ?? DateTime.UtcNow.AddDays(-30);
        DateTime maxDate = endDate ?? DateTime.UtcNow.AddDays(30);

        var upcomingEvents = await calendarService.GetEventsAsync(minDate, maxDate, "primary", ct);
        var maskedEvents = new List<object>();

        foreach (var ev in upcomingEvents)
        {
            if (ev.Visibility.Equals("public", StringComparison.OrdinalIgnoreCase))
            {
                maskedEvents.Add(new
                {
                    Title = ev.Title,
                    Start = ev.Start,
                    End = ev.End,
                    Location = ev.Location,
                    IsPublic = true
                });
            }
            else
            {
                maskedEvents.Add(new
                {
                    Title = "Lịch bận",
                    Start = ev.Start,
                    End = ev.End,
                    Location = (string?)null,
                    IsPublic = false
                });
            }
        }

        var isBusyNow = upcomingEvents.Any(e => e.Start <= DateTime.UtcNow && (e.End == null || e.End >= DateTime.UtcNow));

        return Ok(ApiResponse<object>.Ok(new
        {
            IsBusyNow = isBusyNow,
            Events = maskedEvents,
            Message = "Lịch làm việc cá nhân của Thien HN"
        }));
    }
}
