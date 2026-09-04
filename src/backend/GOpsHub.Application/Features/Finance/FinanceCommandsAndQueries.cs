using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Common.Models;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;

namespace GOpsHub.Application.Features.Finance;

public record ParseTransactionEmailCommand(string GmailMessageId, string BankName, string SpreadsheetId) : ICommand<Transaction?>;

public record GetPendingBankEmailsQuery(string Domain) : IQuery<IReadOnlyList<EmailMessage>>;

public class GetPendingBankEmailsQueryHandler : IQueryHandler<GetPendingBankEmailsQuery, IReadOnlyList<EmailMessage>>
{
    private readonly IGmailService _gmailService;
    public GetPendingBankEmailsQueryHandler(IGmailService gmailService)
    {
        _gmailService = gmailService;
    }
    public async Task<IReadOnlyList<EmailMessage>> HandleAsync(GetPendingBankEmailsQuery query, CancellationToken ct = default)
    {
        return await _gmailService.GetEmailsAsync($"from:{query.Domain} is:unread", 50, ct);
    }
}

public record BankSyncTarget(string Domain, string BankName);

public record SyncBankTransactionsCommand(string? Domain, string? BankName, string SpreadsheetId, List<BankSyncTarget>? Targets = null) : ICommand<int>;

public class SyncBankTransactionsCommandHandler : ICommandHandler<SyncBankTransactionsCommand, int>
{
    private readonly IRepository<Transaction> _transactionRepo;
    private readonly IGmailService _gmailService;
    private readonly IAIService _aiService;
    private readonly ISheetsService _sheetsService;
    private readonly IDriveService _driveService;
    private readonly IRepository<AppConfiguration> _configRepo;
    private readonly IRepository<EmailActionLog> _actionLogRepo;

    public SyncBankTransactionsCommandHandler(
        IRepository<Transaction> transactionRepo,
        IGmailService gmailService,
        IAIService aiService,
        ISheetsService sheetsService,
        IDriveService driveService,
        IRepository<AppConfiguration> configRepo,
        IRepository<EmailActionLog> actionLogRepo)
    {
        _transactionRepo = transactionRepo;
        _gmailService = gmailService;
        _aiService = aiService;
        _sheetsService = sheetsService;
        _driveService = driveService;
        _configRepo = configRepo;
        _actionLogRepo = actionLogRepo;
    }

    public async Task<int> HandleAsync(SyncBankTransactionsCommand command, CancellationToken ct = default)
    {
        var syncTargets = new List<BankSyncTarget>();
        if (command.Targets != null && command.Targets.Any())
        {
            syncTargets.AddRange(command.Targets);
        }
        else if (!string.IsNullOrEmpty(command.Domain) && !string.IsNullOrEmpty(command.BankName))
        {
            syncTargets.Add(new BankSyncTarget(command.Domain, command.BankName));
        }

        if (!syncTargets.Any()) return 0;

        int totalProcessed = 0;
        foreach (var target in syncTargets)
        {
            var emails = await _gmailService.GetEmailsAsync($"from:{target.Domain} is:unread", 20, ct);
            if (emails == null || !emails.Any()) continue;

            var batchContentBuilder = new System.Text.StringBuilder();
            foreach (var email in emails)
            {
                batchContentBuilder.AppendLine($"--- EMAIL ID: {email.Id} ---");
                batchContentBuilder.AppendLine(email.Body ?? email.Snippet);
            }

            var batchResult = await _aiService.ParseBatchTransactionEmailsAsync(batchContentBuilder.ToString(), target.BankName, ct);
            if (batchResult == null || !batchResult.Any()) continue;

            foreach (var aiResult in batchResult)
            {
                if (string.IsNullOrEmpty(aiResult.EmailId)) continue;

                var transactionType = aiResult.TransactionType.Equals("credit", StringComparison.OrdinalIgnoreCase)
                    ? TransactionType.Credit
                    : TransactionType.Debit;

                var transaction = new Transaction
                {
                    SourceEmailId = aiResult.EmailId,
                    TransactionDate = aiResult.TransactionDate,
                    BankName = target.BankName,
                    TransactionType = transactionType,
                    Amount = aiResult.Amount,
                    FeeAmount = aiResult.FeeAmount,
                    TransactionCode = aiResult.TransactionCode,
                    SourceAccount = aiResult.SourceAccount,
                    TargetAccount = aiResult.TargetAccount,
                    BeneficiaryName = aiResult.BeneficiaryName,
                    Currency = "VND",
                    Description = aiResult.Description,
                    Category = aiResult.Category,
                    BalanceAfter = aiResult.BalanceAfter,
                    IsAutoRead = true
                };

                var saved = await _transactionRepo.CreateAsync(transaction, ct);
                await SyncToMonthlyGoogleSheetAsync(saved, command.SpreadsheetId, ct);
                
                // Đánh dấu đã đọc thay vì xóa
                await _gmailService.MarkAsReadAsync(aiResult.EmailId, ct);

                // Ghi audit log chi tiết
                await _actionLogRepo.CreateAsync(new EmailActionLog
                {
                    EmailId = aiResult.EmailId,
                    Subject = $"Biến động số dư: {target.BankName}",
                    Sender = target.Domain,
                    Action = "MarkedRead",
                    SourceJob = "BankTelemetry",
                    Reason = $"Đã ghi nhận giao dịch {transaction.Amount:N0} VND và đánh dấu đã đọc"
                }, ct);

                totalProcessed++;
            }
        }

        return totalProcessed;
    }

