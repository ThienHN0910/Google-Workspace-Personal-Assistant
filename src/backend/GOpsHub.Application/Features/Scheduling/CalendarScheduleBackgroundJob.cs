using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Application.Features.Scheduling;

public class CalendarScheduleBackgroundJob
{
    private readonly IRepository<ExtractedSchedule> _scheduleRepo;
    private readonly IGmailService _gmailService;
    private readonly IAIService _aiService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CalendarScheduleBackgroundJob> _logger;

    public CalendarScheduleBackgroundJob(
        IRepository<ExtractedSchedule> scheduleRepo,
        IGmailService gmailService,
        IAIService aiService,
        INotificationService notificationService,
        ILogger<CalendarScheduleBackgroundJob> logger)
    {
        _scheduleRepo = scheduleRepo;
        _gmailService = gmailService;
        _aiService = aiService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task RunScheduleExtractionAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Calendar Schedule background extractor (UC03)...");

        try
        {
            var query = "is:unread (meeting OR interview OR \"lịch hẹn\" OR \"phỏng vấn\" OR flight OR \"vé máy bay\" OR appointment)";
            var emails = await _gmailService.GetEmailsAsync(query, 15, ct);

            if (emails == null || !emails.Any())
            {
                _logger.LogInformation("No new schedule candidate emails found.");
                return;
            }

            int foundCount = 0;
            foreach (var email in emails)
            {
                // Safeguard: Check if already extracted
                var existing = await _scheduleRepo.FindOneAsync(s => s.SourceEmailId == email.Id, ct);
                if (existing != null) continue;

                var aiResult = await _aiService.ExtractScheduleFromEmailAsync(email.Body ?? email.Snippet, ct);
                if (aiResult == null || string.IsNullOrWhiteSpace(aiResult.Title)) continue;

                // Human-in-the-loop: Alway mark as PendingConfirm so user retains decision control
                var schedule = new ExtractedSchedule
                {
                    SourceEmailId = email.Id,
                    SourceEmailSubject = email.Subject,
                    Title = aiResult.Title,
                    StartTime = aiResult.StartTime,
                    EndTime = aiResult.EndTime,
                    Location = aiResult.Location,
                    Description = aiResult.Description,
                    ConfidenceScore = aiResult.ConfidenceScore,
                    Status = ScheduleStatus.PendingConfirm
                };

                await _scheduleRepo.CreateAsync(schedule, ct);
                foundCount++;

                await _notificationService.SendNotificationAsync(
                    "📅 Phát hiện lịch hẹn mới chờ duyệt (UC03)",
                    $"AI đã trích xuất: \"{schedule.Title}\" ({schedule.StartTime:dd/MM/yyyy HH:mm}). Vui lòng vào mục Lịch để phê duyệt đẩy lên Google Calendar.",
                    "info",
                    ct);
            }

            _logger.LogInformation("Calendar Schedule scan finished. Found {Count} pending schedules.", foundCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Calendar Schedule background extraction.");
        }
    }
}
