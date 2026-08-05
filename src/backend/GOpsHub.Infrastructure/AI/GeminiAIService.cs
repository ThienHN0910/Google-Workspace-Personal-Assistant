using System.Text;
using System.Text.Json;
using GOpsHub.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.AI;

public class GeminiAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly ILogger<GeminiAIService> _logger;

    public GeminiAIService(IConfiguration configuration, ILogger<GeminiAIService> logger)
    {
        _httpClient = new HttpClient();
        _apiKey = configuration["Gemini:ApiKey"] ?? configuration["GEMINI_API_KEY"];
        _model = configuration["Gemini:Model"] ?? configuration["GEMINI_MODEL"] ?? "gemini-3.6-flash-lite";
        _logger = logger;
    }

    public async Task<AIReplyResult> GenerateEmailReplyAsync(string emailContent, string language = "vi", string? templateHint = null, CancellationToken ct = default)
    {
        var prompt = $@"Bạn là trợ lý AI cá nhân cho Thien HN. Hãy giúp soạn câu trả lời email sau bằng tiếng {language}.
Yêu cầu:
- Tác phong lịch sự, ngắn gọn, đi thẳng vào vấn đề.
- Ngôn ngữ: {(language == "en" ? "English" : "Tiếng Việt")}.
{(string.IsNullOrEmpty(templateHint) ? "" : $"- Tham khảo mẫu trả lời sau: {templateHint}")}

Nội dung email nhận được:
{emailContent}

- Bạn phải trả về định dạng HTML thuần túy (sử dụng các thẻ <div>, <p>, <br>, <strong>,...). 
- KHÔNG thêm block code ```html, chỉ trả về nội dung HTML trực tiếp.
- Hãy trả về nội dung email phản hồi duy nhất (không giải thích thêm).";

        try
        {
            var responseText = await CallGeminiApiAsync(prompt, ct);

            return new AIReplyResult
            {
                DraftContent = responseText.Replace("```html", "").Replace("```", "").Trim(),
                ConfidenceScore = 0.90,
                DetectedLanguage = language
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI reply.");
            return new AIReplyResult
            {
                DraftContent = $"Lỗi khi gọi API AI: {ex.Message}",
                ConfidenceScore = 0,
                DetectedLanguage = language
            };
        }
    }

    public async Task<AIScheduleResult?> ExtractScheduleFromEmailAsync(string emailContent, CancellationToken ct = default)
    {
        var prompt = $@"Phân tích email sau và trích xuất thông tin lịch hẹn dưới dạng JSON:
Nội dung email:
{emailContent}

Cấu trúc JSON yêu cầu:
{{
  ""title"": ""Tiêu đề sự kiện"",
  ""startTime"": ""YYYY-MM-DDTHH:mm:ss"",
  ""endTime"": ""YYYY-MM-DDTHH:mm:ss hoặc null"",
  ""location"": ""Địa điểm / Link meeting"",
  ""description"": ""Mô tả ngắn"",
  ""eventType"": ""interview | flight | meeting | appointment | deadline | other"",
  ""confidenceScore"": 0.95
}}

Chỉ trả về JSON thuần hợp lệ (không chứa markdown backticks ```json).";

        var responseText = await CallGeminiApiAsync(prompt, ct);
        try
        {
            var cleanedJson = CleanJsonResponse(responseText);
            var result = JsonSerializer.Deserialize<AIScheduleResult>(cleanedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse schedule JSON from Gemini response: {Response}", responseText);
            return null;
        }
    }

    public async Task<AITransactionResult?> ParseTransactionEmailAsync(string emailContent, string bankName, CancellationToken ct = default)
    {
        var prompt = $@"Phân tích biến động số dư ngân hàng/ví điện tử ({bankName}) từ email sau dưới dạng JSON:
Nội dung email:
{emailContent}

Cấu trúc JSON yêu cầu:
{{
  ""transactionDate"": ""YYYY-MM-DDTHH:mm:ss"",
  ""transactionType"": ""credit | debit"",
  ""amount"": 500000,
  ""feeAmount"": 0,
  ""transactionCode"": ""Mã giao dịch / mã tham chiếu"",
  ""sourceAccount"": ""Số tài khoản trích / chuyển"",
  ""targetAccount"": ""Số tài khoản ghi / nhận"",
  ""beneficiaryName"": ""Tên người hưởng / người chuyển"",
  ""description"": ""Nội dung chuyển khoản / giao dịch"",
  ""category"": ""food | transport | bills | shopping | salary | transfer | other"",
  ""balanceAfter"": 10000000
}}

Chỉ trả về JSON thuần hợp lệ.";

        var responseText = await CallGeminiApiAsync(prompt, ct);
        try
        {
            var cleanedJson = CleanJsonResponse(responseText);
            return JsonSerializer.Deserialize<AITransactionResult>(cleanedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse transaction JSON from Gemini response: {Response}", responseText);
            return null;
        }
    }

    public async Task<List<AIBatchTransactionResult>> ParseBatchTransactionEmailsAsync(string batchContent, string bankName, CancellationToken ct = default)
    {
        var prompt = $@"Phân tích hàng loạt các email biến động số dư ngân hàng/ví điện tử ({bankName}) sau đây.
Mỗi email được phân tách bởi chuỗi bắt đầu bằng --- EMAIL ID: <id> ---

Nhiệm vụ: Trích xuất giao dịch từ MỖI email và trả về 1 mảng (Array) JSON.
LƯU Ý RẤT QUAN TRỌNG: Nếu email không có thông tin 'Số dư hiện tại' (balanceAfter), BẮT BUỘC gán giá trị của nó là null.

Cấu trúc JSON yêu cầu trả về:
[
  {{
    ""emailId"": ""<id của email>"",
    ""transactionDate"": ""YYYY-MM-DDTHH:mm:ss"",
    ""transactionType"": ""credit | debit"",
    ""amount"": 500000,
    ""feeAmount"": 0,
    ""transactionCode"": ""Mã giao dịch / mã tham chiếu"",
    ""sourceAccount"": ""Số tài khoản trích / chuyển"",
    ""targetAccount"": ""Số tài khoản ghi / nhận"",
    ""beneficiaryName"": ""Tên người hưởng / người chuyển"",
    ""description"": ""Nội dung chuyển khoản / giao dịch"",
    ""category"": ""food | transport | bills | shopping | salary | transfer | other"",
    ""balanceAfter"": null
  }}
]

Dữ liệu Email:
{batchContent}

Chỉ trả về JSON Array thuần hợp lệ.";

        var responseText = await CallGeminiApiAsync(prompt, ct);
        try
        {
            var cleanedJson = CleanJsonResponse(responseText);
            var result = JsonSerializer.Deserialize<List<AIBatchTransactionResult>>(cleanedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result ?? new List<AIBatchTransactionResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse batch transactions JSON from Gemini response: {Response}", responseText);
            return new List<AIBatchTransactionResult>();
        }
    }

    public async Task<string> SummarizeEmailThreadAsync(string threadContent, CancellationToken ct = default)
    {
        var prompt = $"Tóm tắt luồng email sau trong 3 câu ngắn gọn bằng tiếng Việt:\n\n{threadContent}";
        return await CallGeminiApiAsync(prompt, ct);
    }

    /// <summary>
    /// UC13 — Smart Email Priority Scoring (1-10)
    /// </summary>
    public async Task<int> ScoreEmailPriorityAsync(string from, string subject, string snippet, CancellationToken ct = default)
    {
        var prompt = $@"Đánh giá độ ưu tiên của email sau trên thang điểm từ 1 đến 10 (10 là rất khẩn cấp/quan trọng).
Người gửi: {from}
Tiêu đề: {subject}
Nội dung: {snippet}

Chỉ trả về 1 con số nguyên duy nhất từ 1 đến 10.";

        var responseText = await CallGeminiApiAsync(prompt, ct);
        if (int.TryParse(responseText.Trim(), out var score))
        {
            return Math.Clamp(score, 1, 10);
        }
        return 5;
    }

    /// <summary>
    /// UC18 — Extract TODO Tasks from Email
    /// </summary>
    public async Task<List<string>> ExtractTasksFromEmailAsync(string emailContent, CancellationToken ct = default)
    {
        var prompt = $@"Trích xuất các việc cần làm (action items) từ email sau thành danh sách JSON các chuỗi:
{emailContent}

Ví dụ trả về: [""Gửi báo cáo trước 5h chiều"", ""Họp với team thiết kế""]
Chỉ trả về JSON array hợp lệ.";

        var responseText = await CallGeminiApiAsync(prompt, ct);
        try
        {
            var cleanedJson = CleanJsonResponse(responseText);
            return JsonSerializer.Deserialize<List<string>>(cleanedJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// UC14 — Recurring Report Generator
    /// </summary>
    public async Task<string> GenerateExecutiveReportAsync(string periodStats, CancellationToken ct = default)
    {
        var prompt = $@"Soạn báo cáo vận hành tóm tắt cấp cao (Executive Summary Report) bằng tiếng Việt cho Thien HN dựa trên số liệu sau:
{periodStats}

Định dạng bằng Markdown đẹp mắt với các tiêu đề rõ ràng.";

        return await CallGeminiApiAsync(prompt, ct);
    }

    private async Task<string> CallGeminiApiAsync(string prompt, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured. Returning fallback response.");
            return "Cảm ơn bạn đã gửi email. Tôi đã nhận được thông tin và sẽ phản hồi sớm nhất.";
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API Error: {StatusCode} - {Body}", response.StatusCode, body);
            throw new Exception($"Gemini API Error: {response.StatusCode} - {body}");
        }

        try
        {
            using var doc = JsonDocument.Parse(body);

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini API response. Body: {Body}", body);
            return $"Lỗi JSON Parser. Raw Body: {body}";
        }
    }

    private static string CleanJsonResponse(string responseText)
    {
        var trimmed = responseText.Trim();
        if (trimmed.StartsWith("```json")) trimmed = trimmed[7..];
        if (trimmed.StartsWith("```")) trimmed = trimmed[3..];
        if (trimmed.EndsWith("```")) trimmed = trimmed[..^3];
        return trimmed.Trim();
    }

    public async Task<bool> CheckCleanupConditionAsync(string emailContent, string prompt, CancellationToken ct = default)
    {
        var aiPrompt = $"Analyze the following email content and decide if it matches this condition: '{prompt}'. Reply only with 'YES' or 'NO'.\n\nEmail:\n{emailContent}";
        
        try
        {
            var result = await CallGeminiApiAsync(aiPrompt, ct);
            return result.Trim().ToUpper().Contains("YES");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API Error in CheckCleanupCondition");
            return false;
        }
    }
}
