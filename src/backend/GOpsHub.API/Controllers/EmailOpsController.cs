using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Models;
using GOpsHub.Application.Features.EmailOps.Commands;
using GOpsHub.Application.Features.EmailOps.Queries;
using GOpsHub.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOpsHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class EmailOpsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public EmailOpsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// List all cleanup rules
    /// </summary>
    [HttpGet("rules")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CleanupRule>>>> GetCleanupRules()
    {
        var rules = await _dispatcher.QueryAsync(new GetCleanupRulesQuery());
        return Ok(ApiResponse<IReadOnlyList<CleanupRule>>.Ok(rules));
    }

    /// <summary>
    /// Create a new cleanup rule (UC01)
    /// </summary>
    [HttpPost("rules")]
    public async Task<ActionResult<ApiResponse<CleanupRule>>> CreateCleanupRule([FromBody] CreateCleanupRuleCommand command)
    {
        var rule = await _dispatcher.SendAsync(command);
        return Ok(ApiResponse<CleanupRule>.Ok(rule, "Đã tạo quy tắc dọn email mới."));
    }

    /// <summary>
    /// Trigger manual email cleanup (UC01)
    /// </summary>
    [HttpPost("rules/run")]
    public async Task<ActionResult<ApiResponse<CleanupLogResult>>> RunCleanup([FromBody] RunCleanupCommand command)
    {
        var result = await _dispatcher.SendAsync(command);
        return Ok(ApiResponse<CleanupLogResult>.Ok(result, "Đã thực thi quy tắc dọn dẹp inbox."));
    }

    /// <summary>
    /// Get cleanup execution history
    /// </summary>
    [HttpGet("logs")]
    public async Task<ActionResult<ApiResponse<PagedResult<CleanupLog>>>> GetCleanupLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var logs = await _dispatcher.QueryAsync(new GetCleanupLogsQuery(page, pageSize));
        return Ok(ApiResponse<PagedResult<CleanupLog>>.Ok(logs));
    }

    /// <summary>
    /// Get pending AI drafts awaiting human approval (UC02)
    /// </summary>
    [HttpGet("drafts/pending")]
    public async Task<ActionResult<ApiResponse<PagedResult<AIDraft>>>> GetPendingDrafts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var drafts = await _dispatcher.QueryAsync(new GetPendingDraftsQuery(page, pageSize));
        return Ok(ApiResponse<PagedResult<AIDraft>>.Ok(drafts));
    }

    /// <summary>
    /// Approve an AI draft (UC02)
    /// </summary>
    [HttpPost("drafts/{id}/approve")]
    public async Task<ActionResult<ApiResponse<AIDraft>>> ApproveDraft(string id, [FromBody] ApproveDraftRequest request)
    {
        var draft = await _dispatcher.SendAsync(new ApproveDraftCommand(id, request.CustomContent));
        return Ok(ApiResponse<AIDraft>.Ok(draft, "Đã phê duyệt và tạo bản nháp phản hồi."));
    }

    /// <summary>
    /// Reject an AI draft (UC02)
    /// </summary>
    [HttpPost("drafts/{id}/reject")]
    public async Task<ActionResult<ApiResponse<AIDraft>>> RejectDraft(string id, [FromBody] RejectDraftRequest request)
    {
        var draft = await _dispatcher.SendAsync(new RejectDraftCommand(id, request.Reason));
        return Ok(ApiResponse<AIDraft>.Ok(draft, "Đã từ chối bản nháp phản hồi."));
    }

    /// <summary>
    /// Generate AI draft for a specific email
    /// </summary>
    [HttpPost("drafts/generate")]
    public async Task<ActionResult<ApiResponse<AIDraft>>> GenerateDraft([FromBody] GenerateAIDraftCommand command)
    {
        var draft = await _dispatcher.SendAsync(command);
        return Ok(ApiResponse<AIDraft>.Ok(draft, "Đã tạo AI draft thành công."));
    }
}

public class ApproveDraftRequest
{
    public string? CustomContent { get; set; }
}

public class RejectDraftRequest
{
    public string Reason { get; set; } = string.Empty;
}
