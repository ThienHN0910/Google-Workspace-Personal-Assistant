using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Common.Models;
using GOpsHub.Application.Features.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOpsHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;
    private readonly IAiUsageTracker _aiUsageTracker;

    public SettingsController(IDispatcher dispatcher, IAiUsageTracker aiUsageTracker)
    {
        _dispatcher = dispatcher;
        _aiUsageTracker = aiUsageTracker;
    }

    /// <summary>
    /// Get unified system settings
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<SystemSettingsDto>>> GetSettings()
    {
        var settings = await _dispatcher.QueryAsync(new GetSystemSettingsQuery());
        return Ok(ApiResponse<SystemSettingsDto>.Ok(settings));
    }

    /// <summary>
    /// Update unified system settings and dynamically reschedule background jobs
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateSettings([FromBody] SystemSettingsDto settings)
    {
        var result = await _dispatcher.SendAsync(new UpdateSystemSettingsCommand(settings));
        return Ok(ApiResponse<bool>.Ok(result, "Đã lưu cài đặt hệ thống và cập nhật lịch chạy tác vụ ngầm thành công."));
    }

    /// <summary>
    /// Test Telegram bot connection
    /// </summary>
    [HttpPost("test-telegram")]
    public async Task<ActionResult<ApiResponse<bool>>> TestTelegram([FromBody] TestTelegramRequest request)
    {
        var result = await _dispatcher.SendAsync(new TestTelegramConnectionCommand(request.BotToken, request.ChatId));
        return Ok(ApiResponse<bool>.Ok(result, "Đã gửi tin nhắn thử nghiệm thành công tới Telegram Bot của bạn!"));
    }

    /// <summary>
    /// Get current monthly AI token usage and quota info
    /// </summary>
    [HttpGet("ai-usage")]
    public async Task<ActionResult<ApiResponse<AiUsageDto>>> GetAiUsage(CancellationToken ct)
    {
        var usage = await _aiUsageTracker.GetCurrentMonthlyUsageAsync(ct);
        var remaining = await _aiUsageTracker.GetRemainingTokensAsync(ct);
        var canRun = await _aiUsageTracker.CanRunBackgroundAiAsync(ct);

        var dto = new AiUsageDto
        {
            YearMonth = usage.YearMonth,
            TotalTokens = usage.TotalTokens,
            PromptTokens = usage.PromptTokens,
            CandidatesTokens = usage.CandidatesTokens,
            FeatureBreakdown = usage.FeatureBreakdown,
            CallCount = usage.CallCount,
            MonthlyQuotaLimit = usage.MonthlyQuotaLimit,
            WarningThreshold = usage.WarningThreshold,
            WarningSent = usage.WarningSent,
            QuotaExceeded = usage.QuotaExceededSent,
            RemainingTokens = remaining,
            CanRunBackgroundAi = canRun,
            UsagePercentage = usage.MonthlyQuotaLimit > 0
                ? Math.Round((double)usage.TotalTokens / usage.MonthlyQuotaLimit * 100, 1)
                : 0
        };

        return Ok(ApiResponse<AiUsageDto>.Ok(dto));
    }
}

public class TestTelegramRequest
{
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
}

public class AiUsageDto
{
    public string YearMonth { get; set; } = string.Empty;
    public long TotalTokens { get; set; }
    public long PromptTokens { get; set; }
    public long CandidatesTokens { get; set; }
    public Dictionary<string, long> FeatureBreakdown { get; set; } = new();
    public int CallCount { get; set; }
    public long MonthlyQuotaLimit { get; set; }
    public long WarningThreshold { get; set; }
    public bool WarningSent { get; set; }
    public bool QuotaExceeded { get; set; }
    public long RemainingTokens { get; set; }
    public bool CanRunBackgroundAi { get; set; }
    public double UsagePercentage { get; set; }
}
