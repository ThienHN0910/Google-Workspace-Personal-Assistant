using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Models;
using GOpsHub.Application.Features.Finance;
using GOpsHub.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOpsHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public FinanceController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// List logged financial transactions (UC04)
    /// </summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<ApiResponse<PagedResult<Transaction>>>> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _dispatcher.QueryAsync(new GetTransactionsQuery(page, pageSize));
        return Ok(ApiResponse<PagedResult<Transaction>>.Ok(result));
    }

    /// <summary>
    /// Get pending unread bank emails to be parsed
    /// </summary>
    [HttpGet("transactions/pending")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EmailMessage>>>> GetPendingBankEmails([FromQuery] string domain = "vpb.com.vn")
    {
        var result = await _dispatcher.QueryAsync(new GetPendingBankEmailsQuery(domain));
        return Ok(ApiResponse<IReadOnlyList<EmailMessage>>.Ok(result));
    }

    /// <summary>
    /// Parse financial transaction from bank email and sync to Google Sheets (UC04)
    /// </summary>
    [HttpPost("transactions/parse")]
    public async Task<ActionResult<ApiResponse<Transaction>>> ParseTransaction([FromBody] ParseTransactionEmailCommand command)
    {
        var transaction = await _dispatcher.SendAsync(command);
        if (transaction == null)
            return BadRequest(ApiResponse<object>.Fail("Không thể phân tích biến động số dư từ email này."));

        return Ok(ApiResponse<Transaction>.Ok(transaction, "Đã ghi nhận giao dịch tài chính và đồng bộ Google Sheets."));
    }
}
