using EventCenter.Web.Domain.Entities;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using System.Text;

namespace EventCenter.Web.Infrastructure.Calendar;

public class IcalNetCalendarService : ICalendarExportService
{
    public byte[] GenerateEventCalendar(Event evt)
    {
        var calendar = new Ical.Net.Calendar
        {
            Method = "PUBLISH",
            ProductId = "-//Veranstaltungscenter//EventCenter 1.0//DE"
        };

        var calendarEvent = new CalendarEvent
        {
            Summary = evt.Title,
            Description = evt.Description ?? string.Empty,
            Location = evt.Location,
            Start = new CalDateTime(evt.StartDateUtc, "UTC"),
            End = new CalDateTime(evt.EndDateUtc, "UTC"),
            Uid = $"event-{evt.Id}@eventcenter.example.com",
            Created = new CalDateTime(DateTime.UtcNow),
            LastModified = new CalDateTime(DateTime.UtcNow),
            Status = EventStatus.Confirmed
        };

        // Add organizer if contact email is provided
        if (!string.IsNullOrEmpty(evt.ContactEmail))
        {
            calendarEvent.Organizer = new Organizer($"mailto:{evt.ContactEmail}")
            {
                CommonName = evt.ContactName ?? "Veranstaltungscenter"
            };
        }

        calendar.Events.Add(calendarEvent);

        var serializer = new CalendarSerializer();
        var icsContent = serializer.SerializeToString(calendar);

        return Encoding.UTF8.GetBytes(icsContent ?? string.Empty);
    }
}
