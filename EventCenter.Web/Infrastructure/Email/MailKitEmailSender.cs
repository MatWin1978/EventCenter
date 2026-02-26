using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Infrastructure.Helpers;
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
}
