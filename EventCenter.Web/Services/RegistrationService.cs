using EventCenter.Web.Domain;
using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Domain.Enums;
using EventCenter.Web.Domain.Extensions;
using EventCenter.Web.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventCenter.Web.Services;

public class RegistrationService
{
    private readonly EventCenterDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        EventCenterDbContext context,
        IEmailSender emailSender,
        ILogger<RegistrationService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _logger = logger;
    }

    /// <summary>
    /// Registers a makler for an event with selected agenda items.
    /// Uses optimistic concurrency to prevent race conditions.
    /// </summary>
    public async Task<(bool Success, int? RegistrationId, string? ErrorMessage)> RegisterMaklerAsync(
        int eventId,
        string userEmail,
        string firstName,
        string lastName,
        string? phone,
        string? company,
        List<int> selectedAgendaItemIds)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Load event with related entities and row version for optimistic concurrency
            var evt = await _context.Events
                .Include(e => e.Registrations)
                .Include(e => e.AgendaItems)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (evt == null)
            {
                return (false, null, "Veranstaltung nicht gefunden.");
            }

            // Validate event state
            var eventState = evt.GetCurrentState();
            if (eventState != EventState.Public)
            {
                return (false, null, "Anmeldung nicht möglich - Frist abgelaufen.");
            }

            // Check capacity
            var currentRegistrationCount = evt.GetCurrentRegistrationCount();
            if (currentRegistrationCount >= evt.MaxCapacity)
            {
                return (false, null, "Veranstaltung ist ausgebucht.");
            }

            // Check for duplicate registration
            var existingRegistration = evt.Registrations
                .FirstOrDefault(r => r.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase));

            if (existingRegistration != null)
            {
                return (false, null, "Sie sind bereits für diese Veranstaltung angemeldet.");
            }

            // Validate selected agenda items
            if (selectedAgendaItemIds.Any())
            {
                var validAgendaItemIds = evt.AgendaItems
                    .Where(ai => ai.MaklerCanParticipate)
                    .Select(ai => ai.Id)
                    .ToHashSet();

                if (!selectedAgendaItemIds.All(id => validAgendaItemIds.Contains(id)))
                {
                    return (false, null, "Ungültige Agendapunkt-Auswahl.");
                }
            }

            // Create registration
            var registration = new Registration
            {
                EventId = eventId,
                Email = userEmail,
                FirstName = firstName,
                LastName = lastName,
                Phone = phone,
                Company = company,
                RegistrationType = RegistrationType.Makler,
                IsConfirmed = true,
                RegistrationDateUtc = DateTime.UtcNow,
                IsCancelled = false
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            // Create registration-agenda item links
            foreach (var agendaItemId in selectedAgendaItemIds)
            {
                var registrationAgendaItem = new RegistrationAgendaItem
                {
                    RegistrationId = registration.Id,
                    AgendaItemId = agendaItemId
                };
                _context.Set<RegistrationAgendaItem>().Add(registrationAgendaItem);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send confirmation email (fire-and-forget with error handling)
            _ = Task.Run(async () =>
            {
                try
                {
                    var registrationWithDetails = await GetRegistrationWithDetailsAsync(registration.Id);
                    if (registrationWithDetails != null)
                    {
                        await _emailSender.SendRegistrationConfirmationAsync(registrationWithDetails);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send registration confirmation email for registration {RegistrationId}", registration.Id);
                }
            });

            return (true, registration.Id, null);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Concurrency conflict during registration for event {EventId}", eventId);
            return (false, null, "Die Veranstaltung wurde zwischenzeitlich geändert. Bitte versuchen Sie es erneut.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during registration for event {EventId}", eventId);
            return (false, null, "Ein Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.");
        }
    }

    /// <summary>
    /// Retrieves a registration with all related details for display.
    /// </summary>
    public async Task<Registration?> GetRegistrationWithDetailsAsync(int registrationId)
    {
        return await _context.Registrations
            .Include(r => r.Event)
                .ThenInclude(e => e.AgendaItems)
            .Include(r => r.RegistrationAgendaItems)
                .ThenInclude(rai => rai.AgendaItem)
            .FirstOrDefaultAsync(r => r.Id == registrationId);
    }

    /// <summary>
    /// Calculates the total cost for selected agenda items.
    /// </summary>
    public decimal CalculateTotalCost(List<EventAgendaItem> selectedItems)
    {
        return selectedItems.Sum(item => item.CostForMakler);
    }

    /// <summary>
    /// Registers a guest (companion) for an event, linked to a broker's registration.
    /// Validates broker registration, companion limits, and agenda item participation rules.
    /// </summary>
    public async Task<(bool Success, int? GuestRegistrationId, string? ErrorMessage)> RegisterGuestAsync(
        int brokerRegistrationId,
        string salutation,
        string firstName,
        string lastName,
        string email,
        string relationshipType,
        List<int> selectedAgendaItemIds)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Load broker registration with event details
            var brokerRegistration = await _context.Registrations
                .Include(r => r.Event)
                    .ThenInclude(e => e.AgendaItems)
                .Include(r => r.Event.Registrations)
                .FirstOrDefaultAsync(r => r.Id == brokerRegistrationId);

            if (brokerRegistration == null)
            {
                return (false, null, "Makler-Anmeldung nicht gefunden.");
            }

            // Verify broker registration is not cancelled
            if (brokerRegistration.IsCancelled)
            {
                return (false, null, "Die Makler-Anmeldung wurde storniert.");
            }

            // Verify registration type is Makler
            if (brokerRegistration.RegistrationType != RegistrationType.Makler)
            {
                return (false, null, "Nur Makler können Begleitpersonen anmelden.");
            }

            var evt = brokerRegistration.Event;

            // Validate event state
            var eventState = evt.GetCurrentState();
            if (eventState != EventState.Public)
            {
                return (false, null, "Anmeldung nicht möglich - Frist abgelaufen.");
            }

            // Check companion limit
            var currentGuestCount = await _context.Registrations
                .CountAsync(r => r.ParentRegistrationId == brokerRegistrationId && !r.IsCancelled);

            if (currentGuestCount >= evt.MaxCompanions)
            {
                return (false, null, $"Maximale Anzahl Begleitpersonen erreicht ({evt.MaxCompanions}).");
            }

            // Validate selected agenda items - only items where GuestsCanParticipate is true
            if (selectedAgendaItemIds.Any())
            {
                var validAgendaItemIds = evt.AgendaItems
                    .Where(ai => ai.GuestsCanParticipate)
                    .Select(ai => ai.Id)
                    .ToHashSet();

                if (!selectedAgendaItemIds.All(id => validAgendaItemIds.Contains(id)))
                {
                    return (false, null, "Ungültige Agendapunkt-Auswahl für Begleitperson.");
                }
            }

            // Create guest registration
            var guestRegistration = new Registration
            {
                EventId = evt.Id,
                ParentRegistrationId = brokerRegistrationId,
                RegistrationType = RegistrationType.Guest,
                Salutation = salutation,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                RelationshipType = relationshipType,
                IsConfirmed = true,
                RegistrationDateUtc = DateTime.UtcNow,
                IsCancelled = false
            };

            _context.Registrations.Add(guestRegistration);
            await _context.SaveChangesAsync();

            // Create registration-agenda item links
            foreach (var agendaItemId in selectedAgendaItemIds)
            {
                var registrationAgendaItem = new RegistrationAgendaItem
                {
                    RegistrationId = guestRegistration.Id,
                    AgendaItemId = agendaItemId
                };
                _context.Set<RegistrationAgendaItem>().Add(registrationAgendaItem);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send confirmation email to broker (fire-and-forget with error handling)
            _ = Task.Run(async () =>
            {
                try
                {
                    var guestWithDetails = await GetRegistrationWithDetailsAsync(guestRegistration.Id);
                    var brokerWithDetails = await GetRegistrationWithDetailsAsync(brokerRegistrationId);
                    if (guestWithDetails != null && brokerWithDetails != null)
                    {
                        await _emailSender.SendGuestRegistrationConfirmationAsync(guestWithDetails, brokerWithDetails);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send guest registration confirmation email for guest registration {GuestRegistrationId}", guestRegistration.Id);
                }
            });

            return (true, guestRegistration.Id, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during guest registration for broker registration {BrokerRegistrationId}", brokerRegistrationId);
            return (false, null, "Ein Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.");
        }
    }

    /// <summary>
    /// Gets the count of non-cancelled guest registrations for a broker.
    /// </summary>
    public async Task<int> GetGuestCountAsync(int brokerRegistrationId)
    {
        return await _context.Registrations
            .CountAsync(r => r.ParentRegistrationId == brokerRegistrationId && !r.IsCancelled);
    }

    /// <summary>
    /// Gets all guest registrations for a broker with agenda item details.
    /// </summary>
    public async Task<List<Registration>> GetGuestRegistrationsAsync(int brokerRegistrationId)
    {
        return await _context.Registrations
            .Include(r => r.RegistrationAgendaItems)
                .ThenInclude(rai => rai.AgendaItem)
            .Where(r => r.ParentRegistrationId == brokerRegistrationId && !r.IsCancelled)
            .OrderBy(r => r.RegistrationDateUtc)
            .ToListAsync();
    }
}
