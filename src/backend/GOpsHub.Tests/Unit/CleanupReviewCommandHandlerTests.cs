using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.EmailOps.Commands;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class CleanupReviewCommandHandlerTests
{
    private readonly ICleanupReviewStore _store = Substitute.For<ICleanupReviewStore>();
    private readonly IRepository<CleanupFeedback> _feedback = Substitute.For<IRepository<CleanupFeedback>>();
    private readonly IRepository<EmailActionLog> _logs = Substitute.For<IRepository<EmailActionLog>>();
    private readonly IGmailService _gmail = Substitute.For<IGmailService>();

    private ResolveCleanupReviewCommandHandler Handler() => new(_store, _feedback, _logs, _gmail,
        Substitute.For<ILogger<ResolveCleanupReviewCommandHandler>>());

    [Fact]
    public async Task AReasonIsRequiredBeforeResolving()
    {
        var act = () => Handler().HandleAsync(new ResolveCleanupReviewCommand("r1", CleanupDecision.Trash, " "));
        await act.Should().ThrowAsync<ArgumentException>();
        await _gmail.DidNotReceive().TrashEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApprovedTrashMovesMessageAndRemembersReasonOnce()
    {
        var review = new CleanupReview { Id = "r1", EmailId = "m1", Sender = "news@example.com", Subject = "Digest", Status = CleanupReviewStatus.Pending };
        _store.GetByIdAsync("r1", Arg.Any<CancellationToken>()).Returns(review);
        _store.TryClaimTrashAsync("r1", Arg.Any<CancellationToken>()).Returns(true);
        _store.CompleteAsync("r1", CleanupReviewStatus.Trashed, "Never read it", Arg.Any<CancellationToken>())
            .Returns(review);

        await Handler().HandleAsync(new ResolveCleanupReviewCommand("r1", CleanupDecision.Trash, "Never read it"));

        await _gmail.Received(1).TrashEmailAsync("m1", Arg.Any<CancellationToken>());
        await _feedback.Received(1).CreateAsync(Arg.Is<CleanupFeedback>(f =>
            f.ReviewId == "r1" && f.Decision == CleanupDecision.Trash && f.Reason == "Never read it"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task KeepLeavesGmailUntouchedAndRemembersReason()
    {
        var review = new CleanupReview { Id = "r2", EmailId = "m2", Sender = "team@example.com", Status = CleanupReviewStatus.Pending };
        _store.GetByIdAsync("r2", Arg.Any<CancellationToken>()).Returns(review);
        _store.TryKeepAsync("r2", "Need team updates", Arg.Any<CancellationToken>()).Returns(true);

        await Handler().HandleAsync(new ResolveCleanupReviewCommand("r2", CleanupDecision.Keep, "Need team updates"));

        await _gmail.DidNotReceive().TrashEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _feedback.Received(1).CreateAsync(Arg.Is<CleanupFeedback>(f => f.ReviewId == "r2" && f.Decision == CleanupDecision.Keep), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ARepeatedTrashRequestDoesNotCallGmailAgain()
    {
        var review = new CleanupReview { Id = "r3", EmailId = "m3", Status = CleanupReviewStatus.Trashed };
        _store.GetByIdAsync("r3", Arg.Any<CancellationToken>()).Returns(review);

        await Handler().HandleAsync(new ResolveCleanupReviewCommand("r3", CleanupDecision.Trash, "Repeat"));

        await _gmail.DidNotReceive().TrashEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AStaleProcessingClaimChecksGmailBeforeRetrying()
    {
        var review = new CleanupReview { Id = "r4", EmailId = "m4", Status = CleanupReviewStatus.ProcessingTrash, UpdatedAt = DateTime.UtcNow.AddMinutes(-5) };
        _store.GetByIdAsync("r4", Arg.Any<CancellationToken>()).Returns(review);
        _gmail.GetEmailByIdAsync("m4", Arg.Any<CancellationToken>())
            .Returns(new EmailMessage { Id = "m4", Labels = new List<string> { "TRASH" } });
        _store.CompleteAsync("r4", CleanupReviewStatus.Trashed, "Old reason", Arg.Any<CancellationToken>()).Returns(review);

        await Handler().HandleAsync(new ResolveCleanupReviewCommand("r4", CleanupDecision.Trash, "Old reason"));

        await _gmail.DidNotReceive().TrashEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _store.Received(1).CompleteAsync("r4", CleanupReviewStatus.Trashed, "Old reason", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GmailResponseFailsAfterTrashButVerifiedTrashIsNotRepeated()
    {
        var review = new CleanupReview { Id = "r5", EmailId = "m5", Status = CleanupReviewStatus.Pending };
        _store.GetByIdAsync("r5", Arg.Any<CancellationToken>()).Returns(review);
        _store.TryClaimTrashAsync("r5", Arg.Any<CancellationToken>()).Returns(true);
        _gmail.TrashEmailAsync("m5", Arg.Any<CancellationToken>()).Returns(Task.FromException(new IOException("Response lost")));
        _gmail.GetEmailByIdAsync("m5", Arg.Any<CancellationToken>())
            .Returns(new EmailMessage { Id = "m5", Labels = new List<string> { "TRASH" } });
        _store.CompleteAsync("r5", CleanupReviewStatus.Trashed, "Noise", Arg.Any<CancellationToken>()).Returns(review);

        await Handler().HandleAsync(new ResolveCleanupReviewCommand("r5", CleanupDecision.Trash, "Noise"));

        await _gmail.Received(1).TrashEmailAsync("m5", Arg.Any<CancellationToken>());
        await _store.DidNotReceive().CompleteAsync("r5", CleanupReviewStatus.Pending, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConcurrentTrashClaimCannotRunGmailTwice()
    {
        var review = new CleanupReview { Id = "r6", EmailId = "m6", Status = CleanupReviewStatus.Pending };
        _store.GetByIdAsync("r6", Arg.Any<CancellationToken>()).Returns(review);
        _store.TryClaimTrashAsync("r6", Arg.Any<CancellationToken>()).Returns(false);

        var act = () => Handler().HandleAsync(new ResolveCleanupReviewCommand("r6", CleanupDecision.Trash, "Noise"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _gmail.DidNotReceive().TrashEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
