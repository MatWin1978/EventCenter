namespace EventCenter.Web.Infrastructure.Calendar;

using EventCenter.Web.Domain.Entities;

public interface ICalendarExportService
{
    byte[] GenerateEventCalendar(Event evt);
}
