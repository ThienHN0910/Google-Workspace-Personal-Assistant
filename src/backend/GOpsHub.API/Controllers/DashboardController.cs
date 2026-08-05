using GOpsHub.Application.Common.Models;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOpsHub.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IRepository<CleanupLog> _cleanupLogRepo;
    private readonly IRepository<AIDraft> _draftRepo;
    private readonly IRepository<Transaction> _transactionRepo;
    private readonly IRepository<SecurityAlert> _alertRepo;
    private readonly IRepository<ExtractedSchedule> _scheduleRepo;

    public DashboardController(
        IRepository<CleanupLog> cleanupLogRepo,
        IRepository<AIDraft> draftRepo,
        IRepository<Transaction> transactionRepo,
        IRepository<SecurityAlert> alertRepo,
        IRepository<ExtractedSchedule> scheduleRepo)
    {
        _cleanupLogRepo = cleanupLogRepo;
        _draftRepo = draftRepo;
        _transactionRepo = transactionRepo;
        _alertRepo = alertRepo;
        _scheduleRepo = scheduleRepo;
    }

    /// <summary>
    /// Summary cards for Dashboard view
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;

        var cleanupLogs = await _cleanupLogRepo.FindAsync(x => x.ExecutedAt >= today, ct);
        var cleanedToday = cleanupLogs.Sum(x => x.TotalTrashed + x.TotalArchived);

        var pendingDrafts = await _draftRepo.CountAsync(x => x.Status == Domain.Enums.DraftStatus.Pending, ct);

        var pendingSchedules = await _scheduleRepo.CountAsync(x => x.Status == Domain.Enums.ExtractedScheduleStatus.Pending, ct);

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthTransactions = await _transactionRepo.FindAsync(x => x.TransactionDate >= monthStart, ct);
        var totalIncome = monthTransactions.Where(x => x.TransactionType == Domain.Enums.TransactionType.Credit).Sum(x => x.Amount);
        var totalExpense = monthTransactions.Where(x => x.TransactionType == Domain.Enums.TransactionType.Debit).Sum(x => x.Amount);

        var activeAlerts = await _alertRepo.CountAsync(x => !x.IsResolved, ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            CleanedToday = cleanedToday,
            PendingDrafts = pendingDrafts,
            PendingSchedules = pendingSchedules,
            MonthlyIncome = totalIncome,
            MonthlyExpense = totalExpense,
            MonthlyNetBalance = totalIncome - totalExpense,
            ActiveAlerts = activeAlerts
        }));
    }
}