    private async Task SyncToMonthlyGoogleSheetAsync(Transaction saved, string? customSpreadsheetId, CancellationToken ct)
    {
        try
        {
            string spreadsheetId = customSpreadsheetId ?? string.Empty;

            var folderConfig = await _configRepo.FindOneAsync(c => c.Key == "Finance_FolderId", ct);
            var patternConfig = await _configRepo.FindOneAsync(c => c.Key == "Finance_FileNamePattern", ct);
            var fixedSheetConfig = await _configRepo.FindOneAsync(c => c.Key == "Finance_SpreadsheetId", ct);

            string? folderId = folderConfig?.Value;
            string fileNamePattern = !string.IsNullOrWhiteSpace(patternConfig?.Value) ? patternConfig.Value : "BaoCaoTaiChinh_{yyyy_MM}";

            if (string.IsNullOrEmpty(spreadsheetId) && !string.IsNullOrWhiteSpace(fixedSheetConfig?.Value))
            {
                spreadsheetId = fixedSheetConfig.Value;
            }

            if (string.IsNullOrEmpty(spreadsheetId))
            {
                var fileName = FormatFileNamePattern(fileNamePattern, saved.TransactionDate);
                var existingId = await _driveService.FindFileByNameAsync(fileName, "application/vnd.google-apps.spreadsheet", folderId, ct);
                if (!string.IsNullOrEmpty(existingId))
                {
                    spreadsheetId = existingId;
                }
                else
                {
                    spreadsheetId = await _sheetsService.CreateSpreadsheetAsync(fileName, ct);
                    if (!string.IsNullOrEmpty(spreadsheetId) && !string.IsNullOrEmpty(folderId))
                    {
                        try
                        {
                            await _driveService.MoveFileAsync(spreadsheetId, folderId, ct);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to move spreadsheet to target folder: {ex.Message}");
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(spreadsheetId))
            {
                var rowValues = new List<object>
                {
                    saved.TransactionCode ?? "",
                    saved.TransactionDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    saved.BankName,
                    saved.TransactionType == TransactionType.Credit ? "+ Nhận" : "- Chi",
                    saved.Amount,
                    saved.FeeAmount,
                    saved.SourceAccount ?? "",
                    saved.TargetAccount ?? "",
                    saved.BeneficiaryName ?? "",
                    saved.Category,
                    saved.Description
                };

                await _sheetsService.AppendRowAsync(spreadsheetId, "A1", rowValues, ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to sync transaction to Google Sheets: {ex.Message}");
        }
    }

    private static string FormatFileNamePattern(string pattern, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(pattern)) pattern = "BaoCaoTaiChinh_{yyyy_MM}";

        string result = pattern
            .Replace("{yyyy_MM}", date.ToString("yyyy_MM"))
            .Replace("{yyyy-MM}", date.ToString("yyyy-MM"))
            .Replace("{yyyy}", date.ToString("yyyy"))
            .Replace("{MM}", date.ToString("MM"));

        return result;
    }
}

public class ParseTransactionEmailCommandHandler : ICommandHandler<ParseTransactionEmailCommand, Transaction?>
{
    private readonly IRepository<Transaction> _transactionRepo;
    private readonly IGmailService _gmailService;
    private readonly IAIService _aiService;
    private readonly ISheetsService _sheetsService;
    private readonly IDriveService _driveService;
    private readonly IRepository<AppConfiguration> _configRepo;

    public ParseTransactionEmailCommandHandler(
        IRepository<Transaction> transactionRepo,
        IGmailService gmailService,
        IAIService aiService,
        ISheetsService sheetsService,
        IDriveService driveService,
        IRepository<AppConfiguration> configRepo)
    {
        _transactionRepo = transactionRepo;
        _gmailService = gmailService;
        _aiService = aiService;
        _sheetsService = sheetsService;
        _driveService = driveService;
        _configRepo = configRepo;
    }

    public async Task<Transaction?> HandleAsync(ParseTransactionEmailCommand command, CancellationToken ct = default)
    {
        var email = await _gmailService.GetEmailByIdAsync(command.GmailMessageId, ct);
        if (email == null) return null;

        // Rate Limit (10 req/min = 6 seconds delay)
        await Task.Delay(6000, ct);

        var contentToParse = !string.IsNullOrWhiteSpace(email.Body) ? email.Body : email.Snippet;
        var aiResult = await _aiService.ParseTransactionEmailAsync(contentToParse, command.BankName, ct);
        if (aiResult == null) return null;

        var transactionType = aiResult.TransactionType.Equals("credit", StringComparison.OrdinalIgnoreCase)
            ? TransactionType.Credit
            : TransactionType.Debit;

        var transaction = new Transaction
        {
            SourceEmailId = email.Id,
            TransactionDate = aiResult.TransactionDate,
            BankName = command.BankName,
            TransactionType = transactionType,
            Amount = aiResult.Amount,
            FeeAmount = aiResult.FeeAmount,
            TransactionCode = aiResult.TransactionCode,
            SourceAccount = aiResult.SourceAccount,
            TargetAccount = aiResult.TargetAccount,
            BeneficiaryName = aiResult.BeneficiaryName,
            Currency = "VND",
            Description = aiResult.Description,
            Category = aiResult.Category,
            BalanceAfter = aiResult.BalanceAfter,
            IsAutoRead = true
        };

        var saved = await _transactionRepo.CreateAsync(transaction, ct);

        await SyncToMonthlyGoogleSheetAsync(saved, command.SpreadsheetId, ct);

        // Mark email as read so it won't be processed again
        await _gmailService.MarkAsReadAsync(email.Id, ct);

        return saved;
    }

    private async Task SyncToMonthlyGoogleSheetAsync(Transaction saved, string? customSpreadsheetId, CancellationToken ct)
    {
        try
        {
            string spreadsheetId = customSpreadsheetId ?? string.Empty;

            var folderConfig = await _configRepo.FindOneAsync(c => c.Key == "Finance_FolderId", ct);
            var patternConfig = await _configRepo.FindOneAsync(c => c.Key == "Finance_FileNamePattern", ct);
            var fixedSheetConfig = await _configRepo.FindOneAsync(c => c.Key == "Finance_SpreadsheetId", ct);

            string? folderId = folderConfig?.Value;
            string fileNamePattern = !string.IsNullOrWhiteSpace(patternConfig?.Value) ? patternConfig.Value : "BaoCaoTaiChinh_{yyyy_MM}";

            if (string.IsNullOrEmpty(spreadsheetId) && !string.IsNullOrWhiteSpace(fixedSheetConfig?.Value))
            {
                spreadsheetId = fixedSheetConfig.Value;
            }

            if (string.IsNullOrEmpty(spreadsheetId))
            {
                var fileName = FormatFileNamePattern(fileNamePattern, saved.TransactionDate);
                var existingId = await _driveService.FindFileByNameAsync(fileName, "application/vnd.google-apps.spreadsheet", folderId, ct);
                if (!string.IsNullOrEmpty(existingId))
                {
                    spreadsheetId = existingId;
                }
                else
                {
                    spreadsheetId = await _sheetsService.CreateSpreadsheetAsync(fileName, ct);
                    if (!string.IsNullOrEmpty(spreadsheetId) && !string.IsNullOrEmpty(folderId))
                    {
                        try
                        {
                            await _driveService.MoveFileAsync(spreadsheetId, folderId, ct);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to move spreadsheet to target folder: {ex.Message}");
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(spreadsheetId))
            {
                var rowValues = new List<object>
                {
                    saved.TransactionCode ?? "",
                    saved.TransactionDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    saved.BankName,
                    saved.TransactionType == TransactionType.Credit ? "+ Nhận" : "- Chi",
                    saved.Amount,
                    saved.FeeAmount,
                    saved.SourceAccount ?? "",
                    saved.TargetAccount ?? "",
                    saved.BeneficiaryName ?? "",
                    saved.Category,
                    saved.Description,
                    saved.BalanceAfter ?? 0
                };

                await _sheetsService.AppendRowAsync(spreadsheetId, "A1", rowValues, ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to sync transaction to Google Sheets: {ex.Message}");
        }
    }

    private static string FormatFileNamePattern(string pattern, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(pattern)) pattern = "BaoCaoTaiChinh_{yyyy_MM}";

        string result = pattern
            .Replace("{yyyy_MM}", date.ToString("yyyy_MM"))
            .Replace("{yyyy-MM}", date.ToString("yyyy-MM"))
            .Replace("{yyyy}", date.ToString("yyyy"))
            .Replace("{MM}", date.ToString("MM"));

        return result;
    }
}

public record GetTransactionsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<Transaction>>;

public class GetTransactionsQueryHandler : IQueryHandler<GetTransactionsQuery, PagedResult<Transaction>>
{
    private readonly IRepository<Transaction> _transactionRepo;

    public GetTransactionsQueryHandler(IRepository<Transaction> transactionRepo)
    {
        _transactionRepo = transactionRepo;
    }

    public async Task<PagedResult<Transaction>> HandleAsync(GetTransactionsQuery query, CancellationToken ct = default)
    {
        var (items, total) = await _transactionRepo.GetPagedAsync(
            null,
            query.Page,
            query.PageSize,
            x => x.TransactionDate,
            true,
            ct);

        return new PagedResult<Transaction>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

public class FinanceConfigDto
{
    public string? FolderId { get; set; }
    public string? FileNamePattern { get; set; }
    public string? SpreadsheetId { get; set; }
}

public record GetFinanceConfigQuery : IQuery<FinanceConfigDto>;

public class GetFinanceConfigQueryHandler : IQueryHandler<GetFinanceConfigQuery, FinanceConfigDto>
{
    private readonly IRepository<AppConfiguration> _configRepo;

    public GetFinanceConfigQueryHandler(IRepository<AppConfiguration> configRepo)
    {
        _configRepo = configRepo;
    }

    public async Task<FinanceConfigDto> HandleAsync(GetFinanceConfigQuery query, CancellationToken ct = default)
    {
        var folderConfig = await _configRepo.FindOneAsync(c => c.Key == "Finance_FolderId", ct);
        var patternConfig = await _configRepo.FindOneAsync(c => c.Key == "Finance_FileNamePattern", ct);
        var sheetConfig = await _configRepo.FindOneAsync(c => c.Key == "Finance_SpreadsheetId", ct);

        return new FinanceConfigDto
        {
            FolderId = folderConfig?.Value ?? "",
            FileNamePattern = !string.IsNullOrWhiteSpace(patternConfig?.Value) ? patternConfig.Value : "BaoCaoTaiChinh_{yyyy_MM}",
            SpreadsheetId = sheetConfig?.Value ?? ""
        };
    }
}

public record UpdateFinanceConfigCommand(string? FolderId, string? FileNamePattern, string? SpreadsheetId) : ICommand<bool>;

public class UpdateFinanceConfigCommandHandler : ICommandHandler<UpdateFinanceConfigCommand, bool>
{
    private readonly IRepository<AppConfiguration> _configRepo;

    public UpdateFinanceConfigCommandHandler(IRepository<AppConfiguration> configRepo)
    {
        _configRepo = configRepo;
    }

    public async Task<bool> HandleAsync(UpdateFinanceConfigCommand command, CancellationToken ct = default)
    {
        await SaveOrUpdateConfig("Finance_FolderId", command.FolderId ?? "", ct);
        await SaveOrUpdateConfig("Finance_FileNamePattern", command.FileNamePattern ?? "BaoCaoTaiChinh_{yyyy_MM}", ct);
        await SaveOrUpdateConfig("Finance_SpreadsheetId", command.SpreadsheetId ?? "", ct);
        return true;
    }

    private async Task SaveOrUpdateConfig(string key, string value, CancellationToken ct)
    {
        var config = await _configRepo.FindOneAsync(c => c.Key == key, ct);
        if (config != null)
        {
            config.Value = value;
            config.UpdatedAt = DateTime.UtcNow;
            await _configRepo.UpdateAsync(config, ct);
        }
        else
        {
            await _configRepo.CreateAsync(new AppConfiguration
            {
                Key = key,
                Value = value
            }, ct);
        }
    }
}

public record FinanceMonthlySummaryDto(decimal TotalIncome, decimal TotalExpense, decimal NetBalance, int TotalTransactions);

public record GetMonthlyFinanceSummaryQuery(int? Year = null, int? Month = null) : IQuery<FinanceMonthlySummaryDto>;

public class GetMonthlyFinanceSummaryQueryHandler : IQueryHandler<GetMonthlyFinanceSummaryQuery, FinanceMonthlySummaryDto>
{
    private readonly IRepository<Transaction> _transactionRepo;

    public GetMonthlyFinanceSummaryQueryHandler(IRepository<Transaction> transactionRepo)
    {
        _transactionRepo = transactionRepo;
    }

    public async Task<FinanceMonthlySummaryDto> HandleAsync(GetMonthlyFinanceSummaryQuery query, CancellationToken ct = default)
    {
        var targetYear = query.Year ?? DateTime.UtcNow.Year;
        var targetMonth = query.Month ?? DateTime.UtcNow.Month;
        var startDate = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        var transactions = await _transactionRepo.FindAsync(t => t.TransactionDate >= startDate && t.TransactionDate < endDate, ct);
        var totalIncome = transactions.Where(t => t.TransactionType == TransactionType.Credit).Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.TransactionType == TransactionType.Debit).Sum(t => t.Amount);

        return new FinanceMonthlySummaryDto(totalIncome, totalExpense, totalIncome - totalExpense, transactions.Count);
    }
}
