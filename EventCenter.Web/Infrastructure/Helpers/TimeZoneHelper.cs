using TZConvert = TimeZoneConverter.TZConvert;

namespace EventCenter.Web.Infrastructure.Helpers;

public static class TimeZoneHelper
{
    private static readonly TimeZoneInfo CetTimeZone =
        TZConvert.GetTimeZoneInfo("Europe/Berlin"); // Works on Windows and Linux

    public static DateTime ConvertUtcToCet(DateTime utcDateTime)
    {
        // EF Core / SQL Server returns DateTimeKind.Unspecified — treat as UTC
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        else if (utcDateTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException("DateTime must be UTC", nameof(utcDateTime));

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, CetTimeZone);
    }

    public static DateTime ConvertCetToUtc(DateTime cetDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(cetDateTime, CetTimeZone);
    }

    /// <summary>
    /// Interprets a date as "end of day" in CET timezone and returns UTC equivalent.
    /// Example: "2026-03-15" becomes "2026-03-15 23:59:59.9999999 CET" converted to UTC.
    /// Handles DST transitions automatically.
    /// </summary>
    public static DateTime GetEndOfDayCetAsUtc(DateTime date)
    {
        // Take the date portion only
        var dateCet = date.Date;

        // Add one day and subtract one tick to get 23:59:59.9999999
        var endOfDayCet = dateCet.AddDays(1).AddTicks(-1);

        // Convert to UTC
        return ConvertCetToUtc(endOfDayCet);
    }

    /// <summary>
    /// Formats a UTC datetime for display in German format with CET timezone.
    /// </summary>
    public static string FormatDateTimeCet(DateTime utcDateTime, string format = "dd.MM.yyyy HH:mm")
    {
        var cet = ConvertUtcToCet(utcDateTime);
        return cet.ToString(format);
    }

    /// <summary>
    /// Checks if registration is still open based on inclusive deadline interpretation.
    /// </summary>
    public static bool IsRegistrationOpen(DateTime registrationDeadlineUtc)
    {
        var now = DateTime.UtcNow;

        // Deadline is interpreted as "end of that day in CET"
        // So convert the deadline UTC value to CET, get end of day, convert back to UTC
        var deadlineCet = ConvertUtcToCet(registrationDeadlineUtc);
        var deadlineEndOfDayUtc = GetEndOfDayCetAsUtc(deadlineCet);

        return now <= deadlineEndOfDayUtc;
    }
}
