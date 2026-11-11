using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;

namespace GOpsHub.Application.Features.Finance;

/// <summary>
/// Appends a recorded transaction to the monthly Google Sheet. Shared by bank sync and
/// single-email parsing so both write the same row layout to the same spreadsheet.
/// </summary>
public static class FinanceSheetSync
{
    public const string DefaultFileNamePattern = "BaoCaoTaiChinh_{yyyy_MM}";

    public static async Task AppendTransactionAsync(
        Transaction saved,
        string? customSpreadsheetId,
        IRepository<AppConfiguration> configRepo,
        IDriveService driveService,
        ISheetsService sheetsService,
        CancellationToken ct)
    {
        try
        {
            var spreadsheetId = await ResolveSpreadsheetIdAsync(saved.TransactionDate, customSpreadsheetId, configRepo, driveService, sheetsService, ct);
            if (string.IsNullOrEmpty(spreadsheetId)) return;

            await sheetsService.AppendRowAsync(spreadsheetId, "A1", BuildRow(saved), ct);
        }
        catch (Exception ex)
        {
            // The transaction is already saved in MongoDB; a Sheets failure must not undo it.
            Console.WriteLine($"Failed to sync transaction to Google Sheets: {ex.Message}");
        }
    }

    /// <summary>Column order matches the header written by SheetsApiService.CreateSpreadsheetAsync.</summary>
    public static List<object> BuildRow(Transaction t) => new()
    {
        t.TransactionCode ?? "",
        t.TransactionDate.ToString("yyyy-MM-dd HH:mm:ss"),
        t.BankName,
        t.TransactionType == TransactionType.Credit ? "+ Nhận" : "- Chi",
        t.Amount,
        t.FeeAmount,
        t.SourceAccount ?? "",
        t.TargetAccount ?? "",
        t.BeneficiaryName ?? "",
        t.Category,
        t.Description,
        t.BalanceAfter ?? 0
    };

    public static string FormatFileNamePattern(string? pattern, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(pattern)) pattern = DefaultFileNamePattern;

        return pattern
            .Replace("{yyyy_MM}", date.ToString("yyyy_MM"))
            .Replace("{yyyy-MM}", date.ToString("yyyy-MM"))
            .Replace("{yyyy}", date.ToString("yyyy"))
            .Replace("{MM}", date.ToString("MM"));
    }

    private static async Task<string?> ResolveSpreadsheetIdAsync(
        DateTime transactionDate,
        string? customSpreadsheetId,
        IRepository<AppConfiguration> configRepo,
        IDriveService driveService,
        ISheetsService sheetsService,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(customSpreadsheetId)) return customSpreadsheetId;

        var fixedSheetConfig = await configRepo.FindOneAsync(c => c.Key == "Finance_SpreadsheetId", ct);
        if (!string.IsNullOrWhiteSpace(fixedSheetConfig?.Value)) return fixedSheetConfig.Value;

        var folderConfig = await configRepo.FindOneAsync(c => c.Key == "Finance_FolderId", ct);
        var patternConfig = await configRepo.FindOneAsync(c => c.Key == "Finance_FileNamePattern", ct);
        var folderId = folderConfig?.Value;
        var fileName = FormatFileNamePattern(patternConfig?.Value, transactionDate);

        var existingId = await driveService.FindFileByNameAsync(fileName, "application/vnd.google-apps.spreadsheet", folderId, ct);
        if (!string.IsNullOrEmpty(existingId)) return existingId;

        var spreadsheetId = await sheetsService.CreateSpreadsheetAsync(fileName, ct);
        if (!string.IsNullOrEmpty(spreadsheetId) && !string.IsNullOrEmpty(folderId))
        {
            try
            {
                await driveService.MoveFileAsync(spreadsheetId, folderId, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to move spreadsheet to target folder: {ex.Message}");
            }
        }
        return spreadsheetId;
    }
}
