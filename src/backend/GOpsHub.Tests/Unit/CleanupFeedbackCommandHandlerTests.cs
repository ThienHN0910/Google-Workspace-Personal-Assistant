using FluentAssertions;
using GOpsHub.Application.Features.EmailOps.Commands;
using GOpsHub.Application.Features.EmailOps.Queries;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using GOpsHub.Domain.Enums;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class CleanupFeedbackCommandHandlerTests
{
    private readonly IRepository<CleanupFeedback> _feedbackRepo = Substitute.For<IRepository<CleanupFeedback>>();
    private readonly IRepository<CleanupReview> _reviews = Substitute.For<IRepository<CleanupReview>>();
    private readonly GOpsHub.Application.Common.CQRS.ICommandHandler<ResolveCleanupReviewCommand, CleanupReview> _resolver =
        Substitute.For<GOpsHub.Application.Common.CQRS.ICommandHandler<ResolveCleanupReviewCommand, CleanupReview>>();

    [Fact]
    public void LegacyFeedbackDefaultsToTrash()
    {
        var oldDocument = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "emailId", "old-message" },
            { "reason", "Previously approved deletion" }
        };

        var feedback = BsonSerializer.Deserialize<CleanupFeedback>(oldDocument);
        feedback.Decision.Should().Be(CleanupDecision.Trash);
    }

    [Fact]
    public async Task FeedbackRequiresWrittenReason()
    {
        var handler = new SubmitCleanupFeedbackCommandHandler(_feedbackRepo, _reviews, _resolver);
        var command = new SubmitCleanupFeedbackCommand("mail-1", "seller@example.com", "Sale", "Discount", "  ", new List<string> { "Promo" });

        var act = () => handler.HandleAsync(command);

        await act.Should().ThrowAsync<ArgumentException>();
        await _resolver.DidNotReceiveWithAnyArgs().HandleAsync(default!, default);
    }

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

        var review = new CleanupReview { Id = "review-001", EmailId = "msg-001", Status = CleanupReviewStatus.Pending };
        _reviews.CreateAsync(Arg.Any<CleanupReview>(), Arg.Any<CancellationToken>()).Returns(review);
        var feedback = new CleanupFeedback { EmailId = "msg-001", ReviewId = review.Id,
            Sender = command.Sender, SenderDomain = "shopee.vn", Subject = command.Subject,
            Reason = command.Reason, Tags = command.Tags! };
        _feedbackRepo.FindOneAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CleanupFeedback, bool>>>(),
            Arg.Any<CancellationToken>()).Returns(feedback);

        var handler = new SubmitCleanupFeedbackCommandHandler(_feedbackRepo, _reviews, _resolver);

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

        await _resolver.Received(1).HandleAsync(Arg.Is<ResolveCleanupReviewCommand>(x =>
            x.ReviewId == "review-001" && x.Decision == CleanupDecision.Trash && x.Reason == command.Reason),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GmailFailureDoesNotPersistManualTrashPreference()
    {
        var review = new CleanupReview { Id = "r-failed", EmailId = "m-failed", Status = CleanupReviewStatus.Pending };
        var store = Substitute.For<ICleanupReviewStore>();
        var logs = Substitute.For<IRepository<EmailActionLog>>();
        var gmail = Substitute.For<IGmailService>();
        _reviews.CreateAsync(Arg.Any<CleanupReview>(), Arg.Any<CancellationToken>()).Returns(review);
        store.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);
        store.TryClaimTrashAsync(review.Id, Arg.Any<CancellationToken>()).Returns(true);
        gmail.TrashEmailAsync(review.EmailId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("Gmail failed")));
        var resolver = new ResolveCleanupReviewCommandHandler(store, _feedbackRepo, logs, gmail,
            Substitute.For<ILogger<ResolveCleanupReviewCommandHandler>>());
        var handler = new SubmitCleanupFeedbackCommandHandler(_feedbackRepo, _reviews, resolver);

        await FluentActions.Invoking(() => handler.HandleAsync(new SubmitCleanupFeedbackCommand(
            "m-failed", "news@example.com", "Sale", "Snippet", "Not useful", null)))
            .Should().ThrowAsync<IOException>();

        await _feedbackRepo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
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
