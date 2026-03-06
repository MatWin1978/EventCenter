using EventCenter.Web.Domain;
using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace EventCenter.Web.Services;

public class EventService
{
    private readonly EventCenterDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public EventService(EventCenterDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    /// <summary>
    /// Creates a new event and persists it to the database.
    /// </summary>
    public async Task<Event> CreateEventAsync(Event evt)
    {
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();
        return evt;
    }

    /// <summary>
    /// Retrieves an event by ID with all related entities loaded.
    /// </summary>
    public async Task<Event?> GetEventByIdAsync(int id)
    {
        return await _context.Events
            .Include(e => e.AgendaItems)
            .Include(e => e.EventOptions)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>
    /// Retrieves a list of events with optional filtering, sorting, and pagination.
    /// </summary>
    public async Task<List<Event>> GetEventsAsync(
        bool includePast,
        string? sortColumn,
        bool ascending,
        int page,
        int pageSize,
        EventType? typeFilter = null)
    {
        var query = _context.Events
            .Include(e => e.Registrations)
            .AsQueryable();

        // Filter by event type if specified
        if (typeFilter.HasValue)
        {
            query = query.Where(e => e.EventType == typeFilter.Value);
        }

        // Filter past events if requested
        if (!includePast)
        {
            query = query.Where(e => e.EndDateUtc >= DateTime.UtcNow);
        }

        // Apply sorting
        var sortCol = sortColumn ?? "StartDateUtc";
        query = sortCol switch
        {
            "Title" => ascending ? query.OrderBy(e => e.Title) : query.OrderByDescending(e => e.Title),
            "Location" => ascending ? query.OrderBy(e => e.Location) : query.OrderByDescending(e => e.Location),
            "StartDateUtc" => ascending ? query.OrderBy(e => e.StartDateUtc) : query.OrderByDescending(e => e.StartDateUtc),
            _ => query.OrderByDescending(e => e.StartDateUtc)
        };

        // Apply pagination
        query = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Returns the total count of events for pagination.
    /// </summary>
    public async Task<int> GetEventCountAsync(bool includePast, EventType? typeFilter = null)
    {
        var query = _context.Events.AsQueryable();

        // Filter by event type if specified
        if (typeFilter.HasValue)
        {
            query = query.Where(e => e.EventType == typeFilter.Value);
        }

        if (!includePast)
        {
            query = query.Where(e => e.EndDateUtc >= DateTime.UtcNow);
        }

        return await query.CountAsync();
    }

    /// <summary>
    /// Updates an existing event. Does not update related entities (AgendaItems, EventOptions).
    /// </summary>
    public async Task UpdateEventAsync(Event evt)
    {
        _context.Events.Update(evt);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Publishes an event by setting IsPublished to true.
    /// For webinars, requires ExternalRegistrationUrl to be set.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> PublishEventAsync(int eventId)
    {
        var evt = await _context.Events.FindAsync(eventId);
        if (evt == null)
        {
            return (false, "Veranstaltung nicht gefunden.");
        }

        if (evt.EventType == EventType.Webinar &&
            string.IsNullOrWhiteSpace(evt.ExternalRegistrationUrl))
        {
            return (false, "Webinar kann nicht veröffentlicht werden ohne externe Anmelde-URL.");
        }

        evt.IsPublished = true;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Unpublishes an event. Blocked if the event has registrations.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> UnpublishEventAsync(int eventId)
    {
        var evt = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (evt == null)
        {
            return (false, "Veranstaltung nicht gefunden.");
        }

        if (evt.Registrations.Any())
        {
            return (false, "Veranstaltung kann nicht zurückgezogen werden, da bereits Anmeldungen existieren.");
        }

        evt.IsPublished = false;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Deletes an event if it has no registrations. Cascade deletes AgendaItems and EventOptions.
    /// </summary>
    public async Task<bool> DeleteEventAsync(int eventId)
    {
        var evt = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (evt == null)
        {
            return false;
        }

        if (evt.Registrations.Any())
        {
            return false;
        }

        _context.Events.Remove(evt);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Duplicates an event with all AgendaItems and EventOptions. New event starts as draft with dates shifted by 1 month.
    /// </summary>
    public async Task<Event> DuplicateEventAsync(int sourceEventId)
    {
        var source = await _context.Events
            .Include(e => e.AgendaItems)
            .Include(e => e.EventOptions)
            .FirstOrDefaultAsync(e => e.Id == sourceEventId);

        if (source == null)
        {
            throw new InvalidOperationException("Source event not found.");
        }

        // Calculate time shift (1 month)
        var timeShift = TimeSpan.FromDays(30);

        var duplicate = new Event
        {
            Title = $"{source.Title} (Kopie)",
            Description = source.Description,
            Location = source.Location,
            StartDateUtc = source.StartDateUtc.Add(timeShift),
            EndDateUtc = source.EndDateUtc.Add(timeShift),
            RegistrationDeadlineUtc = source.RegistrationDeadlineUtc.Add(timeShift),
            MaxCapacity = source.MaxCapacity,
            MaxCompanions = source.MaxCompanions,
            IsPublished = false,
            ContactName = source.ContactName,
            ContactEmail = source.ContactEmail,
            ContactPhone = source.ContactPhone,
            DocumentPaths = new List<string>(source.DocumentPaths),
            EventType = source.EventType,
            ExternalRegistrationUrl = source.ExternalRegistrationUrl
        };

        // Copy AgendaItems with relative time offsets preserved
        foreach (var agendaItem in source.AgendaItems)
        {
            var relativeStart = agendaItem.StartDateTimeUtc - source.StartDateUtc;
            var relativeEnd = agendaItem.EndDateTimeUtc - source.StartDateUtc;

            duplicate.AgendaItems.Add(new EventAgendaItem
            {
                Title = agendaItem.Title,
                Description = agendaItem.Description,
                StartDateTimeUtc = duplicate.StartDateUtc.Add(relativeStart),
                EndDateTimeUtc = duplicate.StartDateUtc.Add(relativeEnd),
                CostForMakler = agendaItem.CostForMakler,
                CostForGuest = agendaItem.CostForGuest,
                IsMandatory = agendaItem.IsMandatory,
                MaxParticipants = agendaItem.MaxParticipants,
                MaklerCanParticipate = agendaItem.MaklerCanParticipate,
                GuestsCanParticipate = agendaItem.GuestsCanParticipate
            });
        }

        // Copy EventOptions
        foreach (var option in source.EventOptions)
        {
            duplicate.EventOptions.Add(new EventOption
            {
                Name = option.Name,
                Description = option.Description,
                Price = option.Price,
                MaxQuantity = option.MaxQuantity
            });
        }

        _context.Events.Add(duplicate);
        await _context.SaveChangesAsync();

        return duplicate;
    }

    /// <summary>
    /// Deletes an event option if it has no bookings (registrations).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> DeleteEventOptionAsync(int optionId)
    {
        var option = await _context.EventOptions
            .Include(o => o.Registrations)
            .FirstOrDefaultAsync(o => o.Id == optionId);

        if (option == null)
        {
            return (false, "Zusatzoption nicht gefunden.");
        }

        if (option.Registrations.Any())
        {
            return (false, "Diese Zusatzoption kann nicht gelöscht werden, da bereits Buchungen existieren.");
        }

        _context.EventOptions.Remove(option);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Saves a document to the filesystem and returns the relative path.
    /// </summary>
    public async Task<string> SaveDocumentAsync(int eventId, string fileName, Stream content)
    {
        // Sanitize filename
        var sanitizedFileName = Path.GetFileName(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";

        // Create directory path
        var relativeDir = $"uploads/events/{eventId}";
        var absoluteDir = Path.Combine(_environment.WebRootPath, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        // Save file
        var filePath = Path.Combine(absoluteDir, uniqueFileName);
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(fileStream);
        }

        // Return relative path with leading slash
        return $"/{relativeDir}/{uniqueFileName}";
    }

    /// <summary>
    /// Deletes a document from the filesystem. Validates path to prevent traversal attacks.
    /// </summary>
    public Task DeleteDocumentAsync(string relativePath)
    {
        // Resolve canonical paths to prevent path traversal (e.g. /uploads/events/../../etc/passwd)
        var uploadRoot = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "uploads", "events"));
        var absolutePath = Path.GetFullPath(Path.Combine(
            _environment.WebRootPath,
            relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
        ));

        if (!absolutePath.StartsWith(uploadRoot + Path.DirectorySeparatorChar))
        {
            throw new InvalidOperationException("Invalid file path.");
        }

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves published events for public display with optional filtering.
    /// Returns events sorted by start date ascending (nearest first).
    /// Includes Registrations and AgendaItems for display logic.
    /// </summary>
    public async Task<List<Event>> GetPublicEventsAsync(
        string? searchTerm,
        DateTime? startDateFrom,
        DateTime? startDateTo,
        string? userEmail)
    {
        var query = _context.Events
            .Include(e => e.Registrations)
            .Include(e => e.AgendaItems)
            .Where(e => e.IsPublished)
            .AsQueryable();

        // Filter by search term (title or location)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(e =>
                e.Title.ToLower().Contains(searchLower) ||
                e.Location.ToLower().Contains(searchLower));
        }

        // Filter by date range
        if (startDateFrom.HasValue)
        {
            query = query.Where(e => e.StartDateUtc >= startDateFrom.Value);
        }

        if (startDateTo.HasValue)
        {
            query = query.Where(e => e.StartDateUtc <= startDateTo.Value);
        }

        // Sort by start date ascending (nearest events first)
        query = query.OrderBy(e => e.StartDateUtc);

        return await query.ToListAsync();
    }
}
