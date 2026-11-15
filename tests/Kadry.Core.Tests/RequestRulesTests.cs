using Kadry.Core.Domain;
using Kadry.Core.Services;
using Xunit;

namespace Kadry.Core.Tests;

public class RequestRulesTests
{
    [Fact]
    public void DaysCount_IncludesBothDates()
    {
        Assert.Equal(3, RequestRules.DaysCount(new DateTime(2026, 1, 10), new DateTime(2026, 1, 12)));
    }

    [Fact]
    public void DaysCount_SameDay_EqualsOne()
    {
        var day = new DateTime(2026, 5, 20);
        Assert.Equal(1, RequestRules.DaysCount(day, day));
    }

    [Fact]
    public void DaysCount_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RequestRules.DaysCount(new DateTime(2026, 2, 10), new DateTime(2026, 2, 5)));
    }

    [Theory]
    [InlineData(RequestStatus.New, RequestStatus.Agreed, true)]
    [InlineData(RequestStatus.New, RequestStatus.Refused, true)]
    [InlineData(RequestStatus.New, RequestStatus.Approved, false)]
    [InlineData(RequestStatus.Agreed, RequestStatus.Approved, true)]
    [InlineData(RequestStatus.Agreed, RequestStatus.Refused, true)]
    [InlineData(RequestStatus.Approved, RequestStatus.Refused, false)]
    [InlineData(RequestStatus.Refused, RequestStatus.Agreed, false)]
    public void CanTransition_FollowsMatrix(RequestStatus from, RequestStatus to, bool expected)
    {
        Assert.Equal(expected, RequestRules.CanTransition(from, to));
    }

    [Fact]
    public void EnsureCanTransition_Forbidden_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RequestRules.EnsureCanTransition(RequestStatus.New, RequestStatus.Approved));
    }

    [Fact]
    public void RemainingDays_SubtractsRecordsOfYear()
    {
        var records = new[]
        {
            new VacationRecord { StartDate = new DateTime(2026, 3, 1), Days = 10 },
            new VacationRecord { StartDate = new DateTime(2026, 9, 1), Days = 7 },
            new VacationRecord { StartDate = new DateTime(2025, 12, 30), Days = 5 }
        };

        Assert.Equal(11, RequestRules.RemainingDays(28, records, 2026));
        Assert.Equal(23, RequestRules.RemainingDays(28, records, 2025));
    }

    [Fact]
    public void RemainingDays_UnlimitedType_ReturnsZero()
    {
        var records = Array.Empty<VacationRecord>();
        Assert.Equal(0, RequestRules.RemainingDays(0, records, 2026));
    }

    [Fact]
    public void OverlapsAny_DetectsIntersection()
    {
        var periods = new[]
        {
            (Start: new DateTime(2026, 6, 1), End: new DateTime(2026, 6, 10))
        };

        Assert.True(RequestRules.OverlapsAny(periods, new DateTime(2026, 6, 8), new DateTime(2026, 6, 12)));
        Assert.False(RequestRules.OverlapsAny(periods, new DateTime(2026, 6, 11), new DateTime(2026, 6, 15)));
    }
}
