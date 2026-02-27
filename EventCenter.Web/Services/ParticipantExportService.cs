using ClosedXML.Excel;
using EventCenter.Web.Domain;
using EventCenter.Web.Domain.Enums;
using EventCenter.Web.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EventCenter.Web.Services;

public class ParticipantExportService
{
    private readonly EventCenterDbContext _context;

    public ParticipantExportService(EventCenterDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Exports active participant list to Excel.
    /// Cancelled registrations are excluded per locked user decision.
    /// Columns: Vorname, Nachname, E-Mail, Firma, Typ, Anmeldedatum
    /// </summary>
    public async Task<byte[]> ExportParticipantListAsync(int eventId)
    {
        var registrations = await _context.Registrations
            .Include(r => r.EventCompany)
            .Where(r => r.EventId == eventId && !r.IsCancelled)
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .ToListAsync();

        var data = registrations.Select(r => new
        {
            Vorname = r.FirstName,
            Nachname = r.LastName,
            EMail = r.Email,
            Firma = r.EventCompany?.CompanyName ?? r.Company ?? "N/A",
            Typ = MapRegistrationType(r.RegistrationType),
            Anmeldedatum = TimeZoneHelper.FormatDateTimeCet(r.RegistrationDateUtc, "dd.MM.yyyy HH:mm")
        }).ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Teilnehmerliste");
        ws.Cell("A1").InsertTable(data);
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Exports contact data for active participants to Excel.
    /// Columns: Vorname, Nachname, E-Mail, Telefon, Firma
    /// </summary>
    public async Task<byte[]> ExportContactDataAsync(int eventId)
    {
        var registrations = await _context.Registrations
            .Include(r => r.EventCompany)
            .Where(r => r.EventId == eventId && !r.IsCancelled)
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .ToListAsync();

        var data = registrations.Select(r => new
        {
            Vorname = r.FirstName,
            Nachname = r.LastName,
            EMail = r.Email,
            Telefon = r.Phone ?? "N/A",
            Firma = r.EventCompany?.CompanyName ?? r.Company ?? "N/A"
        }).ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Kontaktdaten");
        ws.Cell("A1").InsertTable(data);
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Exports non-participants (invited company members minus registered) to Excel.
    /// Only includes companies with a defined MaxParticipants where not all spots are filled.
    /// Columns: Firma, Kontakt-E-Mail, Kontakt-Telefon, Eingeladene, Registrierte, Nicht registriert
    /// </summary>
    public async Task<byte[]> ExportNonParticipantsAsync(int eventId)
    {
        var companies = await _context.EventCompanies
            .Include(ec => ec.Registrations)
            .Where(ec => ec.EventId == eventId &&
                         (ec.Status == InvitationStatus.Sent || ec.Status == InvitationStatus.Booked))
            .OrderBy(ec => ec.CompanyName)
            .ToListAsync();

        var data = companies
            .Where(ec => ec.MaxParticipants.HasValue)
            .Select(ec =>
            {
                var activeRegistrations = ec.Registrations.Count(r => !r.IsCancelled);
                var notRegistered = Math.Max(0, ec.MaxParticipants!.Value - activeRegistrations);
                return new
                {
                    Firma = ec.CompanyName,
                    KontaktEmail = ec.ContactEmail,
                    KontaktTelefon = ec.ContactPhone ?? "N/A",
                    Eingeladene = ec.MaxParticipants.Value,
                    Registrierte = activeRegistrations,
                    NichtRegistriert = notRegistered
                };
            })
            .Where(r => r.NichtRegistriert > 0)
            .ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Nicht-Teilnehmer");
        ws.Cell("A1").InsertTable(data);
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Exports company list with participant counts to Excel.
    /// Columns: Firma, Kontakt-E-Mail, Kontakt-Telefon, Status, Teilnehmer, Einladung gesendet
    /// </summary>
    public async Task<byte[]> ExportCompanyListAsync(int eventId)
    {
        var companies = await _context.EventCompanies
            .Include(ec => ec.Registrations)
            .Where(ec => ec.EventId == eventId)
            .OrderBy(ec => ec.CompanyName)
            .ToListAsync();

        var data = companies.Select(ec => new
        {
            Firma = ec.CompanyName,
            KontaktEmail = ec.ContactEmail,
            KontaktTelefon = ec.ContactPhone ?? "N/A",
            Status = MapInvitationStatus(ec.Status),
            Teilnehmer = ec.Registrations.Count(r => !r.IsCancelled),
            EinladungGesendet = ec.InvitationSentUtc.HasValue
                ? TimeZoneHelper.FormatDateTimeCet(ec.InvitationSentUtc.Value, "dd.MM.yyyy")
                : "N/A"
        }).ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Firmenliste");
        ws.Cell("A1").InsertTable(data);
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string MapRegistrationType(RegistrationType type) => type switch
    {
        RegistrationType.Makler => "Makler",
        RegistrationType.Guest => "Gast",
        RegistrationType.CompanyParticipant => "Firma",
        _ => type.ToString()
    };

    private static string MapInvitationStatus(InvitationStatus status) => status switch
    {
        InvitationStatus.Draft => "Entwurf",
        InvitationStatus.Sent => "Gesendet",
        InvitationStatus.Booked => "Gebucht",
        InvitationStatus.Cancelled => "Storniert",
        _ => status.ToString()
    };
}
