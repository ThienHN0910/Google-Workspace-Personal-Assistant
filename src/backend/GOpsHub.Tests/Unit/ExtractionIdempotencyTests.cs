using System.Linq.Expressions;
using FluentAssertions;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Application.Features.Finance;
using GOpsHub.Application.Features.Scheduling;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using GOpsHub.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class ExtractionIdempotencyTests
{
    private readonly IGmailService _gmail = Substitute.For<IGmailService>();
    private readonly IAIService _ai = Substitute.For<IAIService>();

    [Fact]
    public async Task HighConfidenceScheduleStillWaitsForOwnerConfirmation()
    {
        var repo = Substitute.For<IRepository<ExtractedSchedule>>();
        repo.CreateAsync(Arg.Any<ExtractedSchedule>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<ExtractedSchedule>());
        _gmail.GetEmailByIdAsync("m1", Arg.Any<CancellationToken>())
            .Returns(new EmailMessage { Id = "m1", Subject = "Meeting", Body = "Tomorrow 9am" });
        _ai.ExtractScheduleFromEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AIScheduleResult { Title = "Meeting", StartTime = DateTime.UtcNow, ConfidenceScore = 0.99 });

        var result = await new ExtractScheduleFromEmailCommandHandler(repo, _gmail, _ai)
            .HandleAsync(new ExtractScheduleFromEmailCommand("m1"));

        result!.Status.Should().Be(ScheduleStatus.PendingConfirm);
        result.CalendarEventId.Should().BeNull();
    }

    [Fact]
    public async Task ExtractingTheSameEmailTwiceReturnsTheExistingSchedule()
    {
        var existing = new ExtractedSchedule { Id = "s1", SourceEmailId = "m1", Title = "Meeting" };
        var repo = Substitute.For<IRepository<ExtractedSchedule>>();
        repo.FindOneAsync(Arg.Any<Expression<Func<ExtractedSchedule, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(existing);
        _gmail.GetEmailByIdAsync("m1", Arg.Any<CancellationToken>())
            .Returns(new EmailMessage { Id = "m1" });

        var result = await new ExtractScheduleFromEmailCommandHandler(repo, _gmail, _ai)
            .HandleAsync(new ExtractScheduleFromEmailCommand("m1"));

        result.Should().BeSameAs(existing);
        await repo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await _ai.DidNotReceiveWithAnyArgs().ExtractScheduleFromEmailAsync(default!, default);
    }

    [Fact]
    public async Task ParsingAnAlreadyRecordedBankEmailDoesNotCreateASecondTransaction()
    {
        var existing = new Transaction { Id = "t1", SourceEmailId = "m1" };
        var repo = Substitute.For<IRepository<Transaction>>();
        repo.FindOneAsync(Arg.Any<Expression<Func<Transaction, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(existing);
        _gmail.GetEmailByIdAsync("m1", Arg.Any<CancellationToken>())
            .Returns(new EmailMessage { Id = "m1", Body = "Debit 100,000 VND" });

        var handler = new ParseTransactionEmailCommandHandler(repo, _gmail, _ai,
            Substitute.For<ISheetsService>(), Substitute.For<IDriveService>(),
            Substitute.For<IRepository<AppConfiguration>>());

        var result = await handler.HandleAsync(new ParseTransactionEmailCommand("m1", "VCB", "sheet"));

        result.Should().BeSameAs(existing);
        await repo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await _ai.DidNotReceiveWithAnyArgs().ParseTransactionEmailAsync(default!, default!, default);
    }
}
