using FluentAssertions;
using GOpsHub.Application.Features.DriveGuard;
using GOpsHub.Application.Features.Settings;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class SettingsCommandHandlerTests
{
    private readonly IRepository<AppConfiguration> _configRepo = Substitute.For<IRepository<AppConfiguration>>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly IRecurringJobManager _recurringJobManager = Substitute.For<IRecurringJobManager>();
    private readonly ILogger<UpdateSystemSettingsCommandHandler> _logger = Substitute.For<ILogger<UpdateSystemSettingsCommandHandler>>();

    [Fact]
    public async Task GetSystemSettingsQuery_ShouldReturnPopulatedDto()
    {
        // Arrange
        _configRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AppConfiguration>
            {
                new() { Key = "DriveGuardIntervalMinutes", Value = "10" },
                new() { Key = "EnableTelegram", Value = "true" },
                new() { Key = "TelegramChatId", Value = "123456" }
            });

        var handler = new GetSystemSettingsQueryHandler(_configRepo, _configuration);

        // Act
        var result = await handler.HandleAsync(new GetSystemSettingsQuery());

        // Assert
        result.DriveGuardIntervalMinutes.Should().Be(10);
        result.EnableTelegram.Should().BeTrue();
        result.TelegramChatId.Should().Be("123456");
    }

    [Fact]
    public async Task UpdateSystemSettingsCommand_ShouldRescheduleHangfireJobs()
    {
        // Arrange
        var handler = new UpdateSystemSettingsCommandHandler(_configRepo, _recurringJobManager, _logger);
        var settings = new SystemSettingsDto
        {
            DriveGuardIntervalMinutes = 7,
            BankTelemetryIntervalMinutes = 20,
            EmailCleanupIntervalHours = 12,
            CalendarExtractorIntervalHours = 2
        };

        // Act
        var result = await handler.HandleAsync(new UpdateSystemSettingsCommand(settings));

        // Assert
        result.Should().BeTrue();
        _recurringJobManager.Received().AddOrUpdate(
            "drive-guard-audit",
            Arg.Any<Hangfire.Common.Job>(),
            "*/7 * * * *",
            Arg.Any<RecurringJobOptions>());
    }

    [Fact]
    public async Task UpdateSettingsSectionCommand_WhenSectionIsJobs_ShouldRescheduleHangfireJobs()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UpdateSettingsSectionCommandHandler>>();
        var recurringJobManager = Substitute.For<IRecurringJobManager>();
        var handler = new UpdateSettingsSectionCommandHandler(_configRepo, recurringJobManager, logger);
        var settings = new SystemSettingsDto
        {
            DriveGuardIntervalMinutes = 15,
            BankTelemetryIntervalMinutes = 25,
            EmailCleanupIntervalHours = 6,
            CalendarExtractorIntervalHours = 3,
            BulkDeleteThreshold = 5
        };

        // Act
        var result = await handler.HandleAsync(new UpdateSettingsSectionCommand("jobs", settings));

        // Assert
        result.Should().BeTrue();
        recurringJobManager.Received().AddOrUpdate(
            "drive-guard-audit",
            Arg.Any<Hangfire.Common.Job>(),
            "*/15 * * * *",
            Arg.Any<RecurringJobOptions>());
    }

    [Fact]
    public async Task UpdateSettingsSectionCommand_WhenSectionIsAi_ShouldNotRescheduleHangfireJobs()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UpdateSettingsSectionCommandHandler>>();
        var recurringJobManager = Substitute.For<IRecurringJobManager>();
        var handler = new UpdateSettingsSectionCommandHandler(_configRepo, recurringJobManager, logger);
        var settings = new SystemSettingsDto
        {
            GeminiModel = "gemini-3.5-flash-lite",
            DefaultLanguage = "en",
            DefaultTone = "formal"
        };

        // Act
        var result = await handler.HandleAsync(new UpdateSettingsSectionCommand("ai", settings));

        // Assert
        result.Should().BeTrue();
        recurringJobManager.DidNotReceiveWithAnyArgs().AddOrUpdate(
            Arg.Any<string>(),
            Arg.Any<Hangfire.Common.Job>(),
            Arg.Any<string>(),
            Arg.Any<RecurringJobOptions>());
    }

    [Fact]
    public async Task UpdateSettingsSectionCommand_WhenSectionIsAlerts_ShouldNotRescheduleHangfireJobs()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UpdateSettingsSectionCommandHandler>>();
        var recurringJobManager = Substitute.For<IRecurringJobManager>();
        var handler = new UpdateSettingsSectionCommandHandler(_configRepo, recurringJobManager, logger);
        var settings = new SystemSettingsDto
        {
            EnableTelegram = true,
            TelegramBotToken = "123:ABC",
            TelegramChatId = "999"
        };

        // Act
        var result = await handler.HandleAsync(new UpdateSettingsSectionCommand("alerts", settings));

        // Assert
        result.Should().BeTrue();
        recurringJobManager.DidNotReceiveWithAnyArgs().AddOrUpdate(
            Arg.Any<string>(),
            Arg.Any<Hangfire.Common.Job>(),
            Arg.Any<string>(),
            Arg.Any<RecurringJobOptions>());
    }

    [Fact]
    public async Task UpdateSettingsSectionCommand_WhenSectionIsInvalid_ShouldThrowArgumentException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UpdateSettingsSectionCommandHandler>>();
        var recurringJobManager = Substitute.For<IRecurringJobManager>();
        var handler = new UpdateSettingsSectionCommandHandler(_configRepo, recurringJobManager, logger);

        // Act
        Func<Task> act = async () => await handler.HandleAsync(new UpdateSettingsSectionCommand("unknown-section", new SystemSettingsDto()));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Section không hợp lệ*");
    }

    [Fact]
    public async Task TestGeminiModelCommand_WhenModelIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var config = Substitute.For<IConfiguration>();
        var handler = new TestGeminiModelCommandHandler(config);

        // Act
        Func<Task> act = async () => await handler.HandleAsync(new TestGeminiModelCommand("   "));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Tên model không được để trống*");
    }

    private class MockHttpMessageHandler : System.Net.Http.HttpMessageHandler
    {
        private readonly System.Net.Http.HttpResponseMessage _response;
        public System.Net.Http.HttpRequestMessage? LastRequest { get; private set; }

        public MockHttpMessageHandler(System.Net.Http.HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_response);
        }
    }

    [Fact]
    public async Task TestGeminiModelCommand_WhenGeminiReturns200_ShouldReturnSuccess()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "fake-api-key"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var httpResponse = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new System.Net.Http.StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"pong\"}]}}]}")
        };
        var mockHandler = new MockHttpMessageHandler(httpResponse);
        var httpClient = new System.Net.Http.HttpClient(mockHandler);

        var handler = new TestGeminiModelCommandHandler(config, httpClient);

        // Act
        var result = await handler.HandleAsync(new TestGeminiModelCommand("gemini-3.5-flash-lite"));

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("gemini-3.5-flash-lite");
        mockHandler.LastRequest?.RequestUri?.ToString().Should().Contain("/models/gemini-3.5-flash-lite:generateContent");
    }

    [Fact]
    public async Task TestGeminiModelCommand_WhenGeminiReturns404_ShouldReturnFriendlyNotFoundMessage()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "fake-api-key"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var httpResponse = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
        {
            Content = new System.Net.Http.StringContent("{\"error\":{\"code\":404,\"message\":\"models/nonexistent is not found\"}}")
        };
        var mockHandler = new MockHttpMessageHandler(httpResponse);
        var httpClient = new System.Net.Http.HttpClient(mockHandler);

        var handler = new TestGeminiModelCommandHandler(config, httpClient);

        // Act
        var result = await handler.HandleAsync(new TestGeminiModelCommand("nonexistent-model"));

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("không tồn tại hoặc đã bị Google khai tử");
        result.Message.Should().Contain("HTTP 404");
    }
}


