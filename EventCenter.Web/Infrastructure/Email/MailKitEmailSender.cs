using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Infrastructure.Helpers;
using EventCenter.Web.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EventCenter.Web.Infrastructure.Email;

public class MailKitEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IOptions<SmtpSettings> settings, ILogger<MailKitEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendRegistrationConfirmationAsync(Registration registration)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress($"{registration.FirstName} {registration.LastName}", registration.Email));
            message.Subject = $"Anmeldebestätigung: {registration.Event.Title}";

            var htmlBody = BuildHtmlBody(registration);
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl);

            // Authenticate if username is provided
            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "Successfully sent registration confirmation email to {Email} for event {EventId}",
                registration.Email,
                registration.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send registration confirmation email to {Email} for event {EventId}",
                registration.Email,
                registration.EventId);
            throw;
        }
    }

    public async Task SendCompanyInvitationAsync(EventCompany invitation, Event evt, string personalMessage, string invitationLink)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress(invitation.CompanyName, invitation.ContactEmail));
            message.Subject = $"Einladung: {evt.Title}";

            var htmlBody = BuildCompanyInvitationHtmlBody(invitation, evt, personalMessage, invitationLink);
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl);

            // Authenticate if username is provided
            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "Successfully sent company invitation email to {Email} for event {EventId}",
                invitation.ContactEmail,
                evt.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send company invitation email to {Email} for event {EventId}",
                invitation.ContactEmail,
                evt.Id);
            throw;
        }
    }

    private string BuildHtmlBody(Registration registration)
    {
        var evt = registration.Event;
        var startDate = TimeZoneHelper.FormatDateTimeCet(evt.StartDateUtc, "dd.MM.yyyy HH:mm");
        var endDate = TimeZoneHelper.FormatDateTimeCet(evt.EndDateUtc, "dd.MM.yyyy HH:mm");

        // Calculate total cost based on selected agenda items
        decimal totalCost = 0;
        var agendaItemsHtml = string.Empty;

        if (registration.RegistrationAgendaItems.Any())
        {
            agendaItemsHtml = "<h3 style=\"color: #333; margin-top: 20px;\">Ihre ausgewählten Agendapunkte:</h3><ul style=\"list-style: none; padding: 0;\">";

            foreach (var regAgendaItem in registration.RegistrationAgendaItems)
            {
                var item = regAgendaItem.AgendaItem;
                var cost = registration.RegistrationType == Domain.Enums.RegistrationType.Makler
                    ? item.CostForMakler
                    : item.CostForGuest;
                totalCost += cost;

                var itemStart = TimeZoneHelper.FormatDateTimeCet(item.StartDateTimeUtc, "dd.MM.yyyy HH:mm");
                var itemEnd = TimeZoneHelper.FormatDateTimeCet(item.EndDateTimeUtc, "HH:mm");

                agendaItemsHtml += $@"
                    <li style=""margin: 10px 0; padding: 10px; background-color: #f8f9fa; border-left: 3px solid #007bff;"">
                        <strong>{item.Title}</strong><br/>
                        <span style=""color: #666;"">Zeit: {itemStart} - {itemEnd}</span><br/>
                        <span style=""color: #666;"">Kosten: {cost:C}</span>
                    </li>";
            }
            agendaItemsHtml += "</ul>";
        }

        return $@"
<!DOCTYPE html>
<html lang=""de"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Anmeldebestätigung</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0;"">
        <h1 style=""margin: 0;"">Anmeldebestätigung</h1>
    </div>

    <div style=""background-color: #ffffff; padding: 20px; border: 1px solid #dee2e6; border-radius: 0 0 5px 5px;"">
        <p>Sehr geehrte(r) {registration.FirstName} {registration.LastName},</p>

        <p>Ihre Anmeldung wurde erfolgreich registriert:</p>

        <div style=""background-color: #f8f9fa; padding: 15px; margin: 20px 0; border-radius: 5px;"">
            <h2 style=""color: #007bff; margin-top: 0;"">{evt.Title}</h2>
            <p style=""margin: 5px 0;""><strong>Datum:</strong> {startDate} - {endDate}</p>
            <p style=""margin: 5px 0;""><strong>Ort:</strong> {evt.Location}</p>
        </div>

        {agendaItemsHtml}

        {(totalCost > 0 ? $@"<div style=""background-color: #d1ecf1; padding: 15px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #0c5460;"">
            <strong>Gesamtkosten:</strong> {totalCost:C}
        </div>" : "")}

        <p style=""margin-top: 30px;"">Wir freuen uns auf Ihre Teilnahme!</p>

        <hr style=""border: none; border-top: 1px solid #dee2e6; margin: 30px 0;""/>

        <p style=""color: #666; font-size: 12px; text-align: center;"">
            Bei Fragen wenden Sie sich bitte an: {_settings.SenderEmail}
        </p>
    </div>
</body>
</html>";
    }

    private string BuildCompanyInvitationHtmlBody(EventCompany invitation, Event evt, string personalMessage, string invitationLink)
    {
        var startDate = TimeZoneHelper.FormatDateTimeCet(evt.StartDateUtc, "dd.MM.yyyy HH:mm");
        var endDate = TimeZoneHelper.FormatDateTimeCet(evt.EndDateUtc, "dd.MM.yyyy HH:mm");

        var personalMessageHtml = string.Empty;
        if (!string.IsNullOrWhiteSpace(personalMessage))
        {
            personalMessageHtml = $@"
        <div style=""background-color: #f8f9fa; padding: 15px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #007bff;"">
            <p style=""margin: 0; font-style: italic;"">{personalMessage.Replace("\n", "<br/>")}</p>
        </div>";
        }

        var pricingSummaryHtml = BuildPricingSummaryHtml(invitation);

        return $@"
<!DOCTYPE html>
<html lang=""de"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Veranstaltungseinladung</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0;"">
        <h1 style=""margin: 0;"">Sie sind eingeladen</h1>
    </div>

    <div style=""background-color: #ffffff; padding: 20px; border: 1px solid #dee2e6; border-radius: 0 0 5px 5px;"">
        <p>Sehr geehrte Damen und Herren von {invitation.CompanyName},</p>

        <p>wir laden Sie herzlich zu unserer Veranstaltung ein:</p>

        <div style=""background-color: #f8f9fa; padding: 15px; margin: 20px 0; border-radius: 5px;"">
            <h2 style=""color: #007bff; margin-top: 0;"">{evt.Title}</h2>
            <p style=""margin: 5px 0;""><strong>Datum:</strong> {startDate} - {endDate}</p>
            <p style=""margin: 5px 0;""><strong>Ort:</strong> {evt.Location}</p>
        </div>

        {personalMessageHtml}

        {pricingSummaryHtml}

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{invitationLink}"" style=""display: inline-block; background-color: #007bff; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">
                Jetzt anmelden
            </a>
        </div>

        <p style=""color: #666; font-size: 14px; margin-top: 30px;"">
            Bitte verwenden Sie den obigen Link, um sich für die Veranstaltung zu registrieren.
            Dieser Link ist nur für Ihr Unternehmen bestimmt.
        </p>

        <hr style=""border: none; border-top: 1px solid #dee2e6; margin: 30px 0;""/>

        <p style=""color: #666; font-size: 12px; text-align: center;"">
            Bei Fragen wenden Sie sich bitte an: {_settings.SenderEmail}
        </p>
    </div>
</body>
</html>";
    }

    private string BuildPricingSummaryHtml(EventCompany invitation)
    {
        if (!invitation.AgendaItemPrices.Any())
        {
            return string.Empty;
        }

        var culture = new System.Globalization.CultureInfo("de-DE");
        var tableRows = string.Empty;

        foreach (var agendaPrice in invitation.AgendaItemPrices)
        {
            var item = agendaPrice.AgendaItem;
            var basePrice = item.CostForMakler;
            var customPrice = agendaPrice.CustomPrice ?? basePrice;
            var itemStart = TimeZoneHelper.FormatDateTimeCet(item.StartDateTimeUtc, "dd.MM.yyyy HH:mm");
            var itemEnd = TimeZoneHelper.FormatDateTimeCet(item.EndDateTimeUtc, "HH:mm");

            tableRows += $@"
                <tr>
                    <td style=""padding: 10px; border-bottom: 1px solid #dee2e6;"">
                        <strong>{item.Title}</strong><br/>
                        <span style=""color: #666; font-size: 13px;"">Zeit: {itemStart} - {itemEnd}</span>
                    </td>
                    <td style=""padding: 10px; border-bottom: 1px solid #dee2e6; text-align: right; color: #666;"">
                        {basePrice.ToString("C", culture)}
                    </td>
                    <td style=""padding: 10px; border-bottom: 1px solid #dee2e6; text-align: right; font-weight: bold; color: #007bff;"">
                        {customPrice.ToString("C", culture)}
                    </td>
                </tr>";
        }

        return $@"
        <h3 style=""color: #333; margin-top: 20px;"">Preisübersicht für Ihre Firma:</h3>
        <table style=""width: 100%; border-collapse: collapse; margin: 15px 0;"">
            <thead>
                <tr style=""background-color: #f8f9fa;"">
                    <th style=""padding: 10px; text-align: left; border-bottom: 2px solid #dee2e6;"">Agendapunkt</th>
                    <th style=""padding: 10px; text-align: right; border-bottom: 2px solid #dee2e6;"">Basispreis</th>
                    <th style=""padding: 10px; text-align: right; border-bottom: 2px solid #dee2e6;"">Ihr Preis</th>
                </tr>
            </thead>
            <tbody>
                {tableRows}
            </tbody>
        </table>";
    }

    public async Task SendAdminBookingNotificationAsync(EventCompany company, Event evt, List<ParticipantModel> participants)
    {
        try
        {
            var adminEmail = Environment.GetEnvironmentVariable("AdminNotificationEmail")
                ?? evt.ContactEmail
                ?? "admin@example.com";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress("Administrator", adminEmail));
            message.Subject = $"Neue Firmenbuchung: {company.CompanyName} - {evt.Title}";

            var htmlBody = BuildAdminBookingNotificationHtmlBody(company, evt, participants);
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl);

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "Successfully sent admin booking notification for company {CompanyId} and event {EventId}",
                company.Id,
                evt.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send admin booking notification for company {CompanyId} and event {EventId}",
                company.Id,
                evt.Id);
            throw;
        }
    }

    public async Task SendAdminCancellationNotificationAsync(EventCompany company, Event evt, string? cancellationComment, bool isNonParticipation)
    {
        try
        {
            var adminEmail = Environment.GetEnvironmentVariable("AdminNotificationEmail")
                ?? evt.ContactEmail
                ?? "admin@example.com";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress("Administrator", adminEmail));

            var subjectType = isNonParticipation ? "Nicht-Teilnahme" : "Firmenstornierung";
            message.Subject = $"{subjectType}: {company.CompanyName} - {evt.Title}";

            var htmlBody = BuildAdminCancellationNotificationHtmlBody(company, evt, cancellationComment, isNonParticipation);
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl);

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "Successfully sent admin cancellation notification for company {CompanyId} and event {EventId}",
                company.Id,
                evt.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send admin cancellation notification for company {CompanyId} and event {EventId}",
                company.Id,
                evt.Id);
            throw;
        }
    }

    private string BuildAdminBookingNotificationHtmlBody(EventCompany company, Event evt, List<ParticipantModel> participants)
    {
        var startDate = TimeZoneHelper.FormatDateTimeCet(evt.StartDateUtc, "dd.MM.yyyy HH:mm");
        var endDate = TimeZoneHelper.FormatDateTimeCet(evt.EndDateUtc, "dd.MM.yyyy HH:mm");
        var culture = new System.Globalization.CultureInfo("de-DE");

        var participantRows = string.Empty;
        foreach (var participant in participants)
        {
            participantRows += $@"
                <tr>
                    <td style=""padding: 10px; border-bottom: 1px solid #dee2e6;"">{participant.Salutation}</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #dee2e6;"">{participant.FirstName} {participant.LastName}</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #dee2e6;"">{participant.Email}</td>
                </tr>";
        }

        return $@"
<!DOCTYPE html>
<html lang=""de"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Neue Firmenbuchung</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0;"">
        <h1 style=""margin: 0;"">Neue Firmenbuchung</h1>
    </div>

    <div style=""background-color: #ffffff; padding: 20px; border: 1px solid #dee2e6; border-radius: 0 0 5px 5px;"">
        <p>Eine Firma hat sich für eine Veranstaltung angemeldet:</p>

        <div style=""background-color: #f8f9fa; padding: 15px; margin: 20px 0; border-radius: 5px;"">
            <h2 style=""color: #007bff; margin-top: 0;"">{evt.Title}</h2>
            <p style=""margin: 5px 0;""><strong>Datum:</strong> {startDate} - {endDate}</p>
            <p style=""margin: 5px 0;""><strong>Ort:</strong> {evt.Location}</p>
        </div>

        <div style=""background-color: #e9ecef; padding: 15px; margin: 20px 0; border-radius: 5px;"">
            <h3 style=""margin-top: 0; color: #333;"">Firmendetails</h3>
            <p style=""margin: 5px 0;""><strong>Firma:</strong> {company.CompanyName}</p>
            <p style=""margin: 5px 0;""><strong>Kontakt-E-Mail:</strong> {company.ContactEmail}</p>
            <p style=""margin: 5px 0;""><strong>Anzahl Teilnehmer:</strong> {participants.Count}</p>
        </div>

        <h3 style=""color: #333; margin-top: 20px;"">Teilnehmerliste:</h3>
        <table style=""width: 100%; border-collapse: collapse; margin: 15px 0;"">
            <thead>
                <tr style=""background-color: #f8f9fa;"">
                    <th style=""padding: 10px; text-align: left; border-bottom: 2px solid #dee2e6;"">Anrede</th>
                    <th style=""padding: 10px; text-align: left; border-bottom: 2px solid #dee2e6;"">Name</th>
                    <th style=""padding: 10px; text-align: left; border-bottom: 2px solid #dee2e6;"">E-Mail</th>
                </tr>
            </thead>
            <tbody>
                {participantRows}
            </tbody>
        </table>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""https://example.com/admin/events/{evt.Id}/companies"" style=""display: inline-block; background-color: #007bff; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">
                Zur Firmenverwaltung
            </a>
        </div>

        <hr style=""border: none; border-top: 1px solid #dee2e6; margin: 30px 0;""/>

        <p style=""color: #666; font-size: 12px; text-align: center;"">
            Diese E-Mail wurde automatisch generiert.
        </p>
    </div>
</body>
</html>";
    }

    private string BuildAdminCancellationNotificationHtmlBody(EventCompany company, Event evt, string? cancellationComment, bool isNonParticipation)
    {
        var startDate = TimeZoneHelper.FormatDateTimeCet(evt.StartDateUtc, "dd.MM.yyyy HH:mm");
        var endDate = TimeZoneHelper.FormatDateTimeCet(evt.EndDateUtc, "dd.MM.yyyy HH:mm");
        var cancellationType = isNonParticipation ? "Nicht-Teilnahme gemeldet" : "Stornierung";
        var headerColor = "#dc3545";

        var commentHtml = string.Empty;
        if (!string.IsNullOrWhiteSpace(cancellationComment))
        {
            commentHtml = $@"
        <div style=""background-color: #f8f9fa; padding: 15px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #dc3545;"">
            <h3 style=""margin-top: 0; color: #333;"">Kommentar:</h3>
            <p style=""margin: 0; font-style: italic;"">{cancellationComment.Replace("\n", "<br/>")}</p>
        </div>";
        }

        return $@"
<!DOCTYPE html>
<html lang=""de"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{cancellationType}</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background-color: {headerColor}; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0;"">
        <h1 style=""margin: 0;"">{cancellationType}</h1>
    </div>

    <div style=""background-color: #ffffff; padding: 20px; border: 1px solid #dee2e6; border-radius: 0 0 5px 5px;"">
        <p>Eine Firma hat {(isNonParticipation ? "eine Nicht-Teilnahme gemeldet" : "ihre Buchung storniert")}:</p>

        <div style=""background-color: #f8f9fa; padding: 15px; margin: 20px 0; border-radius: 5px;"">
            <h2 style=""color: #007bff; margin-top: 0;"">{evt.Title}</h2>
            <p style=""margin: 5px 0;""><strong>Datum:</strong> {startDate} - {endDate}</p>
            <p style=""margin: 5px 0;""><strong>Ort:</strong> {evt.Location}</p>
        </div>

        <div style=""background-color: #e9ecef; padding: 15px; margin: 20px 0; border-radius: 5px;"">
            <h3 style=""margin-top: 0; color: #333;"">Firmendetails</h3>
            <p style=""margin: 5px 0;""><strong>Firma:</strong> {company.CompanyName}</p>
            <p style=""margin: 5px 0;""><strong>Kontakt-E-Mail:</strong> {company.ContactEmail}</p>
            <p style=""margin: 5px 0;""><strong>Art:</strong> {cancellationType}</p>
        </div>

        {commentHtml}

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""https://example.com/admin/events/{evt.Id}/companies"" style=""display: inline-block; background-color: #dc3545; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">
                Zur Firmenverwaltung
            </a>
        </div>

        <hr style=""border: none; border-top: 1px solid #dee2e6; margin: 30px 0;""/>

        <p style=""color: #666; font-size: 12px; text-align: center;"">
            Diese E-Mail wurde automatisch generiert.
        </p>
    </div>
</body>
</html>";
    }
}
