using EventCenter.Web.Infrastructure.Helpers;
using Xunit;

namespace EventCenter.Tests;

public class TimeZoneHelperTests
{
    [Fact]
    public void ConvertUtcToCet_ReturnsCorrectCetTime()
    {
        // 2026-03-15 12:00:00 UTC = 2026-03-15 13:00:00 CET (winter time, UTC+1)
        var utc = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var cet = TimeZoneHelper.ConvertUtcToCet(utc);

        Assert.Equal(13, cet.Hour);
    }

    [Fact]
    public void ConvertUtcToCet_HandlesDstTransition()
    {
        // 2026-03-29 12:00:00 UTC = 2026-03-29 14:00:00 CEST (summer time starts, UTC+2)
        var utc = new DateTime(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc);
        var cet = TimeZoneHelper.ConvertUtcToCet(utc);

        // After DST transition, should be UTC+2
        Assert.Equal(14, cet.Hour);
    }

    [Fact]
    public void GetEndOfDayCetAsUtc_ReturnsEndOfDayInUtc()
    {
        // Input: 2026-03-15 (any time component ignored)
        var date = new DateTime(2026, 3, 15, 10, 30, 0);

        // Expected: 2026-03-15 23:59:59.9999999 CET converted to UTC
        var endOfDayUtc = TimeZoneHelper.GetEndOfDayCetAsUtc(date);

        // Convert back to CET to verify
        var endOfDayCet = TimeZoneHelper.ConvertUtcToCet(endOfDayUtc);

        Assert.Equal(15, endOfDayCet.Day);
        Assert.Equal(23, endOfDayCet.Hour);
        Assert.Equal(59, endOfDayCet.Minute);
    }

    [Fact]
    public void IsRegistrationOpen_ReturnsTrueBeforeDeadline()
    {
        // Deadline: tomorrow end of day
        var tomorrow = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(1).Date, DateTimeKind.Unspecified);
        var deadlineUtc = TimeZoneHelper.GetEndOfDayCetAsUtc(tomorrow);

        Assert.True(TimeZoneHelper.IsRegistrationOpen(deadlineUtc));
    }

    [Fact]
    public void IsRegistrationOpen_ReturnsFalseAfterDeadline()
    {
        // Deadline: yesterday end of day
        var yesterday = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1).Date, DateTimeKind.Unspecified);
        var deadlineUtc = TimeZoneHelper.GetEndOfDayCetAsUtc(yesterday);

        Assert.False(TimeZoneHelper.IsRegistrationOpen(deadlineUtc));
    }
}
