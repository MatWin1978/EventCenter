using EventCenter.Web.Domain;
using EventCenter.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventCenter.Web.Services;

public class GuestooEventApiService(EventCenterDbContext db, IConfiguration config)
{
    public async Task<List<GuestooEventDto>> GetActiveEventsAsync()
    {
        var today = DateTime.Today;
        var baseUrl = config["Guestoo:BaseUrl"]?.TrimEnd('/') ?? string.Empty;

        var events = await db.Events
            .Where(e => e.IsPublished && e.EndDateUtc >= today)
            .OrderBy(e => e.StartDateUtc)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.Location,
                e.StartDateUtc,
                e.EndDateUtc,
                e.MaxCapacity,
                ActiveRegistrationCount = e.Registrations.Count(r => !r.IsCancelled)
            })
            .ToListAsync();

        return events.Select(e => new GuestooEventDto(
            Title: e.Title,
            Subtitle: null,
            Location: string.IsNullOrWhiteSpace(e.Location)
                ? null
                : new GuestooEventLocation(null, null, null, e.Location, null),
            StartDate: new DateTimeOffset(DateTime.SpecifyKind(e.StartDateUtc, DateTimeKind.Utc)),
            EndDate: new DateTimeOffset(DateTime.SpecifyKind(e.EndDateUtc, DateTimeKind.Utc)),
            AvailableSeats: e.MaxCapacity - e.ActiveRegistrationCount,
            ImageUrl: null,
            ShortDescription: e.Description,
            EventLink: string.IsNullOrEmpty(baseUrl) ? null : $"{baseUrl}/portal/events/{e.Id}"
        )).ToList();
    }
}
