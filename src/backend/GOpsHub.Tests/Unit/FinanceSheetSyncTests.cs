using FluentAssertions;
using GOpsHub.Application.Features.Finance;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Enums;
using Xunit;

namespace GOpsHub.Tests.Unit;

public class FinanceSheetSyncTests
{
    [Fact]
    public void BuildRow_IncludesBalanceAfterAsTwelfthColumn()
    {
        var row = FinanceSheetSync.BuildRow(new Transaction
        {
            TransactionCode = "FT1",
            TransactionDate = new DateTime(2026, 11, 1, 8, 30, 0),
            BankName = "VCB",
            TransactionType = TransactionType.Credit,
            Amount = 100_000,
            BalanceAfter = 500_000
        });

        row.Should().HaveCount(12);
        row[3].Should().Be("+ Nhận");
        row[11].Should().Be(500_000m);
    }

    [Theory]
    [InlineData(null, "BaoCaoTaiChinh_2026_11")]
    [InlineData("Finance_{yyyy-MM}", "Finance_2026-11")]
    [InlineData("{yyyy}/{MM}", "2026/11")]
    public void FormatFileNamePattern_ExpandsDateTokens(string? pattern, string expected)
    {
        FinanceSheetSync.FormatFileNamePattern(pattern, new DateTime(2026, 11, 1)).Should().Be(expected);
    }
}
