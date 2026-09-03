using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.GoogleApis;

public class CalendarApiService : ICalendarService
{
    private readonly IRepository<AdminUser> _userRepo;
    private readonly ITokenEncryptionService _encryptionService;
    private readonly IGoogleTokenService _googleTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CalendarApiService> _logger;
    private readonly string _adminEmail;

    public CalendarApiService(
        IRepository<AdminUser> userRepo,
        ITokenEncryptionService encryptionService,
        IGoogleTokenService googleTokenService,
        IConfiguration configuration,
        ILogger<CalendarApiService> logger)
    {
        _userRepo = userRepo;
        _encryptionService = encryptionService;
        _googleTokenService = googleTokenService;
        _configuration = configuration;
        _logger = logger;
        _adminEmail = _configuration["ADMIN_EMAIL"] ?? "hnt.vn.vn@gmail.com";
    }

    private async Task<CalendarService?> GetCalendarClientAsync(CancellationToken ct = default)
    {
        var user = await _userRepo.FindOneAsync(u => u.Email == _adminEmail, ct);
        if (user == null || string.IsNullOrEmpty(user.GoogleAccessToken))
        {
            _logger.LogWarning("Admin user token not found for Calendar API calls.");
            return null;
        }

        var accessToken = _encryptionService.Decrypt(user.GoogleAccessToken);

        // Auto-refresh token if expired or about to expire
        if (user.GoogleTokenExpiresAt.HasValue && user.GoogleTokenExpiresAt.Value <= DateTime.UtcNow.AddMinutes(5))
        {
            if (!string.IsNullOrEmpty(user.GoogleRefreshToken))
            {
                try
                {
                    var refreshToken = _encryptionService.Decrypt(user.GoogleRefreshToken);
                    var newTokens = await _googleTokenService.RefreshAccessTokenAsync(refreshToken, ct);

                    accessToken = newTokens.AccessToken;
                    user.GoogleAccessToken = _encryptionService.Encrypt(newTokens.AccessToken);
                    user.GoogleTokenExpiresAt = DateTime.UtcNow.AddSeconds(newTokens.ExpiresInSeconds);
                    await _userRepo.UpdateAsync(user, ct);

                    _logger.LogInformation("Successfully refreshed Google access token for Calendar API.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh Google access token for Calendar.");
                    return null;
                }
            }
            else
            {
                _logger.LogWarning("Google access token expired and no refresh token available for Calendar.");
                return null;
            }
        }

        var credential = GoogleCredential.FromAccessToken(accessToken);

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "G-Ops Hub"
        });
    }

    public async Task<IReadOnlyList<GOpsHub.Application.Common.Interfaces.CalendarListEntry>> GetCalendarListAsync(CancellationToken ct = default)
    {
        var service = await GetCalendarClientAsync(ct);
        if (service == null) return Array.Empty<GOpsHub.Application.Common.Interfaces.CalendarListEntry>();

        var request = service.CalendarList.List();
        var response = await request.ExecuteAsync(ct);
        if (response.Items == null) return Array.Empty<GOpsHub.Application.Common.Interfaces.CalendarListEntry>();

        return response.Items.Select(c => new GOpsHub.Application.Common.Interfaces.CalendarListEntry
        {
            Id = c.Id,
            Summary = c.Summary,
            Description = c.Description,
            Primary = c.Primary ?? false,
            BackgroundColor = c.BackgroundColor,
            ForegroundColor = c.ForegroundColor
        }).ToList();
    }

    public async Task<string> CreateEventAsync(string title, DateTime start, DateTime? end, string? location, string? description, bool isPublic = true, string? calendarId = "primary", bool createMeetLink = false, IReadOnlyList<string>? attendees = null, string? colorId = null, bool isAllDay = false, int? reminderMinutes = null, CancellationToken ct = default)
    {
        var service = await GetCalendarClientAsync(ct);
        if (service == null) return string.Empty;

        var targetCalendar = string.IsNullOrWhiteSpace(calendarId) ? "primary" : calendarId;
        var eventEndTime = end ?? start.AddHours(1);

        var calendarEvent = new Event
        {
            Summary = title,
            Location = location,
            Description = description,
            Visibility = isPublic ? "public" : "private"
        };

        if (isAllDay)
        {
            calendarEvent.Start = new EventDateTime { Date = start.ToString("yyyy-MM-dd") };
            calendarEvent.End = new EventDateTime { Date = (end ?? start.AddDays(1)).ToString("yyyy-MM-dd") };
        }
        else
        {
            calendarEvent.Start = new EventDateTime { DateTimeDateTimeOffset = start };
            calendarEvent.End = new EventDateTime { DateTimeDateTimeOffset = eventEndTime };
        }

        if (!string.IsNullOrEmpty(colorId))
        {
            calendarEvent.ColorId = colorId;
        }

        if (attendees != null && attendees.Count > 0)
        {
            calendarEvent.Attendees = attendees
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(email => new EventAttendee { Email = email.Trim() })
                .ToList();
        }

        if (createMeetLink)
        {
            calendarEvent.ConferenceData = new ConferenceData
            {
                CreateRequest = new CreateConferenceRequest
                {
                    RequestId = Guid.NewGuid().ToString("N"),
                    ConferenceSolutionKey = new ConferenceSolutionKey { Type = "hangoutsMeet" }
                }
            };
        }

        if (reminderMinutes.HasValue)
        {
            calendarEvent.Reminders = new Event.RemindersData
            {
                UseDefault = false,
                Overrides = new List<EventReminder>
                {
                    new EventReminder { Method = "popup", Minutes = reminderMinutes.Value }
                }
            };
        }

        var insertRequest = service.Events.Insert(calendarEvent, targetCalendar);
        if (createMeetLink)
        {
            insertRequest.ConferenceDataVersion = 1;
        }

        var created = await insertRequest.ExecuteAsync(ct);
        return created.Id;
    }

    public async Task UpdateEventAsync(string eventId, string title, DateTime start, DateTime? end, string? location, string? description, bool isPublic = true, string? calendarId = "primary", bool createMeetLink = false, IReadOnlyList<string>? attendees = null, string? colorId = null, bool isAllDay = false, int? reminderMinutes = null, CancellationToken ct = default)
    {
        var service = await GetCalendarClientAsync(ct);
        if (service == null) return;

        var targetCalendar = string.IsNullOrWhiteSpace(calendarId) ? "primary" : calendarId;
        var existing = await service.Events.Get(targetCalendar, eventId).ExecuteAsync(ct);
        if (existing == null) return;

        existing.Summary = title;
        existing.Location = location;
        existing.Description = description;
        existing.Visibility = isPublic ? "public" : "private";

        if (isAllDay)
        {
            existing.Start = new EventDateTime { Date = start.ToString("yyyy-MM-dd") };
            existing.End = new EventDateTime { Date = (end ?? start.AddDays(1)).ToString("yyyy-MM-dd") };
        }
        else
        {
            existing.Start = new EventDateTime { DateTimeDateTimeOffset = start };
            existing.End = new EventDateTime { DateTimeDateTimeOffset = end ?? start.AddHours(1) };
        }

        if (!string.IsNullOrEmpty(colorId))
        {
            existing.ColorId = colorId;
        }

        if (attendees != null)
        {
            existing.Attendees = attendees
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(email => new EventAttendee { Email = email.Trim() })
                .ToList();
        }

        if (createMeetLink && existing.ConferenceData == null)
        {
            existing.ConferenceData = new ConferenceData
            {
                CreateRequest = new CreateConferenceRequest
                {
                    RequestId = Guid.NewGuid().ToString("N"),
                    ConferenceSolutionKey = new ConferenceSolutionKey { Type = "hangoutsMeet" }
                }
            };
        }

        if (reminderMinutes.HasValue)
        {
            existing.Reminders = new Event.RemindersData
            {
                UseDefault = false,
                Overrides = new List<EventReminder>
                {
                    new EventReminder { Method = "popup", Minutes = reminderMinutes.Value }
                }
            };
        }

        var updateRequest = service.Events.Update(existing, targetCalendar, eventId);
        if (createMeetLink)
        {
            updateRequest.ConferenceDataVersion = 1;
        }

        await updateRequest.ExecuteAsync(ct);
    }

    public async Task DeleteEventAsync(string eventId, string? calendarId = "primary", CancellationToken ct = default)
    {
        var service = await GetCalendarClientAsync(ct);
        if (service == null) return;

        var targetCalendar = string.IsNullOrWhiteSpace(calendarId) ? "primary" : calendarId;
        await service.Events.Delete(targetCalendar, eventId).ExecuteAsync(ct);
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetUpcomingEventsAsync(int days = 7, string? calendarId = "primary", CancellationToken ct = default)
    {
        return await GetEventsAsync(DateTime.UtcNow, DateTime.UtcNow.AddDays(days), calendarId, ct);
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(DateTime? timeMin = null, DateTime? timeMax = null, string? calendarId = "primary", CancellationToken ct = default)
    {
        var service = await GetCalendarClientAsync(ct);
        if (service == null) return Array.Empty<CalendarEvent>();

        var targetCalendar = string.IsNullOrWhiteSpace(calendarId) ? "primary" : calendarId;
        var request = service.Events.List(targetCalendar);
        request.TimeMinDateTimeOffset = timeMin ?? DateTime.UtcNow.AddDays(-30);
        request.TimeMaxDateTimeOffset = timeMax ?? DateTime.UtcNow.AddDays(30);
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = await request.ExecuteAsync(ct);
        if (events.Items == null) return Array.Empty<CalendarEvent>();

        return events.Items.Select(e => new CalendarEvent
        {
            Id = e.Id,
            Title = e.Summary,
            Start = e.Start.DateTimeDateTimeOffset?.UtcDateTime ?? (e.Start.Date != null ? DateTime.Parse(e.Start.Date) : DateTime.UtcNow),
            End = e.End?.DateTimeDateTimeOffset?.UtcDateTime ?? (e.End?.Date != null ? DateTime.Parse(e.End.Date) : (DateTime?)null),
            Location = e.Location,
            Description = e.Description,
            HtmlLink = e.HtmlLink ?? string.Empty,
            Visibility = string.Equals(e.Visibility, "private", StringComparison.OrdinalIgnoreCase) ? "private" : "public",
            MeetUrl = e.ConferenceData?.EntryPoints?.FirstOrDefault(x => x.EntryPointType == "video")?.Uri ?? e.HangoutLink,
            Attendees = e.Attendees?.Select(a => a.Email).Where(email => !string.IsNullOrEmpty(email)).ToList() ?? new List<string>(),
            ColorId = e.ColorId,
            IsAllDay = !string.IsNullOrEmpty(e.Start?.Date),
            ReminderMinutes = e.Reminders?.Overrides?.FirstOrDefault()?.Minutes
        }).ToList();
    }

    public async Task<IReadOnlyList<CalendarBusySlot>> GetBusySlotsAsync(DateTime start, DateTime end, string? calendarId = "primary", CancellationToken ct = default)
    {
        var service = await GetCalendarClientAsync(ct);
        if (service == null) return Array.Empty<CalendarBusySlot>();

        var freeBusyRequest = new FreeBusyRequest
        {
            TimeMinDateTimeOffset = start,
            TimeMaxDateTimeOffset = end,
            Items = new List<FreeBusyRequestItem> { new FreeBusyRequestItem { Id = "primary" } }
        };

        var response = await service.Freebusy.Query(freeBusyRequest).ExecuteAsync(ct);
        if (!response.Calendars.TryGetValue("primary", out var calendar) || calendar.Busy == null)
            return Array.Empty<CalendarBusySlot>();

        return calendar.Busy.Select(b => new CalendarBusySlot
        {
            Start = b.StartDateTimeOffset?.UtcDateTime ?? start,
            End = b.EndDateTimeOffset?.UtcDateTime ?? end
        }).ToList();
    }
}
