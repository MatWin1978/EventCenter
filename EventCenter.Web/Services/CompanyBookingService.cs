using System.Security.Cryptography;
using System.Text;
using EventCenter.Web.Domain;
using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Domain.Enums;
using EventCenter.Web.Infrastructure.Email;
using EventCenter.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventCenter.Web.Services;

public class CompanyBookingService
{
    private readonly EventCenterDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<CompanyBookingService> _logger;
    private readonly IConfiguration _configuration;

    public CompanyBookingService(
        EventCenterDbContext context,
        IEmailSender emailSender,
        ILogger<CompanyBookingService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _emailSender = emailSender;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Validates invitation code using constant-time comparison to prevent timing attacks.
    /// </summary>
    public async Task<(bool IsValid, EventCompany? Company, string? ErrorMessage)> ValidateInvitationCodeAsync(string invitationCode)
    {
        // Load all invitations from database to perform constant-time comparison in-memory
        var invitations = await _context.EventCompanies
            .Include(ec => ec.Event)
                .ThenInclude(e => e.AgendaItems)
            .Include(ec => ec.Event.EventOptions)
            .Include(ec => ec.AgendaItemPrices)
            .ToListAsync();

        EventCompany? matchedInvitation = null;

        // Convert input code to bytes for constant-time comparison
        var inputBytes = Encoding.UTF8.GetBytes(invitationCode);

        foreach (var invitation in invitations)
        {
            if (string.IsNullOrEmpty(invitation.InvitationCode))
                continue;

            var storedBytes = Encoding.UTF8.GetBytes(invitation.InvitationCode);

            // Use constant-time comparison to prevent timing attacks
            if (inputBytes.Length == storedBytes.Length &&
                CryptographicOperations.FixedTimeEquals(inputBytes, storedBytes))
            {
                matchedInvitation = invitation;
                break;
            }
        }

        if (matchedInvitation == null)
        {
            return (false, null, "Dieser Link ist ungültig oder abgelaufen.");
        }

        // Check expiration
        if (matchedInvitation.ExpiresAtUtc.HasValue && matchedInvitation.ExpiresAtUtc.Value < DateTime.UtcNow)
        {
            return (false, null, "Dieser Link ist abgelaufen. Bitte kontaktieren Sie uns für eine neue Einladung.");
        }

        // Check status - Draft invitations cannot be accessed
        if (matchedInvitation.Status == InvitationStatus.Draft)
        {
            return (false, null, "Diese Einladung wurde noch nicht versendet.");
        }

        // Allow Booked and Cancelled status for status viewing
        return (true, matchedInvitation, null);
    }

    /// <summary>
    /// Submits a company booking with participant registrations.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SubmitBookingAsync(int eventCompanyId, CompanyBookingFormModel formModel)
    {
        var invitation = await _context.EventCompanies
            .Include(ec => ec.Event)
            .FirstOrDefaultAsync(ec => ec.Id == eventCompanyId);

        if (invitation == null)
        {
            return (false, "Einladung nicht gefunden.");
        }

        if (invitation.Status != InvitationStatus.Sent)
        {
            return (false, "Diese Einladung kann nicht mehr gebucht werden.");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<(bool Success, string? ErrorMessage)>(async () =>
        {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Validate submitted IDs belong to this event (prevent cross-event ID injection)
            var validAgendaItemIds = await _context.AgendaItems
                .Where(a => a.EventId == invitation.EventId)
                .Select(a => a.Id)
                .ToHashSetAsync();

            var validOptionIds = await _context.EventOptions
                .Where(o => o.EventId == invitation.EventId)
                .Select(o => o.Id)
                .ToHashSetAsync();

            var allAgendaIdsValid = formModel.Participants
                .SelectMany(p => p.SelectedAgendaItemIds)
                .All(id => validAgendaItemIds.Contains(id));

            var allOptionIdsValid = formModel.SelectedExtraOptionIds
                .All(id => validOptionIds.Contains(id));

            if (!allAgendaIdsValid || !allOptionIdsValid)
            {
                return (false, "Ungültige Auswahl.");
            }

            // Update invitation status
            invitation.Status = InvitationStatus.Booked;
            invitation.BookingDateUtc = DateTime.UtcNow;

            // Create registrations for each participant
            foreach (var participant in formModel.Participants)
            {
                var registration = new Registration
                {
                    EventId = invitation.EventId,
                    EventCompanyId = invitation.Id,
                    RegistrationType = RegistrationType.CompanyParticipant,
                    FirstName = participant.FirstName,
                    LastName = participant.LastName,
                    Email = participant.Email,
                    RegistrationDateUtc = DateTime.UtcNow,
                    IsConfirmed = true
                };

                _context.Registrations.Add(registration);
                await _context.SaveChangesAsync(); // Save to get registration ID

                // Link selected agenda items
                foreach (var agendaItemId in participant.SelectedAgendaItemIds)
                {
                    var registrationAgendaItem = new RegistrationAgendaItem
                    {
                        RegistrationId = registration.Id,
                        AgendaItemId = agendaItemId
                    };

                    _context.RegistrationAgendaItems.Add(registrationAgendaItem);
                }
            }

            // Link extra options to first registration if any participants
            if (formModel.Participants.Any() && formModel.SelectedExtraOptionIds.Any())
            {
                var firstRegistration = await _context.Registrations
                    .FirstAsync(r => r.EventCompanyId == invitation.Id && r.EventId == invitation.EventId);

                var options = await _context.EventOptions
                    .Where(eo => formModel.SelectedExtraOptionIds.Contains(eo.Id))
                    .ToListAsync();

                foreach (var option in options)
                {
                    firstRegistration.SelectedOptions.Add(option);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Fire-and-forget admin notification
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendAdminBookingNotificationAsync(
                        invitation,
                        invitation.Event,
                        formModel.Participants);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send admin booking notification for company {CompanyId} event {EventId}",
                        invitation.Id,
                        invitation.EventId);
                }
            });

            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to submit booking for company {CompanyId}", eventCompanyId);
            throw;
        }
        }); // end ExecuteAsync
    }

    /// <summary>
    /// Cancels a company booking and marks all registrations as cancelled.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> CancelBookingAsync(int eventCompanyId, string? cancellationComment)
    {
        var invitation = await _context.EventCompanies
            .Include(ec => ec.Event)
            .Include(ec => ec.Registrations)
            .FirstOrDefaultAsync(ec => ec.Id == eventCompanyId);

        if (invitation == null)
        {
            return (false, "Einladung nicht gefunden.");
        }

        if (invitation.Status != InvitationStatus.Booked)
        {
            return (false, "Keine aktive Buchung vorhanden.");
        }

        // Update invitation
        invitation.Status = InvitationStatus.Cancelled;
        invitation.CancellationComment = cancellationComment;
        invitation.IsNonParticipation = false;

        // Mark all registrations as cancelled
        foreach (var registration in invitation.Registrations)
        {
            registration.IsCancelled = true;
            registration.CancellationDateUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Fire-and-forget admin notification
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailSender.SendAdminCancellationNotificationAsync(
                    invitation,
                    invitation.Event,
                    cancellationComment,
                    false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send admin cancellation notification for company {CompanyId} event {EventId}",
                    invitation.Id,
                    invitation.EventId);
            }
        });

        return (true, null);
    }

    /// <summary>
    /// Cancels a single registration. If all registrations are then cancelled, the booking is marked as cancelled too.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> CancelSingleRegistrationAsync(int registrationId, int eventCompanyId, string? cancellationComment)
    {
        var invitation = await _context.EventCompanies
            .Include(ec => ec.Event)
            .Include(ec => ec.Registrations)
            .FirstOrDefaultAsync(ec => ec.Id == eventCompanyId);

        if (invitation == null)
            return (false, "Einladung nicht gefunden.");

        if (invitation.Status != InvitationStatus.Booked)
            return (false, "Keine aktive Buchung vorhanden.");

        var registration = invitation.Registrations.FirstOrDefault(r => r.Id == registrationId);
        if (registration == null)
            return (false, "Teilnehmer nicht gefunden.");

        if (registration.IsCancelled)
            return (false, "Teilnehmer ist bereits storniert.");

        registration.IsCancelled = true;
        registration.CancellationDateUtc = DateTime.UtcNow;
        registration.CancellationReason = cancellationComment;

        // If all registrations are now cancelled, mark the entire booking as cancelled
        if (invitation.Registrations.All(r => r.IsCancelled))
        {
            invitation.Status = InvitationStatus.Cancelled;
            invitation.CancellationComment = cancellationComment;
            invitation.IsNonParticipation = false;
        }

        await _context.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Reports non-participation for a company booking.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> ReportNonParticipationAsync(int eventCompanyId, string? comment)
    {
        var invitation = await _context.EventCompanies
            .Include(ec => ec.Event)
            .Include(ec => ec.Registrations)
            .FirstOrDefaultAsync(ec => ec.Id == eventCompanyId);

        if (invitation == null)
        {
            return (false, "Einladung nicht gefunden.");
        }

        if (invitation.Status != InvitationStatus.Booked)
        {
            return (false, "Keine aktive Buchung vorhanden.");
        }

        // Update invitation
        invitation.Status = InvitationStatus.Cancelled;
        invitation.CancellationComment = comment;
        invitation.IsNonParticipation = true;

        // Mark all registrations as cancelled
        foreach (var registration in invitation.Registrations)
        {
            registration.IsCancelled = true;
            registration.CancellationDateUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Fire-and-forget admin notification with non-participation flag
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailSender.SendAdminCancellationNotificationAsync(
                    invitation,
                    invitation.Event,
                    comment,
                    true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send admin non-participation notification for company {CompanyId} event {EventId}",
                    invitation.Id,
                    invitation.EventId);
            }
        });

        return (true, null);
    }

    /// <summary>
    /// Gets booking status with navigation properties loaded.
    /// </summary>
    public async Task<EventCompany?> GetBookingStatusAsync(int eventCompanyId)
    {
        return await _context.EventCompanies
            .Include(ec => ec.Event)
            .Include(ec => ec.Registrations)
                .ThenInclude(r => r.RegistrationAgendaItems)
                    .ThenInclude(rai => rai.AgendaItem)
            .FirstOrDefaultAsync(ec => ec.Id == eventCompanyId);
    }

    /// <summary>
    /// Calculates total cost based on custom pricing and selected options.
    /// </summary>
    public decimal CalculateTotalCost(
        EventCompany company,
        CompanyBookingFormModel formModel,
        List<EventAgendaItem> agendaItems,
        List<EventOption> eventOptions)
    {
        decimal total = 0;

        // Calculate participant costs
        foreach (var participant in formModel.Participants)
        {
            foreach (var agendaItemId in participant.SelectedAgendaItemIds)
            {
                var agendaItem = agendaItems.FirstOrDefault(ai => ai.Id == agendaItemId);
                if (agendaItem == null) continue;

                // Check for custom price
                var customPrice = company.AgendaItemPrices
                    .FirstOrDefault(aip => aip.AgendaItemId == agendaItemId);

                if (customPrice != null && customPrice.CustomPrice.HasValue)
                {
                    total += customPrice.CustomPrice.Value;
                }
                else
                {
                    total += agendaItem.CostForMakler;
                }
            }
        }

        // Add extra options
        foreach (var optionId in formModel.SelectedExtraOptionIds)
        {
            var option = eventOptions.FirstOrDefault(eo => eo.Id == optionId);
            if (option != null)
            {
                total += option.Price;
            }
        }

        return total;
    }
}
