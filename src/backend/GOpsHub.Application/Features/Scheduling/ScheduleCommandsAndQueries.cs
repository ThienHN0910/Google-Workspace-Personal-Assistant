using GOpsHub.Application.Common.CQRS;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Common.Models;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;

namespace GOpsHub.Application.Features.Scheduling;

public record ExtractScheduleFromEmailCommand(string GmailMessageId) : ICommand<ExtractedSchedule?>;

public class ExtractScheduleFromEmailCommandHandler : ICommandHandler<ExtractScheduleFromEmailCommand, ExtractedSchedule?>
{
    private readonly IRepository<ExtractedSchedule> _scheduleRepo;
    private readonly IGmailService _gmailService;
    private readonly IAIService _aiService;

    public ExtractScheduleFromEmailCommandHandler(
        IRepository<ExtractedSchedule> scheduleRepo,
        IGmailService gmailService,
        IAIService aiService)
    {
        _scheduleRepo = scheduleRepo;
        _gmailService = gmailService;
        _aiService = aiService;
    }

    public async Task<ExtractedSchedule?> HandleAsync(ExtractScheduleFromEmailCommand command, CancellationToken ct = default)
    {
        var email = await _gmailService.GetEmailByIdAsync(command.GmailMessageId, ct);
        if (email == null) return null;

        var aiResult = await _aiService.ExtractScheduleFromEmailAsync(email.Snippet, ct);
        if (aiResult == null) return null;

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
            Status = aiResult.ConfidenceScore >= 0.90 ? ScheduleStatus.AutoCreated : ScheduleStatus.PendingConfirm
        };

        return await _scheduleRepo.CreateAsync(schedule, ct);
    }
}

public record ConfirmScheduleCommand(string ScheduleId) : ICommand<ExtractedSchedule>;

public class ConfirmScheduleCommandHandler : ICommandHandler<ConfirmScheduleCommand, ExtractedSchedule>
{
    private readonly IRepository<ExtractedSchedule> _scheduleRepo;
    private readonly ICalendarService _calendarService;

    public ConfirmScheduleCommandHandler(
        IRepository<ExtractedSchedule> scheduleRepo,
        ICalendarService calendarService)
    {
        _scheduleRepo = scheduleRepo;
        _calendarService = calendarService;
    }

    public async Task<ExtractedSchedule> HandleAsync(ConfirmScheduleCommand command, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepo.GetByIdAsync(command.ScheduleId, ct);
        if (schedule == null)
            throw new KeyNotFoundException($"Extracted schedule {command.ScheduleId} not found.");

        var eventId = await _calendarService.CreateEventAsync(
            schedule.Title,
            schedule.StartTime,
            schedule.EndTime,
            schedule.Location,
            schedule.Description,
            ct);

        schedule.CalendarEventId = eventId;
        schedule.Status = ScheduleStatus.Confirmed;

        await _scheduleRepo.UpdateAsync(schedule, ct);
        return schedule;
    }
}

public record GetSchedulesQuery(int Page = 1, int PageSize = 10) : IQuery<PagedResult<ExtractedSchedule>>;

public class GetSchedulesQueryHandler : IQueryHandler<GetSchedulesQuery, PagedResult<ExtractedSchedule>>
{
    private readonly IRepository<ExtractedSchedule> _scheduleRepo;

    public GetSchedulesQueryHandler(IRepository<ExtractedSchedule> scheduleRepo)
    {
        _scheduleRepo = scheduleRepo;
    }

    public async Task<PagedResult<ExtractedSchedule>> HandleAsync(GetSchedulesQuery query, CancellationToken ct = default)
    {
        var (items, total) = await _scheduleRepo.GetPagedAsync(
            null,
            query.Page,
            query.PageSize,
            x => x.StartTime,
            true,
            ct);

        return new PagedResult<ExtractedSchedule>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
