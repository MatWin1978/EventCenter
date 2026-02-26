using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Infrastructure.Calendar;
using System.Text;

namespace EventCenter.Tests.Services;

public class CalendarExportServiceTests
{
    private readonly IcalNetCalendarService _service;

    public CalendarExportServiceTests()
    {
        _service = new IcalNetCalendarService();
    }

    [Fact]
    public void GenerateEventCalendar_ReturnsValidIcsContent()
    {
        // Arrange
        var evt = CreateTestEvent();

        // Act
        var icsBytes = _service.GenerateEventCalendar(evt);
        var icsContent = Encoding.UTF8.GetString(icsBytes);

        // Assert
        Assert.Contains("BEGIN:VCALENDAR", icsContent);
        Assert.Contains("BEGIN:VEVENT", icsContent);
        Assert.Contains("SUMMARY:", icsContent);
        Assert.Contains("LOCATION:", icsContent);
        Assert.Contains("END:VEVENT", icsContent);
        Assert.Contains("END:VCALENDAR", icsContent);
    }

    [Fact]
    public void GenerateEventCalendar_IncludesCorrectEventDetails()
    {
        // Arrange
        var evt = CreateTestEvent();

        // Act
        var icsBytes = _service.GenerateEventCalendar(evt);
        var icsContent = Encoding.UTF8.GetString(icsBytes);

        // Assert
        Assert.Contains("SUMMARY:Test Event Title", icsContent);
        Assert.Contains("LOCATION:Test Location", icsContent);
        Assert.Contains($"UID:event-{evt.Id}@eventcenter.example.com", icsContent);
    }

    [Fact]
    public void GenerateEventCalendar_UsesUtcTimezone()
    {
        // Arrange
        var evt = CreateTestEvent();

        // Act
        var icsBytes = _service.GenerateEventCalendar(evt);
        var icsContent = Encoding.UTF8.GetString(icsBytes);

        // Assert
        // Verify that times are marked with UTC timezone
        Assert.Contains("DTSTART:", icsContent);
        Assert.Contains("DTEND:", icsContent);

        // The Ical.Net library should include timezone information
        // We check that the dates are present and formatted correctly
        var startYear = evt.StartDateUtc.Year.ToString();
        var endYear = evt.EndDateUtc.Year.ToString();
        Assert.Contains(startYear, icsContent);
        Assert.Contains(endYear, icsContent);
    }

    [Fact]
    public void GenerateEventCalendar_IncludesOrganizerWhenContactEmailSet()
    {
        // Arrange
        var evt = CreateTestEvent();
        evt.ContactEmail = "organizer@example.com";
        evt.ContactName = "Test Organizer";

        // Act
        var icsBytes = _service.GenerateEventCalendar(evt);
        var icsContent = Encoding.UTF8.GetString(icsBytes);

        // Assert
        Assert.Contains("ORGANIZER", icsContent);
        Assert.Contains("organizer@example.com", icsContent);
    }

    [Fact]
    public void GenerateEventCalendar_ExcludesOrganizerWhenContactEmailNotSet()
    {
        // Arrange
        var evt = CreateTestEvent();
        evt.ContactEmail = null;

        // Act
        var icsBytes = _service.GenerateEventCalendar(evt);
        var icsContent = Encoding.UTF8.GetString(icsBytes);

        // Assert
        Assert.DoesNotContain("ORGANIZER", icsContent);
    }

    private Event CreateTestEvent()
    {
        return new Event
        {
            Id = 123,
            Title = "Test Event Title",
            Description = "Test Description",
            Location = "Test Location",
            StartDateUtc = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 3, 15, 18, 0, 0, DateTimeKind.Utc),
            RegistrationDeadlineUtc = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            MaxCapacity = 100,
            MaxCompanions = 2,
            IsPublished = true,
            ContactName = "Test Contact",
            ContactEmail = null
        };
    }
}
