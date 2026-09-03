using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Application.Features.Finance;

public class BankTelemetryBackgroundJob
{
    private readonly IDispatcher _dispatcher;
    private readonly INotificationService _notificationService;
    private readonly IRepository<AppConfiguration> _configRepo;
    private readonly ILogger<BankTelemetryBackgroundJob> _logger;

    public BankTelemetryBackgroundJob(
        IDispatcher dispatcher,
        INotificationService notificationService,
        IRepository<AppConfiguration> configRepo,
        ILogger<BankTelemetryBackgroundJob> logger)
    {
        _dispatcher = dispatcher;
        _notificationService = notificationService;
        _configRepo = configRepo;
        _logger = logger;
    }

    public async Task RunTelemetryAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Bank Telemetry background scan (UC04)...");

        try
        {
            var defaultTargets = new List<BankSyncTarget>
            {
                new("vpb.com.vn", "VPBank"),
                new("vietcombank.com.vn", "Vietcombank"),
                new("techcombank.com.vn", "Techcombank"),
                new("mbbank.com.vn", "MB Bank"),
                new("momo.vn", "MoMo")
            };

            // Get spreadsheet id configuration if any
            var sheetConfig = await _configRepo.FindOneAsync(c => c.Key == "Finance_SpreadsheetId", ct);
            var spreadsheetId = sheetConfig?.Value ?? string.Empty;

            var command = new SyncBankTransactionsCommand(
                Domain: null,
                BankName: null,
                SpreadsheetId: spreadsheetId,
                Targets: defaultTargets);

            var processedCount = await _dispatcher.SendAsync(command, ct);

            if (processedCount > 0)
            {
                _logger.LogInformation("Successfully processed {Count} bank transactions in background.", processedCount);
                await _notificationService.SendNotificationAsync(
                    "💳 Tự động ghi nhận giao dịch tài chính (UC04)",
                    $"Đã phát hiện và đồng bộ tự động {processedCount} biến động số dư ngân hàng vào Google Sheets.",
                    "info",
                    ct);
            }
            else
            {
                _logger.LogInformation("Bank Telemetry scan complete. No new bank emails found.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Bank Telemetry background execution.");
        }
    }
}
