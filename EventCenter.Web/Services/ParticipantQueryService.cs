using EventCenter.Web.Domain;
using EventCenter.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventCenter.Web.Services;

public class ParticipantQueryService
{
    private readonly EventCenterDbContext _context;

    public ParticipantQueryService(EventCenterDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns ALL registrations for an event including cancelled for admin participant table.
    /// Per user decision: admin sees full history with visual indicators for cancelled.
    /// </summary>
    public async Task<List<Registration>> GetParticipantsAsync(int eventId)
    {
        return await _context.Registrations
            .Include(r => r.EventCompany)
            .Include(r => r.RegistrationAgendaItems)
                .ThenInclude(rai => rai.AgendaItem)
            .Include(r => r.SelectedOptions)
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .ToListAsync();
    }
}
