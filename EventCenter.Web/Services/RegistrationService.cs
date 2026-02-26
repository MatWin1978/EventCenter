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
}
