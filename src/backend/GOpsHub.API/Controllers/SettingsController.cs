using GOpsHub.Application.Common.CQRS;
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

    public SettingsController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
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
}

public class TestTelegramRequest
{
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
}
