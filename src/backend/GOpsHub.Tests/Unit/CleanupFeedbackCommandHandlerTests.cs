using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.EmailOps.Commands;
using GOpsHub.Application.Features.EmailOps.Queries;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class CleanupFeedbackCommandHandlerTests
{
    private readonly IRepository<CleanupFeedback> _feedbackRepo = Substitute.For<IRepository<CleanupFeedback>>();
    private readonly IRepository<EmailActionLog> _actionLogRepo = Substitute.For<IRepository<EmailActionLog>>();
    private readonly IGmailService _gmailService = Substitute.For<IGmailService>();
    private readonly ILogger<SubmitCleanupFeedbackCommandHandler> _logger = Substitute.For<ILogger<SubmitCleanupFeedbackCommandHandler>>();

    [Fact]
    public async Task SubmitCleanupFeedbackCommand_ShouldCreateFeedback_TrashEmail_AndLogAction()
    {
        // Arrange
        var command = new SubmitCleanupFeedbackCommand(
            EmailId: "msg-001",
            Sender: "Shopee Vietnam <noreply@shopee.vn>",
            Subject: "Siêu Sale 10.10 Giảm 50%",
            Snippet: "Đừng bỏ lỡ mã giảm giá hôm nay...",
            Reason: "Quảng cáo lặp đi lặp lại không bao giờ mua",
            Tags: new List<string> { "Quảng cáo / Khuyến mãi" }
        );

        _feedbackRepo.CreateAsync(Arg.Any<CleanupFeedback>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<CleanupFeedback>());

        var handler = new SubmitCleanupFeedbackCommandHandler(_feedbackRepo, _actionLogRepo, _gmailService, _logger);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.EmailId.Should().Be("msg-001");
        result.Sender.Should().Be("Shopee Vietnam <noreply@shopee.vn>");
        result.SenderDomain.Should().Be("shopee.vn");
        result.Subject.Should().Be("Siêu Sale 10.10 Giảm 50%");
        result.Reason.Should().Be("Quảng cáo lặp đi lặp lại không bao giờ mua");
        result.Tags.Should().Contain("Quảng cáo / Khuyến mãi");

        await _feedbackRepo.Received(1).CreateAsync(Arg.Is<CleanupFeedback>(f =>
            f.EmailId == "msg-001" &&
            f.SenderDomain == "shopee.vn"), Arg.Any<CancellationToken>());

        await _gmailService.Received(1).TrashEmailAsync("msg-001", Arg.Any<CancellationToken>());

        await _actionLogRepo.Received(1).CreateAsync(Arg.Is<EmailActionLog>(l =>
            l.EmailId == "msg-001" &&
            l.Action == "UserTaughtTrash" &&
            l.Reason.Contains("Quảng cáo / Khuyến mãi")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCleanupFeedbackCommand_WhenFound_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var feedback = new CleanupFeedback { Id = "fb-123", EmailId = "msg-123" };
        _feedbackRepo.GetByIdAsync("fb-123", Arg.Any<CancellationToken>()).Returns(feedback);

        var handler = new DeleteCleanupFeedbackCommandHandler(_feedbackRepo);

        // Act
        var result = await handler.HandleAsync(new DeleteCleanupFeedbackCommand("fb-123"));

        // Assert
        result.Should().BeTrue();
        await _feedbackRepo.Received(1).DeleteAsync("fb-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCleanupFeedbackCommand_WhenNotFound_ShouldReturnFalse()
    {
        // Arrange
        _feedbackRepo.GetByIdAsync("fb-999", Arg.Any<CancellationToken>()).Returns((CleanupFeedback?)null);

        var handler = new DeleteCleanupFeedbackCommandHandler(_feedbackRepo);

        // Act
        var result = await handler.HandleAsync(new DeleteCleanupFeedbackCommand("fb-999"));

        // Assert
        result.Should().BeFalse();
        await _feedbackRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCleanupFeedbacksQuery_ShouldReturnOrderedLimitItems()
    {
        // Arrange
        var list = new List<CleanupFeedback>
        {
            new() { Id = "1", CreatedAt = DateTime.UtcNow.AddMinutes(-10), Reason = "Old" },
            new() { Id = "2", CreatedAt = DateTime.UtcNow, Reason = "Newest" },
            new() { Id = "3", CreatedAt = DateTime.UtcNow.AddMinutes(-5), Reason = "Middle" }
        };
        _feedbackRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(list);

        var handler = new GetCleanupFeedbacksQueryHandler(_feedbackRepo);

        // Act
        var result = await handler.HandleAsync(new GetCleanupFeedbacksQuery(Limit: 2));

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be("2");
        result[1].Id.Should().Be("3");
    }
}
