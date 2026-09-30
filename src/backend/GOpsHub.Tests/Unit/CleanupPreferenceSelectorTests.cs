using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.EmailOps;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class CleanupPreferenceSelectorTests
{
    [Fact]
    public void OldRelevantVercelAndKeepExamplesBeatUnrelatedRecentFeedback()
    {
        var candidate = new EmailMessage { From = "Vercel <notifications@vercel.com>", Subject = "Failed deployment for alpha" };
        var oldTrash = new CleanupFeedback { Id = "a", Sender = "notifications@vercel.com", Subject = candidate.Subject,
            Decision = CleanupDecision.Trash, CreatedAt = DateTime.UtcNow.AddMonths(-2) };
        var oldKeep = new CleanupFeedback { Id = "b", Sender = "notifications@vercel.com", Subject = "Security alert",
            Decision = CleanupDecision.Keep, CreatedAt = DateTime.UtcNow.AddMonths(-1) };
        var recent = Enumerable.Range(0, 15).Select(i => new CleanupFeedback { Id = $"r{i}", Sender = "sales@example.com",
            Subject = "Sale", CreatedAt = DateTime.UtcNow.AddDays(-i) }).ToList();

        var selected = CleanupPreferenceSelector.SelectRelevant(candidate, recent.Concat(new[] { oldTrash, oldKeep }).ToList(), 2);

        selected.Select(x => x.Id).Should().Contain(new[] { "a", "b" });
    }

    [Fact]
    public void ChangedProjectInSubjectCannotAutoTrashEvenWithCitedPreference()
    {
        var candidate = new EmailMessage { From = "notifications@vercel.com", Subject = "Failed deployment for beta" };
        var cited = new CleanupFeedback { Id = "a", Sender = "notifications@vercel.com", Subject = "Failed deployment for alpha",
            Decision = CleanupDecision.Trash };
        var ai = new AICleanupDecision { EmailId = "mail", Outcome = AICleanupOutcome.Trash,
            FeedbackIds = new List<string> { "a" } };

        CleanupPreferenceSelector.CanAutoTrashKnownType(candidate, ai, new[] { cited }, Array.Empty<CleanupFeedback>())
            .Should().BeFalse();
    }

    [Fact]
    public void ExactKnownTypeCanAutoTrashOnlyWithoutConflictingKeep()
    {
        var candidate = new EmailMessage { From = "Vercel <notifications@vercel.com>", Subject = "Failed deployment for alpha" };
        var cited = new CleanupFeedback { Id = "a", Sender = "notifications@vercel.com", Subject = "failed  deployment for ALPHA",
            Decision = CleanupDecision.Trash };
        var ai = new AICleanupDecision { EmailId = "mail", Outcome = AICleanupOutcome.Trash,
            FeedbackIds = new List<string> { "a" } };

        CleanupPreferenceSelector.CanAutoTrashKnownType(candidate, ai, new[] { cited }, Array.Empty<CleanupFeedback>())
            .Should().BeTrue();
        CleanupPreferenceSelector.CanAutoTrashKnownType(candidate, ai, new[] { cited }, new[]
        {
            new CleanupFeedback { Sender = candidate.From, Subject = candidate.Subject, Decision = CleanupDecision.Keep }
        }).Should().BeFalse();
    }

    [Fact]
    public void SimilarSubjectOutranksNewerUnrelatedSubjectFromSameSender()
    {
        var candidate = new EmailMessage { From = "notifications@vercel.com", Subject = "Failed deployment for beta" };
        var olderRelated = new CleanupFeedback { Id = "related", Sender = candidate.From,
            Subject = "Failed deployment for alpha", CreatedAt = DateTime.UtcNow.AddDays(-10) };
        var newerUnrelated = new CleanupFeedback { Id = "unrelated", Sender = candidate.From,
            Subject = "New feature announcement", CreatedAt = DateTime.UtcNow };

        var selected = CleanupPreferenceSelector.SelectRelevant(candidate, new[] { newerUnrelated, olderRelated }, 1);

        selected.Single().Id.Should().Be("related");
    }
}
