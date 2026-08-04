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

    public async Task<string> CreateEventAsync(string title, DateTime start, DateTime? end, string? location, string? description, CancellationToken ct = default)
    {
        var service = await GetCalendarClientAsync(ct);
        if (service == null) return string.Empty;

        var eventEndTime = end ?? start.AddHours(1);

        var calendarEvent = new Event
        {
            Summary = title,
            Location = location,
            Description = description,
            Start = new EventDateTime { DateTimeDateTimeOffset = start },
            End = new EventDateTime { DateTimeDateTimeOffset = eventEndTime }
        };

        var created = await service.Events.Insert(calendarEvent, "primary").ExecuteAsync(ct);
        return created.Id;
    }

    public async Task DeleteEventAsync(string eventId, CancellationToken ct = default)
    {
        var service = await GetCalendarClientAsync(ct);
        if (service == null) return;

        await service.Events.Delete("primary", eventId).ExecuteAsync(ct);
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetUpcomingEventsAsync(int days = 7, CancellationToken ct = default)
    {
        var service = await GetCalendarClientAsync(ct);
        if (service == null) return Array.Empty<CalendarEvent>();

        var request = service.Events.List("primary");
        request.TimeMinDateTimeOffset = DateTime.UtcNow;
        request.TimeMaxDateTimeOffset = DateTime.UtcNow.AddDays(days);
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = await request.ExecuteAsync(ct);
        if (events.Items == null) return Array.Empty<CalendarEvent>();

        return events.Items.Select(e => new CalendarEvent
        {
            Id = e.Id,
            Title = e.Summary,
            Start = e.Start.DateTimeDateTimeOffset?.UtcDateTime ?? DateTime.UtcNow,
            End = e.End.DateTimeDateTimeOffset?.UtcDateTime,
            Location = e.Location
        }).ToList();
    }

    public async Task<IReadOnlyList<CalendarBusySlot>> GetBusySlotsAsync(DateTime start, DateTime end, CancellationToken ct = default)
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
