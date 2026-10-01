using FluentAssertions;
using GOpsHub.API.Controllers;
using GOpsHub.Application.Common.Models;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class BackgroundJobsControllerTests
{
    private readonly IRecurringJobManager _recurringJobManager = Substitute.For<IRecurringJobManager>();
    private readonly BackgroundJobsController _controller;

    public BackgroundJobsControllerTests()
    {
        _controller = new BackgroundJobsController(_recurringJobManager);
    }

    [Fact]
    public void GetJobs_WhenCalled_ReturnsOkWithPredefinedJobInfoList()
    {
        // Act
        var result = _controller.GetJobs();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<JobInfoDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data.Should().HaveCountGreaterOrEqualTo(4);

        var jobIds = apiResponse.Data.Select(j => j.Id).ToList();
        jobIds.Should().Contain(new[] { "drive-guard-audit", "email-cleanup", "bank-telemetry", "calendar-extractor" });
    }

    [Fact]
    public void TriggerJob_WhenSuccessful_CallsRecurringJobManagerTrigger()
    {
        // Act
        var result = _controller.TriggerJob("drive-guard-audit");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().BeTrue();

        _recurringJobManager.Received(1).Trigger("drive-guard-audit");
    }

    [Fact]
    public void TriggerJob_WhenExceptionOccurs_ReturnsBadRequest()
    {
        // Arrange
        _recurringJobManager.When(m => m.Trigger("invalid-job"))
            .Do(_ => throw new InvalidOperationException("Job not found"));

        // Act
        var result = _controller.TriggerJob("invalid-job");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("Job not found");
    }

    [Fact]
    public void GetHistory_WhenCalled_ReturnsOkApiResponseWithHistoryList()
    {
        // Act
        var result = _controller.GetHistory(limit: 20);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<JobExecutionHistoryDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
    }
}
