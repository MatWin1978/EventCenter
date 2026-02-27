namespace EventCenter.Web.Infrastructure.Email;

using EventCenter.Web.Domain.Entities;

public interface IEmailSender
{
    Task SendRegistrationConfirmationAsync(Registration registration);
    Task SendCompanyInvitationAsync(EventCompany invitation, Event evt, string personalMessage, string invitationLink);
}
