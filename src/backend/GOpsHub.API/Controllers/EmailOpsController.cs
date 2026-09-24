using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Models;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.EmailOps.Commands;
using GOpsHub.Application.Features.EmailOps.Queries;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOpsHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class EmailOpsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;
    private readonly IGmailService _gmailService;
    private readonly IAIService _aiService;

    public EmailOpsController(IDispatcher dispatcher, IGmailService gmailService, IAIService aiService)
    {
        _dispatcher = dispatcher;
        _gmailService = gmailService;
        _aiService = aiService;
    }

    /// <summary>
    /// Get recent emails from Inbox
    /// </summary>
    [HttpGet("inbox")]
    public async Task<ActionResult<ApiResponse<object>>> GetInbox([FromQuery] bool isRead = false, [FromQuery] int maxResults = 10, [FromQuery] string? pageToken = null, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var query = !string.IsNullOrWhiteSpace(search) 
            ? search 
            : (isRead ? "in:inbox is:read" : "in:inbox is:unread");
        var (emails, nextToken) = await _gmailService.GetPagedEmailsAsync(query, maxResults, pageToken, ct);
        return Ok(ApiResponse<object>.Ok(new { Items = emails, NextPageToken = nextToken }));
    }

    [HttpPost("{id}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(string id, CancellationToken ct)
    {
        await _gmailService.MarkAsReadAsync(id, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã đánh dấu đã đọc."));
    }

    [HttpPost("{id}/unread")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsUnread(string id, CancellationToken ct)
    {
        await _gmailService.MarkAsUnreadAsync(id, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã đánh dấu chưa đọc."));
    }

    [HttpPost("{id}/star")]
    public async Task<ActionResult<ApiResponse<bool>>> StarEmail(string id, CancellationToken ct)
    {
        await _gmailService.StarEmailAsync(id, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã gắn sao email."));
    }

    [HttpPost("{id}/unstar")]
    public async Task<ActionResult<ApiResponse<bool>>> UnstarEmail(string id, CancellationToken ct)
    {
        await _gmailService.UnstarEmailAsync(id, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã bỏ gắn sao email."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> TrashEmail(string id, CancellationToken ct)
    {
        await _gmailService.TrashEmailAsync(id, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã chuyển email vào thùng rác."));
    }

    [HttpPost("{id}/reply")]
    public async Task<ActionResult<ApiResponse<bool>>> ReplyToEmail(string id, [FromBody] ReplyEmailRequest request, CancellationToken ct)
    {
        var email = await _gmailService.GetEmailByIdAsync(id, ct);
        if (email == null) return NotFound(ApiResponse<bool>.Fail("Không tìm thấy email."));

        var draftId = await _gmailService.CreateDraftAsync(email.From, $"Re: {email.Subject}", request.Body, email.ThreadId, null, null, ct);
        await _gmailService.SendDraftAsync(draftId, ct);
        
        return Ok(ApiResponse<bool>.Ok(true, "Đã gửi phản hồi."));
    }

    [HttpPost("{id}/draft-ai")]
    public async Task<ActionResult<ApiResponse<string>>> DraftAiReply(string id, CancellationToken ct)
    {
        var email = await _gmailService.GetEmailByIdAsync(id, ct);
        if (email == null) return NotFound(ApiResponse<string>.Fail("Không tìm thấy email."));

        var aiResult = await _aiService.GenerateEmailReplyAsync(email.Snippet ?? email.Body ?? "", "vi", null, ct);
        return Ok(ApiResponse<string>.Ok(aiResult.DraftContent, "Đã tạo nháp AI."));
    }

    [HttpPost("send")]
    public async Task<ActionResult<ApiResponse<bool>>> SendEmail([FromBody] SendEmailRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.To))
            return BadRequest(ApiResponse<bool>.Fail("Địa chỉ người nhận không được để trống."));

        var draftId = await _gmailService.CreateDraftAsync(request.To, request.Subject ?? string.Empty, request.Body ?? string.Empty, null, request.Cc, request.Bcc, ct);
        await _gmailService.SendDraftAsync(draftId, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Đã gửi email thành công."));
    }

    [HttpPost("compose-ai")]
    public async Task<ActionResult<ApiResponse<AIReplyResult>>> ComposeAiDraft([FromBody] ComposeAiRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(ApiResponse<AIReplyResult>.Fail("Prompt yêu cầu soạn thảo không được để trống."));

        var promptBuilder = $"Yêu cầu soạn thảo email mới bằng tiếng Việt: {request.Prompt}. Người nhận: {request.RecipientHint ?? "Đối tác/Đồng nghiệp"}. Hãy sinh nội dung hoàn chỉnh và tiêu đề phù hợp.";
        var aiResult = await _aiService.GenerateEmailReplyAsync(promptBuilder, "vi", null, ct);
        return Ok(ApiResponse<AIReplyResult>.Ok(aiResult, "AI đã soạn thảo nội dung thành công."));
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
    /// Submit user deletion feedback to teach AI and trash email immediately
    /// </summary>
    [HttpPost("cleanup/feedback")]
    public async Task<ActionResult<ApiResponse<CleanupFeedback>>> SubmitCleanupFeedback([FromBody] SubmitCleanupFeedbackCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.EmailId))
            return BadRequest(ApiResponse<CleanupFeedback>.Fail("EmailId không được để trống."));

        if (string.IsNullOrWhiteSpace(command.Reason) && (command.Tags == null || !command.Tags.Any()))
            return BadRequest(ApiResponse<CleanupFeedback>.Fail("Vui lòng cung cấp ít nhất một lý do hoặc tag xóa email."));

        var result = await _dispatcher.SendAsync(command, ct);
        return Ok(ApiResponse<CleanupFeedback>.Ok(result, "Đã xóa email và lưu mẫu huấn luyện AI thành công."));
    }

    /// <summary>
    /// Get recent user cleanup feedbacks
    /// </summary>
    [HttpGet("cleanup/feedback")]
    public async Task<ActionResult<ApiResponse<List<CleanupFeedback>>>> GetCleanupFeedbacks([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var result = await _dispatcher.QueryAsync(new GetCleanupFeedbacksQuery(limit), ct);
        return Ok(ApiResponse<List<CleanupFeedback>>.Ok(result));
    }

    /// <summary>
    /// Delete a user cleanup feedback entry
    /// </summary>
    [HttpDelete("cleanup/feedback/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCleanupFeedback(string id, CancellationToken ct = default)
    {
        var result = await _dispatcher.SendAsync(new DeleteCleanupFeedbackCommand(id), ct);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Không tìm thấy mẫu phản hồi để xóa."));

        return Ok(ApiResponse<bool>.Ok(true, "Đã xóa mẫu phản hồi dạy AI."));
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
    /// Get detailed email action audit logs (UC01 / Telemetry)
    /// </summary>
    [HttpGet("action-logs")]
    public async Task<ActionResult<ApiResponse<PagedResult<EmailActionLog>>>> GetEmailActionLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? action = null,
        [FromQuery] string? sourceJob = null,
        [FromQuery] string? search = null)
    {
        var logs = await _dispatcher.QueryAsync(new GetEmailActionLogsQuery(page, pageSize, action, sourceJob, search));
        return Ok(ApiResponse<PagedResult<EmailActionLog>>.Ok(logs));
    }

    /// <summary>
    /// Delete a specific action log entry
    /// </summary>
    [HttpDelete("action-logs/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteEmailActionLog(string id)
    {
        var result = await _dispatcher.SendAsync(new DeleteEmailActionLogCommand(id));
        return Ok(ApiResponse<bool>.Ok(result, "Đã xóa bản ghi nhật ký."));
    }

    /// <summary>
    /// Batch delete action logs (by IDs, all, or older than N days)
    /// </summary>
    [HttpPost("action-logs/delete-batch")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteBatchEmailActionLogs([FromBody] DeleteBatchActionLogsRequest request)
    {
        var result = await _dispatcher.SendAsync(new DeleteBatchEmailActionLogsCommand(
            request.Ids,
            request.DeleteAll,
            request.OlderThanDays,
            request.Action));
        return Ok(ApiResponse<bool>.Ok(result, "Đã xóa các bản ghi nhật ký được chọn."));
    }

    /// <summary>
    /// Approve an uncertain email action awaiting human confirmation (Trash or Archive)
    /// </summary>
    [HttpPost("action-logs/{id}/approve")]
    public async Task<ActionResult<ApiResponse<EmailActionLog>>> ApproveEmailAction(string id, [FromBody] ApproveEmailActionRequest request)
    {
        var updatedLog = await _dispatcher.SendAsync(new ApproveEmailActionCommand(id, request.Action ?? "Trash"));
        return Ok(ApiResponse<EmailActionLog>.Ok(updatedLog, "Đã phê duyệt và thực thi dọn dẹp email."));
    }

    /// <summary>
    /// Dismiss an uncertain email action awaiting human confirmation
    /// </summary>
    [HttpPost("action-logs/{id}/reject")]
    public async Task<ActionResult<ApiResponse<EmailActionLog>>> DismissEmailAction(string id)
    {
        var updatedLog = await _dispatcher.SendAsync(new DismissEmailActionCommand(id));
        return Ok(ApiResponse<EmailActionLog>.Ok(updatedLog, "Đã bỏ qua email khỏi danh sách chờ duyệt."));
    }

    /// <summary>
    /// Get all pending uncertain email actions awaiting human confirmation
    /// </summary>
    [HttpGet("action-logs/pending")]
    public async Task<ActionResult<ApiResponse<PagedResult<EmailActionLog>>>> GetPendingEmailActionLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var logs = await _dispatcher.QueryAsync(new GetEmailActionLogsQuery(page, pageSize, "PendingApproval", null, search));
        return Ok(ApiResponse<PagedResult<EmailActionLog>>.Ok(logs));
    }

    /// <summary>
    /// Batch approve or dismiss uncertain email actions awaiting human confirmation
    /// </summary>
    [HttpPost("action-logs/pending/batch-action")]
    public async Task<ActionResult<ApiResponse<BatchPendingEmailActionResult>>> BatchPendingEmailActions([FromBody] BatchPendingEmailActionCommand command)
    {
        var result = await _dispatcher.SendAsync(command);
        return Ok(ApiResponse<BatchPendingEmailActionResult>.Ok(result, $"Đã xử lý {result.SuccessCount}/{result.TotalRequested} email chờ duyệt."));
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

    /// <summary>
    /// Update an existing cleanup rule
    /// </summary>
    [HttpPut("rules/{id}")]
    public async Task<ActionResult<ApiResponse<CleanupRule>>> UpdateCleanupRule(string id, [FromBody] UpdateCleanupRuleRequest request)
    {
        var command = new UpdateCleanupRuleCommand(
            id,
            request.RuleName,
            request.Action,
            request.WhitelistDomains,
            request.CustomQuery,
            request.UseAI,
            request.AIPrompt,
            request.SubjectRegex,
            request.BodyRegex);
        var rule = await _dispatcher.SendAsync(command);
        return Ok(ApiResponse<CleanupRule>.Ok(rule, "Đã cập nhật quy tắc."));
    }

    /// <summary>
    /// Delete a cleanup rule
    /// </summary>
    [HttpDelete("rules/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCleanupRule(string id)
    {
        var result = await _dispatcher.SendAsync(new DeleteCleanupRuleCommand(id));
        return Ok(ApiResponse<bool>.Ok(result, "Đã xóa quy tắc."));
    }

    /// <summary>
    /// Toggle a cleanup rule active/inactive
    /// </summary>
    [HttpPatch("rules/{id}/toggle")]
    public async Task<ActionResult<ApiResponse<CleanupRule>>> ToggleCleanupRule(string id)
    {
        var rule = await _dispatcher.SendAsync(new ToggleCleanupRuleCommand(id));
        return Ok(ApiResponse<CleanupRule>.Ok(rule, rule.IsActive ? "Đã kích hoạt quy tắc." : "Đã vô hiệu hóa quy tắc."));
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

public class UpdateCleanupRuleRequest
{
    public string RuleName { get; set; } = string.Empty;
    public CleanupAction Action { get; set; }
    public List<string> WhitelistDomains { get; set; } = new();
    public string? CustomQuery { get; set; }
    public bool UseAI { get; set; }
    public string? AIPrompt { get; set; }
    public string? SubjectRegex { get; set; }
    public string? BodyRegex { get; set; }
}

public class ReplyEmailRequest
{
    public string Body { get; set; } = string.Empty;
}

public class SendEmailRequest
{
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class ComposeAiRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string? RecipientHint { get; set; }
}

public class DeleteBatchActionLogsRequest
{
    public List<string>? Ids { get; set; }
    public bool DeleteAll { get; set; }
    public int? OlderThanDays { get; set; }
    public string? Action { get; set; }
}

public class ApproveEmailActionRequest
{
    public string? Action { get; set; } = "Trash";
}
