using System.Linq.Expressions;
using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.EmailOps.Commands;
using GOpsHub.Application.Features.EmailOps.Queries;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class CleanupRuleApprovalTests
{
    private readonly IRepository<CleanupRule> _rules = Substitute.For<IRepository<CleanupRule>>();
    private readonly IRepository<CleanupFeedback> _feedback = Substitute.For<IRepository<CleanupFeedback>>();
    private readonly IRepository<CleanupReview> _reviews = Substitute.For<IRepository<CleanupReview>>();
    private readonly IGmailService _gmail = Substitute.For<IGmailService>();
    private readonly ICleanupRuleApprovalStore _approval = Substitute.For<ICleanupRuleApprovalStore>();

    [Theory]
    [InlineData("(?i).*", "Sale", "Match-any sender regex")]
    [InlineData("[", "Sale", "Invalid regex")]
    [InlineData("team@example.com", "", "Sender-only rule")]
    public async Task UnsafePatternCannotBeApproved(string sender, string subject, string blocker)
    {
        var rule = new CleanupRule { Id = "r", SenderRegex = sender, SubjectRegex = subject, IsActive = false };
        Arrange(rule);
        var preview = await Preview().HandleAsync(new PreviewCleanupRuleQuery("r"));
        preview.Blockers.Should().Contain(x => x.Contains(blocker));
        var act = () => Approve().HandleAsync(new ApproveCleanupRuleCommand("r"));
        await act.Should().ThrowAsync<InvalidOperationException>();
        rule.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("@")]
    [InlineData("[\\s\\S]*")]
    public async Task BroadSenderCannotPassApprovalWithoutInboxSamples(string sender)
    {
        var rule = new CleanupRule { Id = "r", SenderRegex = sender, SubjectRegex = "Sale", IsActive = false };
        Arrange(rule);
        var preview = await Preview().HandleAsync(new PreviewCleanupRuleQuery("r"));
        preview.Blockers.Should().Contain(x => x.Contains("exact mailbox"));
        await FluentActions.Invoking(() => Approve().HandleAsync(new ApproveCleanupRuleCommand("r")))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MatchingKeepExampleBlocksApproval()
    {
        var rule = new CleanupRule { Id = "r", SenderRegex = "^team@example\\.com$", SubjectRegex = "Weekly", IsActive = false };
        Arrange(rule);
        _feedback.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<CleanupFeedback> {
            new() { Sender = "team@example.com", Subject = "Weekly report", Decision = CleanupDecision.Keep } });
        (await Preview().HandleAsync(new PreviewCleanupRuleQuery("r"))).Blockers.Should().Contain(x => x.Contains("Keep"));
        await FluentActions.Invoking(() => Approve().HandleAsync(new ApproveCleanupRuleCommand("r")))
            .Should().ThrowAsync<InvalidOperationException>();
        rule.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ApprovalRechecksLivePendingMailAfterPreview()
    {
        var rule = new CleanupRule { Id = "r", SenderRegex = "^team@example\\.com$", SubjectRegex = "Weekly" };
        Arrange(rule);
        (await Preview().HandleAsync(new PreviewCleanupRuleQuery("r"))).Blockers.Should().BeEmpty();
        _reviews.FindAsync(Arg.Any<Expression<Func<CleanupReview, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupReview> { new() { Sender = "team@example.com", Subject = "Weekly report", Status = CleanupReviewStatus.Pending } });
        await FluentActions.Invoking(() => Approve().HandleAsync(new ApproveCleanupRuleCommand("r")))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task NarrowVercelRuleCanBeApproved()
    {
        var rule = new CleanupRule { Id = "r", SenderRegex = "^notifications@vercel\\.com$",
            SubjectRegex = "(?i)failed deployment", IsActive = false };
        Arrange(rule);
        var approved = await Approve().HandleAsync(new ApproveCleanupRuleCommand("r"));
        approved.IsActive.Should().BeTrue();
        approved.ApprovalStatus.Should().Be(CleanupRuleApprovalStatus.Approved);
    }

    [Fact]
    public async Task ChangedRuleVersionCannotBeApproved()
    {
        var original = new CleanupRule { Id = "r", SenderRegex = "^notifications@vercel\\.com$",
            SubjectRegex = "Failed deployment", IsActive = false, UpdatedAt = DateTime.UtcNow.AddSeconds(-2) };
        var edited = new CleanupRule { Id = "r", SenderRegex = "^notifications@vercel\\.com$",
            SubjectRegex = "Sale", IsActive = false, UpdatedAt = DateTime.UtcNow };
        Arrange(original);
        _rules.GetByIdAsync("r", Arg.Any<CancellationToken>()).Returns(original, edited);

        await FluentActions.Invoking(() => Approve().HandleAsync(new ApproveCleanupRuleCommand("r")))
            .Should().ThrowAsync<InvalidOperationException>();
        await _approval.DidNotReceiveWithAnyArgs().TryApproveAsync(default!, default);
    }

    private void Arrange(CleanupRule rule)
    {
        _rules.GetByIdAsync("r", Arg.Any<CancellationToken>()).Returns(rule);
        _rules.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<CleanupRule> { rule });
        _feedback.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<CleanupFeedback>());
        _reviews.FindAsync(Arg.Any<Expression<Func<CleanupReview, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CleanupReview>());
        _gmail.GetEmailsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage>());
        _approval.TryApproveAsync(Arg.Any<CleanupRule>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                var value = call.Arg<CleanupRule>();
                value.IsActive = true;
                value.ApprovalStatus = CleanupRuleApprovalStatus.Approved;
                return value;
            });
    }

    private PreviewCleanupRuleQueryHandler Preview() => new(_rules, _feedback, _reviews, _gmail);
    private ApproveCleanupRuleCommandHandler Approve() => new(_rules, _feedback, _reviews, _gmail, _approval);
}
