using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Models;
using GOpsHub.Application.Features.DriveGuard;
using GOpsHub.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOpsHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DriveGuardController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public DriveGuardController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// List Drive audit logs (UC05)
    /// </summary>
    [HttpGet("audit-logs")]
    public async Task<ActionResult<ApiResponse<PagedResult<DriveAuditLog>>>> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _dispatcher.QueryAsync(new GetAuditLogsQuery(page, pageSize));
        return Ok(ApiResponse<PagedResult<DriveAuditLog>>.Ok(result));
    }

    /// <summary>
    /// List security alerts (UC06)
    /// </summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<ApiResponse<PagedResult<SecurityAlert>>>> GetSecurityAlerts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _dispatcher.QueryAsync(new GetSecurityAlertsQuery(page, pageSize));
        return Ok(ApiResponse<PagedResult<SecurityAlert>>.Ok(result));
    }

    /// <summary>
    /// List folders being monitored (UC05)
    /// </summary>
    [HttpGet("folders")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MonitoredFolder>>>> GetMonitoredFolders()
    {
        var folders = await _dispatcher.QueryAsync(new GetMonitoredFoldersQuery());
        return Ok(ApiResponse<IReadOnlyList<MonitoredFolder>>.Ok(folders));
    }

    /// <summary>
    /// Add folder to monitor (UC05)
    /// </summary>
    [HttpPost("folders")]
    public async Task<ActionResult<ApiResponse<MonitoredFolder>>> AddMonitoredFolder([FromBody] AddMonitoredFolderCommand command)
    {
        var folder = await _dispatcher.SendAsync(command);
        return Ok(ApiResponse<MonitoredFolder>.Ok(folder, "Đã thêm thư mục vào danh sách theo dõi."));
    }

    /// <summary>
    /// Quarantine dangerous file (UC06)
    /// </summary>
    [HttpPost("quarantine")]
    public async Task<ActionResult<ApiResponse<bool>>> QuarantineFile([FromBody] QuarantineFileCommand command)
    {
        var result = await _dispatcher.SendAsync(command);
        return Ok(ApiResponse<bool>.Ok(result, "Đã cách ly file nguy hiểm."));
    }
}
