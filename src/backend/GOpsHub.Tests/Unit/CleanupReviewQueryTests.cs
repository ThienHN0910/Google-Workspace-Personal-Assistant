using System.Linq.Expressions;
using FluentAssertions;
using GOpsHub.Application.Features.EmailOps.Queries;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class CleanupReviewQueryTests
{
    [Fact]
    public async Task RecoverableTrashAndIncompleteKeepStayVisible()
    {
        var reviews = Substitute.For<IRepository<CleanupReview>>();
        var feedback = Substitute.For<IRepository<CleanupFeedback>>();
        reviews.FindAsync(Arg.Any<Expression<Func<CleanupReview, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupReview> {
                new() { Id = "r1", Status = CleanupReviewStatus.ProcessingTrash },
                new() { Id = "r2", Status = CleanupReviewStatus.Kept },
                new() { Id = "r3", Status = CleanupReviewStatus.Kept }
            });
        feedback.FindAsync(Arg.Any<Expression<Func<CleanupFeedback, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupFeedback> { new() { ReviewId = "r3", Decision = CleanupDecision.Keep } });

        var result = await new GetPendingCleanupReviewsQueryHandler(reviews, feedback)
            .HandleAsync(new GetPendingCleanupReviewsQuery());

        result.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { "r1", "r2" });
    }
}
