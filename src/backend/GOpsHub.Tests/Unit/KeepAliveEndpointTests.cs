using FluentAssertions;
using GOpsHub.API.Controllers;
using GOpsHub.Application.Common.Models;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class KeepAliveEndpointTests
{
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly IRecurringJobManager _recurringJobManager = Substitute.For<IRecurringJobManager>();
    private readonly IRepository<AppConfiguration> _configRepo = Substitute.For<IRepository<AppConfiguration>>();

    [Fact]
    public async Task KeepAlive_ShouldReturn200Ok_WhenNoKeyConfigured()
    {
        // Arrange
        _configuration["KEEP_ALIVE_KEY"].Returns((string?)null);
        var controller = new PublicController(_configuration, _recurringJobManager, _configRepo);

        // Act
        var result = await controller.KeepAlive(null, null, null, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var apiResponse = okResult.Value as ApiResponse<object>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task KeepAlive_ShouldReturn401_WhenKeyIsConfiguredAndInvalidKeyProvided()
    {
        // Arrange
        _configuration["KEEP_ALIVE_KEY"].Returns("secret_token_123");
        var controller = new PublicController(_configuration, _recurringJobManager, _configRepo);

        // Act
        var result = await controller.KeepAlive(null, "wrong_token", null, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objResult = (ObjectResult)result;
        objResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task KeepAlive_ShouldReturn200_WhenValidKeyProvidedViaQueryOrHeader()
    {
        // Arrange
        _configuration["KEEP_ALIVE_KEY"].Returns("secret_token_123");
        var controller = new PublicController(_configuration, _recurringJobManager, _configRepo);

        // Act: via query
        var resultQuery = await controller.KeepAlive(null, "secret_token_123", null, CancellationToken.None);
        // Act: via header
        var resultHeader = await controller.KeepAlive("secret_token_123", null, null, CancellationToken.None);

        // Assert
        resultQuery.Should().BeOfType<OkObjectResult>();
        resultHeader.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task KeepAlive_ShouldTriggerJob_WhenTriggerParameterProvided()
    {
        // Arrange
        _configuration["KEEP_ALIVE_KEY"].Returns((string?)null);
        var controller = new PublicController(_configuration, _recurringJobManager, _configRepo);

        // Act
        var result = await controller.KeepAlive(null, null, "email-cleanup", CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _recurringJobManager.Received(1).Trigger("email-cleanup");
    }
}
