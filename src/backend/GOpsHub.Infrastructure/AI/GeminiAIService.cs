using System.Text;
using System.Text.Json;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.AI;

public class GeminiAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _defaultModel;
    private readonly ILogger<GeminiAIService> _logger;
    private readonly GeminiRateLimiter _rateLimiter;
    private readonly IAiUsageTracker _usageTracker;
    private readonly INotificationService? _notificationService;
    private readonly IRepository<AppConfiguration>? _configRepo;

    public GeminiAIService(
        IConfiguration configuration,
        ILogger<GeminiAIService> logger,
        GeminiRateLimiter rateLimiter,
        IAiUsageTracker usageTracker,
        INotificationService? notificationService = null,
        HttpClient? httpClient = null,
        IRepository<AppConfiguration>? configRepo = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _apiKey = configuration["Gemini:ApiKey"] ?? configuration["GEMINI_API_KEY"];
        _defaultModel = configuration["Gemini:Model"] ?? configuration["GEMINI_MODEL"] ?? "gemini-3.5-flash-lite";
        _logger = logger;
        _rateLimiter = rateLimiter;
        _usageTracker = usageTracker;
        _notificationService = notificationService;
        _configRepo = configRepo;
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
            var responseText = await CallGeminiApiAsync(prompt, featureName: "EmailReply", isBackground: false, ct: ct);

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

        var responseText = await CallGeminiApiAsync(prompt, featureName: "ScheduleExtractor", isBackground: true, ct: ct);
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

        var responseText = await CallGeminiApiAsync(prompt, featureName: "BankTelemetry", isBackground: true, ct: ct);
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

        var responseText = await CallGeminiApiAsync(prompt, featureName: "BankTelemetry", isBackground: true, ct: ct);
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
        return await CallGeminiApiAsync(prompt, featureName: "EmailSummary", isBackground: false, ct: ct);
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

        var responseText = await CallGeminiApiAsync(prompt, featureName: "EmailPriority", isBackground: true, ct: ct);
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

        var responseText = await CallGeminiApiAsync(prompt, featureName: "TaskExtractor", isBackground: false, ct: ct);
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

        return await CallGeminiApiAsync(prompt, featureName: "ExecutiveReport", isBackground: false, ct: ct);
    }

    private async Task<string> GetActiveModelAsync(CancellationToken ct)
    {
        if (_configRepo != null)
        {
            try
            {
                var dbConfig = await _configRepo.FindOneAsync(c => c.Key == "GeminiModel", ct);
                if (!string.IsNullOrWhiteSpace(dbConfig?.Value))
                {
                    return dbConfig.Value.Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read GeminiModel from AppConfiguration repository. Falling back to default model.");
            }
        }
        return _defaultModel;
    }

    private async Task<string> CallGeminiApiAsync(
        string prompt,
        string featureName = "General",
        bool isBackground = false,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured. Returning fallback response.");
            return "Cảm ơn bạn đã gửi email. Tôi đã nhận được thông tin và sẽ phản hồi sớm nhất.";
        }

        // Ước tính input tokens trước khi gửi để tránh vượt quá 240k TPM
        long estimatedInputTokens = (long)(prompt.Length / 3.5);
        await _rateLimiter.WaitForSlotAsync(estimatedInputTokens, ct);

        var activeModel = await GetActiveModelAsync(ct);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{activeModel}:generateContent?key={_apiKey}";

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

            // Cảnh báo thời gian thực khi bị lỗi HTTP 429 Too Many Requests
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && _notificationService != null)
            {
                try
                {
                    await _notificationService.SendNotificationAsync(
                        "🚨 Cảnh báo Gemini AI: Chạm ngưỡng Rate Limit (HTTP 429)",
                        $"Google AI vừa từ chối yêu cầu cho tính năng <b>{featureName}</b> do chạm ngưỡng tần suất gọi (HTTP 429 Too Many Requests).\nHệ thống đang tự động kích hoạt cơ chế giãn cách chờ slot khả dụng.",
                        "critical",
                        ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send HTTP 429 rate limit notification to Telegram.");
                }
            }

            throw new Exception($"Gemini API Error: {response.StatusCode} - {body}");
        }

        try
        {
            using var doc = JsonDocument.Parse(body);

            // Ghi nhận token tiêu thụ và kiểm tra ngưỡng tải input tokens
            if (doc.RootElement.TryGetProperty("usageMetadata", out var usageElem))
            {
                long promptTokens = usageElem.TryGetProperty("promptTokenCount", out var pt) ? pt.GetInt64() : 0;
                long candTokens = usageElem.TryGetProperty("candidatesTokenCount", out var ctElem) ? ctElem.GetInt64() : 0;
                long totalTokens = usageElem.TryGetProperty("totalTokenCount", out var tt) ? tt.GetInt64() : (promptTokens + candTokens);

                await _usageTracker.RecordUsageAsync(featureName, promptTokens, candTokens, totalTokens, ct);

                // Ghi nhận input token vào sliding window và cảnh báo nếu chạm đỉnh >= 200k TPM
                bool isPeak = _rateLimiter.RecordInputTokens(promptTokens);
                if (isPeak && _notificationService != null)
                {
                    var (_, currentTpm, _, _) = _rateLimiter.GetStatus();
                    try
                    {
                        await _notificationService.SendNotificationAsync(
                            "⚠️ Cảnh báo Tải Input Token Gemini AI tăng cao",
                            $"Lượng input token trong 1 phút vừa qua đã đạt đỉnh <b>{currentTpm:N0} / 240,000 TPM</b> (vượt ngưỡng cảnh báo 200k TPM).\nHệ thống đang tự động điều tiết tốc độ để phòng ngừa lỗi 429.",
                            "warning",
                            ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send input token peak warning notification to Telegram.");
                    }
                }
            }

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
            var result = await CallGeminiApiAsync(aiPrompt, featureName: "EmailCleanup", isBackground: true, ct: ct);
            return result.Trim().ToUpper().Contains("YES");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API Error in CheckCleanupCondition");
            return false;
        }
    }

    public Task<AIRegexRuleSuggestion?> AnalyzeSpamPatternsAsync(string emailSnippetsBatch, CancellationToken ct = default)
        => AnalyzeSpamPatternsAsync(emailSnippetsBatch, null, ct);

    public async Task<AIRegexRuleSuggestion?> AnalyzeSpamPatternsAsync(string emailSnippetsBatch, List<CleanupFeedback>? userFeedbacks, CancellationToken ct = default)
    {
        var feedbackSection = new StringBuilder();
        if (userFeedbacks != null && userFeedbacks.Any())
        {
            feedbackSection.AppendLine();
            feedbackSection.AppendLine("VÍ DỤ THỰC TẾ NGƯỜI DÙNG ĐÃ CHỦ ĐỘNG DẠY CÓ THỂ XÓA (FEW-SHOT USER FEEDBACK):");
            feedbackSection.AppendLine("Người dùng đã xác nhận các email mẫu sau đây là rác/không đọc và có thể xóa. Hãy ưu tiên phân tích theo các gu và lý do này:");
            foreach (var fb in userFeedbacks.Take(10))
            {
                var tagStr = fb.Tags != null && fb.Tags.Any() ? $"[{string.Join(", ", fb.Tags)}] " : "";
                feedbackSection.AppendLine($"- Người gửi: {fb.Sender} | Tiêu đề: {fb.Subject} | Lý do xóa: {tagStr}{fb.Reason}");
            }
            feedbackSection.AppendLine("LƯU Ý AN TOÀN QUAN TRỌNG: Dù người dùng dạy xóa email từ ngân hàng/tổ chức tài chính, TUYỆT ĐỐI KHÔNG sinh quy tắc suggestedSenderRegex bao phủ cả domain ngân hàng. Chỉ sinh suggestedSubjectRegex lọc đúng từ khóa quảng cáo cụ thể.");
        }

        var prompt = $@"Bạn là chuyên gia phân loại email. Hãy phân tích danh sách tóm tắt các email sau và tìm xem có nhóm email nào là thư rác, quảng cáo, khuyến mãi (sale off, giảm giá), newsletter lặp đi lặp lại hay không.
Nếu phát hiện mẫu email lặp lại, hãy gợi ý một quy tắc Regex chuẩn để tự động nhận diện các email tương tự trong tương lai.
{feedbackSection}
Quy tắc phân loại và an toàn BẮT BUỘC:
1. Hành động (Action):
   - Chọn ""Archive"" cho: Bản tin (Newsletter), tin tức công nghệ/công việc, cập nhật tính năng sản phẩm (Feature updates/Digest), khảo sát học tập, thông báo cộng đồng. Giúp giữ hộp thư gọn gàng mà không làm mất tài liệu tra cứu.
   - Chọn ""Trash"" cho: Quảng cáo bán hàng, khuyến mãi mua sắm (Shopee, Lazada, voucher), thông báo mạng xã hội phiền toái, spam thực sự.
2. An toàn người gửi & Từ khóa:
   - TUYỆT ĐỐI KHÔNG tạo quy tắc lọc người gửi chung từ các nền tảng kỹ thuật/công việc quan trọng (ví dụ: notifications@github.com, gitlab.com, google.com).
   - TUYỆT ĐỐI KHÔNG lọc các email liên quan đến lịch làm việc/ca trực, cảnh báo bảo mật, mã OTP, hóa đơn tài chính.
   - Các từ khóa ngắn trong Regex bắt buộc phải có word boundary \b (ví dụ: \b(sale|free|deal)\b) để không bắt nhầm các từ ghép (như freelance, freeze).

Danh sách email cần phân tích:
{emailSnippetsBatch}

Yêu cầu trả về đúng định dạng JSON thuần (KHÔNG có markdown block):
{{
  ""hasPattern"": true,
  ""category"": ""Tên nhóm email (VD: Khuyến mãi Shopee / Newsletter HackerNoon)"",
  ""suggestedSubjectRegex"": ""(?i).*\\b(khuyến mãi|sale\\s*(off|\\d+%)|siêu sale)\\b.*"",
  ""suggestedSenderRegex"": ""(?i).*@(shopee|lazada)\\.vn.*"",
  ""action"": ""Archive"",
  ""targetEmailIds"": [""id1"", ""id2""],
  ""reason"": ""Lý do các email này thuộc diện newsletter hoặc quảng cáo lặp lại"",
  ""confidenceScore"": 0.90
}}
Quy định về confidenceScore (thang điểm 0.0 đến 1.0):
- Điểm >= 0.85: Chắc chắn là thư rác/quảng cáo/newsletter định kỳ, tự động dọn dẹp và áp dụng regex.
- Điểm 0.50 đến 0.84: Nghi ngờ là spam/quảng cáo nhưng còn phân vân (có thể chứa thông báo/hóa đơn quan trọng), cần người dùng xem xét duyệt trước khi dọn.
- Điểm < 0.50: Không nên xử lý (đặt hasPattern: false).
Nếu không tìm thấy mẫu email rác lặp lại nào, trả về:
{{
  ""hasPattern"": false
}}";

        try
        {
            var responseText = await CallGeminiApiAsync(prompt, featureName: "EmailCleanup", isBackground: true, ct: ct);
            var cleanJson = CleanJsonResponse(responseText);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<AIRegexRuleSuggestion>(cleanJson, options);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze spam patterns with Gemini AI.");
            return null;
        }
    }

    public async Task<UrgentEmailAnalysisResult> AnalyzeUrgentEmailAsync(string subject, string from, string content, CancellationToken ct = default)
    {
        var prompt = $@"Bạn là trợ lý AI phân tích email công việc. Hãy phân tích email hành động khẩn cấp/quan trọng dưới đây:
Tiêu đề: {subject}
Người gửi: {from}
Nội dung tóm tắt: {content}

Hãy xác định:
1. Mức độ quan trọng: ""Khẩn cấp"" (dịch vụ sắp ngừng hoạt động, cảnh báo bảo mật nguy hiểm, khóa tài khoản), ""Cao"" (cần nâng cấp runtime, deadline dự án trong tuần), hoặc ""Trung bình"".
2. Tóm tắt hành động người dùng cần làm ngắn gọn trong 1 câu (tiếng Việt).
3. Hạn chót/Deadline nếu có nêu rõ trong email (Ví dụ: ""01/10/2026"" hoặc null nếu không nêu).

Trả về đúng định dạng JSON thuần (KHÔNG có markdown codeblock):
{{
  ""urgencyLevel"": ""Khẩn cấp"",
  ""actionSummary"": ""Cần nâng cấp Node.js lên phiên bản 24 trước ngày 01/10/2026 để tránh lỗi bản build"",
  ""deadline"": ""01/10/2026""
}}";

        try
        {
            var responseText = await CallGeminiApiAsync(prompt, featureName: "EmailUrgentAnalysis", isBackground: true, ct: ct);
            var cleanJson = CleanJsonResponse(responseText);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<UrgentEmailAnalysisResult>(cleanJson, options);
            if (result != null && !string.IsNullOrEmpty(result.ActionSummary))
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze urgent email with Gemini AI.");
        }

        return new UrgentEmailAnalysisResult
        {
            UrgencyLevel = "Cao",
            ActionSummary = $"Cần kiểm tra và xử lý email '{subject}' từ {from}",
            Deadline = null
        };
    }
}
