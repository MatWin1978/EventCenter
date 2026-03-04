using System.Security.Cryptography;
using EventCenter.Web.Domain;
using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Domain.Enums;
using EventCenter.Web.Infrastructure.Email;
using EventCenter.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventCenter.Web.Services;

public class CompanyInvitationService
{
    private readonly EventCenterDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<CompanyInvitationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    public CompanyInvitationService(
        EventCenterDbContext context,
        IEmailSender emailSender,
        ILogger<CompanyInvitationService> logger,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _emailSender = emailSender;
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Generates a cryptographically secure GUID-based invitation code (32 hex characters without dashes).
    /// </summary>
    public static string GenerateSecureInvitationCode()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);

        // Set version 4 bits (RFC 4122)
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x40);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        var guid = new Guid(bytes);
        return guid.ToString("N"); // N format = 32 hex chars without dashes
    }

    /// <summary>
    /// Calculates custom price with percentage discount applied first, then manual override takes precedence.
    /// </summary>
    public static decimal CalculateCustomPrice(decimal basePrice, decimal? percentageDiscount, decimal? manualOverride)
    {
        if (manualOverride.HasValue)
        {
            return manualOverride.Value;
        }

        if (percentageDiscount.HasValue)
        {
            var discount = basePrice * (percentageDiscount.Value / 100m);
            var discountedPrice = basePrice - discount;
            return Math.Round(discountedPrice, 2, MidpointRounding.AwayFromZero);
        }

        return basePrice;
    }

    /// <summary>
    /// Creates a new company invitation with optional immediate sending.
    /// </summary>
    public async Task<(bool Success, int? InvitationId, string? ErrorMessage)> CreateInvitationAsync(
        CompanyInvitationFormModel formModel)
    {
        // Verify event exists
        var evt = await _context.Events
            .Include(e => e.AgendaItems)
            .FirstOrDefaultAsync(e => e.Id == formModel.EventId);

        if (evt == null)
        {
            return (false, null, "Veranstaltung nicht gefunden.");
        }

        // Check for duplicate email
        var existingInvitation = await _context.EventCompanies
            .AnyAsync(ec => ec.EventId == formModel.EventId &&
                           ec.ContactEmail == formModel.ContactEmail);

        if (existingInvitation)
        {
            return (false, null, "Diese Firma wurde bereits eingeladen.");
        }

        EventCompany? createdInvitation = null;

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var invitation = new EventCompany
                {
                    EventId = formModel.EventId,
                    CompanyId = formModel.CompanyId,
                    CompanyName = formModel.CompanyName,
                    ContactEmail = formModel.ContactEmail,
                    ContactPhone = formModel.ContactPhone,
                    InvitationCode = GenerateSecureInvitationCode(),
                    PercentageDiscount = formModel.PercentageDiscount,
                    PersonalMessage = formModel.PersonalMessage,
                    Status = formModel.SendImmediately ? InvitationStatus.Sent : InvitationStatus.Draft,
                    InvitationSentUtc = formModel.SendImmediately ? DateTime.UtcNow : null
                };

                _context.EventCompanies.Add(invitation);
                await _context.SaveChangesAsync();

                foreach (var priceModel in formModel.AgendaItemPrices)
                {
                    var agendaItem = evt.AgendaItems.FirstOrDefault(a => a.Id == priceModel.AgendaItemId);
                    if (agendaItem == null) continue;

                    var customPrice = CalculateCustomPrice(
                        priceModel.BasePrice,
                        formModel.PercentageDiscount,
                        priceModel.ManualOverride);

                    _context.EventCompanyAgendaItemPrices.Add(new EventCompanyAgendaItemPrice
                    {
                        EventCompanyId = invitation.Id,
                        AgendaItemId = priceModel.AgendaItemId,
                        CustomPrice = customPrice
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                createdInvitation = invitation;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create company invitation for event {EventId}", formModel.EventId);
                throw;
            }
        });

        if (formModel.SendImmediately && createdInvitation != null)
        {
            var invitationId = createdInvitation.Id;
            var invitationEmail = createdInvitation.ContactEmail;
            var personalMessage = formModel.PersonalMessage ?? string.Empty;
            var invitationLink = BuildInvitationLink(createdInvitation.InvitationCode!);

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<EventCenterDbContext>();

                    var invitationForEmail = await context.EventCompanies
                        .Include(ec => ec.AgendaItemPrices)
                            .ThenInclude(aip => aip.AgendaItem)
                        .FirstAsync(ec => ec.Id == invitationId);

                    await _emailSender.SendCompanyInvitationAsync(
                        invitationForEmail,
                        evt,
                        personalMessage,
                        invitationLink);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send company invitation email to {Email} for event {EventId}",
                        invitationEmail,
                        formModel.EventId);
                }
            });
        }

        return (true, createdInvitation!.Id, null);
    }

    /// <summary>
    /// Sends a draft invitation (transitions to Sent status).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendInvitationAsync(int invitationId)
    {
        var invitation = await _context.EventCompanies
            .Include(ec => ec.Event)
            .Include(ec => ec.AgendaItemPrices)
                .ThenInclude(aip => aip.AgendaItem)
            .FirstOrDefaultAsync(ec => ec.Id == invitationId);

        if (invitation == null)
        {
            return (false, "Einladung nicht gefunden.");
        }

        if (invitation.Status != InvitationStatus.Draft)
        {
            return (false, "Nur Entwürfe können versendet werden.");
        }

        invitation.Status = InvitationStatus.Sent;
        invitation.InvitationSentUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Fire-and-forget email
        _ = Task.Run(async () =>
        {
            try
            {
                var invitationLink = BuildInvitationLink(invitation.InvitationCode!);
                var personalMessage = invitation.PersonalMessage ?? string.Empty;

                await _emailSender.SendCompanyInvitationAsync(
                    invitation,
                    invitation.Event,
                    personalMessage,
                    invitationLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send company invitation email to {Email} for invitation {InvitationId}",
                    invitation.ContactEmail,
                    invitationId);
            }
        });

        return (true, null);
    }

    /// <summary>
    /// Resends an existing invitation (updates timestamp and sends email again).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> ResendInvitationAsync(int invitationId)
    {
        var invitation = await _context.EventCompanies
            .Include(ec => ec.Event)
            .Include(ec => ec.AgendaItemPrices)
                .ThenInclude(aip => aip.AgendaItem)
            .FirstOrDefaultAsync(ec => ec.Id == invitationId);

        if (invitation == null)
        {
            return (false, "Einladung nicht gefunden.");
        }

        if (invitation.Status != InvitationStatus.Sent)
        {
            return (false, "Nur versendete Einladungen können erneut versendet werden.");
        }

        invitation.InvitationSentUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Fire-and-forget email
        _ = Task.Run(async () =>
        {
            try
            {
                var invitationLink = BuildInvitationLink(invitation.InvitationCode!);
                var personalMessage = invitation.PersonalMessage ?? string.Empty;

                await _emailSender.SendCompanyInvitationAsync(
                    invitation,
                    invitation.Event,
                    personalMessage,
                    invitationLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to resend company invitation email to {Email} for invitation {InvitationId}",
                    invitation.ContactEmail,
                    invitationId);
            }
        });

        return (true, null);
    }

    /// <summary>
    /// Updates invitation details (pricing always editable per user decision).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> UpdateInvitationAsync(
        int invitationId,
        CompanyInvitationFormModel formModel)
    {
        var invitation = await _context.EventCompanies
            .Include(ec => ec.AgendaItemPrices)
            .FirstOrDefaultAsync(ec => ec.Id == invitationId);

        if (invitation == null)
        {
            return (false, "Einladung nicht gefunden.");
        }

        var evt = await _context.Events
            .Include(e => e.AgendaItems)
            .FirstOrDefaultAsync(e => e.Id == formModel.EventId);

        if (evt == null)
        {
            return (false, "Veranstaltung nicht gefunden.");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (formModel.CompanyId.HasValue)
                    invitation.CompanyId = formModel.CompanyId;
                invitation.CompanyName = formModel.CompanyName;
                invitation.ContactEmail = formModel.ContactEmail;
                invitation.ContactPhone = formModel.ContactPhone;
                invitation.PercentageDiscount = formModel.PercentageDiscount;
                invitation.PersonalMessage = formModel.PersonalMessage;

                _context.EventCompanyAgendaItemPrices.RemoveRange(invitation.AgendaItemPrices);

                foreach (var priceModel in formModel.AgendaItemPrices)
                {
                    var agendaItem = evt.AgendaItems.FirstOrDefault(a => a.Id == priceModel.AgendaItemId);
                    if (agendaItem == null) continue;

                    var customPrice = CalculateCustomPrice(
                        priceModel.BasePrice,
                        formModel.PercentageDiscount,
                        priceModel.ManualOverride);

                    _context.EventCompanyAgendaItemPrices.Add(new EventCompanyAgendaItemPrice
                    {
                        EventCompanyId = invitation.Id,
                        AgendaItemId = priceModel.AgendaItemId,
                        CustomPrice = customPrice
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to update company invitation {InvitationId}", invitationId);
                throw;
            }
        });

        return (true, null);
    }

    /// <summary>
    /// Deletes invitation (prevented if status is Booked).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> DeleteInvitationAsync(int invitationId)
    {
        var invitation = await _context.EventCompanies
            .Include(ec => ec.AgendaItemPrices)
            .FirstOrDefaultAsync(ec => ec.Id == invitationId);

        if (invitation == null)
        {
            return (false, "Einladung nicht gefunden.");
        }

        if (invitation.Status == InvitationStatus.Booked)
        {
            return (false, "Diese Einladung kann nicht gelöscht werden, da bereits eine Buchung vorliegt.");
        }

        _context.EventCompanyAgendaItemPrices.RemoveRange(invitation.AgendaItemPrices);
        _context.EventCompanies.Remove(invitation);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    /// <summary>
    /// Gets all invitations for an event with navigation properties loaded.
    /// </summary>
    public async Task<List<EventCompany>> GetInvitationsForEventAsync(int eventId)
    {
        return await _context.EventCompanies
            .Include(ec => ec.Event)
            .Where(ec => ec.EventId == eventId)
            .OrderBy(ec => ec.CompanyName)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a single invitation by ID with agenda item prices loaded.
    /// </summary>
    public async Task<EventCompany?> GetInvitationByIdAsync(int invitationId)
    {
        return await _context.EventCompanies
            .Include(ec => ec.Event)
            .Include(ec => ec.AgendaItemPrices)
                .ThenInclude(aip => aip.AgendaItem)
            .FirstOrDefaultAsync(ec => ec.Id == invitationId);
    }

    /// <summary>
    /// Gets invitation status summary (counts per status for overview).
    /// </summary>
    public async Task<Dictionary<InvitationStatus, int>> GetInvitationStatusSummaryAsync(int eventId)
    {
        var invitations = await _context.EventCompanies
            .Where(ec => ec.EventId == eventId)
            .ToListAsync();

        var summary = new Dictionary<InvitationStatus, int>();

        foreach (InvitationStatus status in Enum.GetValues(typeof(InvitationStatus)))
        {
            summary[status] = invitations.Count(i => i.Status == status);
        }

        return summary;
    }

    /// <summary>
    /// Creates multiple invitations in batch with standard pricing. Returns counts and errors.
    /// </summary>
    public async Task<(int Succeeded, int Failed, List<string> Errors)> CreateBatchInvitationsAsync(
        int eventId,
        List<CompanyInvitationFormModel> models)
    {
        var succeeded = 0;
        var failed = 0;
        var errors = new List<string>();

        foreach (var model in models)
        {
            var (success, _, error) = await CreateInvitationAsync(model);
            if (success)
            {
                succeeded++;
            }
            else
            {
                failed++;
                errors.Add($"{model.CompanyName}: {error}");
            }
        }

        return (succeeded, failed, errors);
    }

    private string BuildInvitationLink(string invitationCode)
    {
        var baseUrl = _configuration["BaseUrl"] ?? "https://localhost";
        return $"{baseUrl}/company/booking/{invitationCode}";
    }
}
