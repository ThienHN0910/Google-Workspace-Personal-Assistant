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

public record SyncBankTransactionsCommand(string Domain, string BankName, string SpreadsheetId) : ICommand<int>;

public class SyncBankTransactionsCommandHandler : ICommandHandler<SyncBankTransactionsCommand, int>
{
    private readonly IRepository<Transaction> _transactionRepo;
    private readonly IGmailService _gmailService;
    private readonly IAIService _aiService;
    private readonly ISheetsService _sheetsService;

    public SyncBankTransactionsCommandHandler(
        IRepository<Transaction> transactionRepo,
        IGmailService gmailService,
        IAIService aiService,
        ISheetsService sheetsService)
    {
        _transactionRepo = transactionRepo;
        _gmailService = gmailService;
        _aiService = aiService;
        _sheetsService = sheetsService;
    }

    public async Task<int> HandleAsync(SyncBankTransactionsCommand command, CancellationToken ct = default)
    {
        var emails = await _gmailService.GetEmailsAsync($"from:{command.Domain} is:unread", 20, ct);
        if (emails == null || !emails.Any()) return 0;

        var batchContentBuilder = new System.Text.StringBuilder();
        foreach (var email in emails)
        {
            batchContentBuilder.AppendLine($"--- EMAIL ID: {email.Id} ---");
            batchContentBuilder.AppendLine(email.Body ?? email.Snippet);
        }

        var batchResult = await _aiService.ParseBatchTransactionEmailsAsync(batchContentBuilder.ToString(), command.BankName, ct);
        if (batchResult == null || !batchResult.Any()) return 0;

        int processed = 0;
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
                BankName = command.BankName,
                TransactionType = transactionType,
                Amount = aiResult.Amount,
                Currency = "VND",
                Description = aiResult.Description,
                Category = aiResult.Category,
                BalanceAfter = aiResult.BalanceAfter,
                IsAutoRead = true
            };

            var saved = await _transactionRepo.CreateAsync(transaction, ct);

            if (!string.IsNullOrEmpty(command.SpreadsheetId))
            {
                var sheetName = $"Transactions_{saved.TransactionDate:yyyy_MM}";
                var rowValues = new List<object>
                {
                    saved.TransactionDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    saved.BankName,
                    saved.TransactionType.ToString(),
                    saved.Amount,
                    saved.Category,
                    saved.Description,
                    saved.BalanceAfter ?? 0
                };
                await _sheetsService.AppendRowAsync(command.SpreadsheetId, sheetName, rowValues, ct);
            }

            await _gmailService.MarkAsReadAsync(aiResult.EmailId, ct);
            processed++;
        }

        return processed;
    }
}

public class ParseTransactionEmailCommandHandler : ICommandHandler<ParseTransactionEmailCommand, Transaction?>
{
    private readonly IRepository<Transaction> _transactionRepo;
    private readonly IGmailService _gmailService;
    private readonly IAIService _aiService;
    private readonly ISheetsService _sheetsService;

    public ParseTransactionEmailCommandHandler(
        IRepository<Transaction> transactionRepo,
        IGmailService gmailService,
        IAIService aiService,
        ISheetsService sheetsService)
    {
        _transactionRepo = transactionRepo;
        _gmailService = gmailService;
        _aiService = aiService;
        _sheetsService = sheetsService;
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
            Currency = "VND",
            Description = aiResult.Description,
            Category = aiResult.Category,
            BalanceAfter = aiResult.BalanceAfter,
            IsAutoRead = true
        };

        var saved = await _transactionRepo.CreateAsync(transaction, ct);

        // Sync to Google Sheets if spreadsheet ID provided
        if (!string.IsNullOrEmpty(command.SpreadsheetId))
        {
            var sheetName = $"Transactions_{saved.TransactionDate:yyyy_MM}";
            var rowValues = new List<object>
            {
                saved.TransactionDate.ToString("yyyy-MM-dd HH:mm:ss"),
                saved.BankName,
                saved.TransactionType.ToString(),
                saved.Amount,
                saved.Category,
                saved.Description,
                saved.BalanceAfter ?? 0
            };

            await _sheetsService.AppendRowAsync(command.SpreadsheetId, sheetName, rowValues, ct);
        }

        // Mark email as read so it won't be processed again
        await _gmailService.MarkAsReadAsync(email.Id, ct);

        return saved;
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
