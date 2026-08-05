using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.GoogleApis;

public class SheetsApiService : ISheetsService
{
    private readonly IRepository<AdminUser> _userRepo;
    private readonly ITokenEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SheetsApiService> _logger;
    private readonly string _adminEmail;

    public SheetsApiService(
        IRepository<AdminUser> userRepo,
        ITokenEncryptionService encryptionService,
        IConfiguration configuration,
        ILogger<SheetsApiService> logger)
    {
        _userRepo = userRepo;
        _encryptionService = encryptionService;
        _configuration = configuration;
        _logger = logger;
        _adminEmail = _configuration["ADMIN_EMAIL"] ?? "hnt.vn.vn@gmail.com";
    }

    private async Task<SheetsService?> GetSheetsClientAsync(CancellationToken ct = default)
    {
        var user = await _userRepo.FindOneAsync(u => u.Email == _adminEmail, ct);
        if (user == null || string.IsNullOrEmpty(user.GoogleAccessToken))
        {
            _logger.LogWarning("Admin user token not found for Sheets API calls.");
            return null;
        }

        var accessToken = _encryptionService.Decrypt(user.GoogleAccessToken);
        var credential = GoogleCredential.FromAccessToken(accessToken);

        return new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "G-Ops Hub"
        });
    }

    public async Task AppendRowAsync(string spreadsheetId, string sheetName, IList<object> values, CancellationToken ct = default)
    {
        var service = await GetSheetsClientAsync(ct);
        if (service == null) return;

        try
        {
            string actualSheetTitle = sheetName;
            if (string.IsNullOrWhiteSpace(actualSheetTitle) || actualSheetTitle.Equals("A1", StringComparison.OrdinalIgnoreCase) || actualSheetTitle.Equals("Sheet1", StringComparison.OrdinalIgnoreCase))
            {
                var meta = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync(ct);
                actualSheetTitle = meta.Sheets?.FirstOrDefault()?.Properties?.Title ?? "Sheet1";
            }

            var valueRange = new ValueRange
            {
                Values = new List<IList<object>> { values }
            };

            var request = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, $"'{actualSheetTitle}'!A1");
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

            await request.ExecuteAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error appending row to Google Spreadsheet {SpreadsheetId} in sheet '{SheetName}'", spreadsheetId, sheetName);
            throw;
        }
    }

    public async Task<IList<IList<object>>> GetRangeAsync(string spreadsheetId, string range, CancellationToken ct = default)
    {
        var service = await GetSheetsClientAsync(ct);
        if (service == null) return Array.Empty<IList<object>>();

        var response = await service.Spreadsheets.Values.Get(spreadsheetId, range).ExecuteAsync(ct);
        return response.Values ?? Array.Empty<IList<object>>();
    }

    public async Task<string> CreateSheetTabAsync(string spreadsheetId, string sheetName, CancellationToken ct = default)
    {
        var service = await GetSheetsClientAsync(ct);
        if (service == null) return string.Empty;

        var addSheetRequest = new AddSheetRequest
        {
            Properties = new SheetProperties { Title = sheetName }
        };

        var batchUpdate = new BatchUpdateSpreadsheetRequest
        {
            Requests = new List<Request> { new Request { AddSheet = addSheetRequest } }
        };

        var response = await service.Spreadsheets.BatchUpdate(batchUpdate, spreadsheetId).ExecuteAsync(ct);
        return response.Replies.FirstOrDefault()?.AddSheet?.Properties?.SheetId?.ToString() ?? string.Empty;
    }

    public async Task<string> CreateSpreadsheetAsync(string title, CancellationToken ct = default)
    {
        var service = await GetSheetsClientAsync(ct);
        if (service == null) return string.Empty;

        try
        {
            var spreadsheet = new Spreadsheet
            {
                Properties = new SpreadsheetProperties
                {
                    Title = title
                }
            };

            var created = await service.Spreadsheets.Create(spreadsheet).ExecuteAsync(ct);
            var firstSheetTitle = created.Sheets?.FirstOrDefault()?.Properties?.Title ?? "Sheet1";

            var headers = new List<object>
            {
                "Mã GD",
                "Thời gian",
                "Ngân hàng",
                "Loại",
                "Số tiền",
                "Số tiền phí",
                "Tài khoản trích",
                "Tài khoản ghi",
                "Tên người hưởng",
                "Danh mục",
                "Nội dung"
            };

            var valueRange = new ValueRange { Values = new List<IList<object>> { headers } };
            var appendReq = service.Spreadsheets.Values.Append(valueRange, created.SpreadsheetId, $"'{firstSheetTitle}'!A1");
            appendReq.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            appendReq.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
            await appendReq.ExecuteAsync(ct);

            return created.SpreadsheetId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Google Spreadsheet with title '{Title}'", title);
            throw;
        }
    }
}
