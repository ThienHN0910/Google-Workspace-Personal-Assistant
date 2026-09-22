using System.Net;
using System.Text;
using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using GOpsHub.Infrastructure.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class GeminiAIServiceTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public HttpRequestMessage? LastRequest { get; private set; }

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_response);
        }
    }

    [Fact]
    public async Task CallGeminiApiAsync_WhenHttp429_ShouldSendCriticalTelegramNotification()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "fake-api-key",
            ["Gemini:Model"] = "gemini-3.5-flash-lite"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var logger = Substitute.For<ILogger<GeminiAIService>>();
        var rateLimiter = new GeminiRateLimiter();
        var usageTracker = Substitute.For<IAiUsageTracker>();
        var notificationService = Substitute.For<INotificationService>();

        var httpResponse = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{\"error\": {\"code\": 429, \"message\": \"Resource has been exhausted\"}}")
        };
        var httpClient = new HttpClient(new MockHttpMessageHandler(httpResponse));

        var service = new GeminiAIService(config, logger, rateLimiter, usageTracker, notificationService, httpClient);

        // Act
        var result = await service.GenerateEmailReplyAsync("Test content");

        // Assert
        result.ConfidenceScore.Should().Be(0); // Error fallback
        await notificationService.Received(1).SendNotificationAsync(
            Arg.Is<string>(t => t.Contains("429")),
            Arg.Is<string>(m => m.Contains("Too Many Requests")),
            "critical",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CallGeminiApiAsync_WhenInputTokensReach200k_ShouldSendWarningNotification()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "fake-api-key",
            ["Gemini:Model"] = "gemini-3.5-flash-lite"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var logger = Substitute.For<ILogger<GeminiAIService>>();
        var rateLimiter = new GeminiRateLimiter();
        var usageTracker = Substitute.For<IAiUsageTracker>();
        var notificationService = Substitute.For<INotificationService>();

        var validResponseBody = @"{
            ""candidates"": [{
                ""content"": {
                    ""parts"": [{ ""text"": ""<p>Phản hồi thử nghiệm</p>"" }]
                }
            }],
            ""usageMetadata"": {
                ""promptTokenCount"": 205000,
                ""candidatesTokenCount"": 500,
                ""totalTokenCount"": 205500
            }
        }";

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(validResponseBody, Encoding.UTF8, "application/json")
        };
        var httpClient = new HttpClient(new MockHttpMessageHandler(httpResponse));

        var service = new GeminiAIService(config, logger, rateLimiter, usageTracker, notificationService, httpClient);

        // Act
        var result = await service.GenerateEmailReplyAsync("Prompt dài");

        // Assert
        result.DraftContent.Should().Contain("Phản hồi thử nghiệm");
        await notificationService.Received(1).SendNotificationAsync(
            Arg.Is<string>(t => t.Contains("Input Token")),
            Arg.Is<string>(m => m.Contains("200k")),
            "warning",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CallGeminiApiAsync_WhenInputTokensUnder200k_ShouldNotSendWarningNotification()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "fake-api-key",
            ["Gemini:Model"] = "gemini-3.5-flash-lite"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var logger = Substitute.For<ILogger<GeminiAIService>>();
        var rateLimiter = new GeminiRateLimiter();
        var usageTracker = Substitute.For<IAiUsageTracker>();
        var notificationService = Substitute.For<INotificationService>();

        var validResponseBody = @"{
            ""candidates"": [{
                ""content"": {
                    ""parts"": [{ ""text"": ""<p>Bình thường</p>"" }]
                }
            }],
            ""usageMetadata"": {
                ""promptTokenCount"": 5000,
                ""candidatesTokenCount"": 100,
                ""totalTokenCount"": 5100
            }
        }";

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(validResponseBody, Encoding.UTF8, "application/json")
        };
        var httpClient = new HttpClient(new MockHttpMessageHandler(httpResponse));

        var service = new GeminiAIService(config, logger, rateLimiter, usageTracker, notificationService, httpClient);

        // Act
        var result = await service.GenerateEmailReplyAsync("Prompt ngắn");

        // Assert
        result.DraftContent.Should().Contain("Bình thường");
        await notificationService.DidNotReceiveWithAnyArgs().SendNotificationAsync(
            default!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task CallGeminiApiAsync_WhenDatabaseHasCustomModel_ShouldUseDatabaseModel()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "fake-api-key",
            ["Gemini:Model"] = "gemini-3.5-flash-lite"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var logger = Substitute.For<ILogger<GeminiAIService>>();
        var rateLimiter = new GeminiRateLimiter();
        var usageTracker = Substitute.For<IAiUsageTracker>();
        var notificationService = Substitute.For<INotificationService>();
        var configRepo = Substitute.For<IRepository<AppConfiguration>>();

        configRepo.FindOneAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<AppConfiguration, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new AppConfiguration { Key = "GeminiModel", Value = "gemini-custom-pro" });

        var validResponseBody = @"{
            ""candidates"": [{
                ""content"": {
                    ""parts"": [{ ""text"": ""<p>Phản hồi từ model động</p>"" }]
                }
            }]
        }";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(validResponseBody, Encoding.UTF8, "application/json")
        };
        var mockHandler = new MockHttpMessageHandler(httpResponse);
        var httpClient = new HttpClient(mockHandler);

        var service = new GeminiAIService(config, logger, rateLimiter, usageTracker, notificationService, httpClient, configRepo);

        // Act
        var result = await service.GenerateEmailReplyAsync("Prompt kiểm tra model động");

        // Assert
        result.DraftContent.Should().Contain("Phản hồi từ model động");
        mockHandler.LastRequest?.RequestUri?.ToString().Should().Contain("/models/gemini-custom-pro:generateContent");
    }
}

