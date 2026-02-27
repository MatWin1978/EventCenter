namespace EventCenter.Web.Infrastructure.Email;

using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Models;

public interface IEmailSender
{
    Task SendRegistrationConfirmationAsync(Registration registration);
    Task SendCompanyInvitationAsync(EventCompany invitation, Event evt, string personalMessage, string invitationLink);
    Task SendAdminBookingNotificationAsync(EventCompany company, Event evt, List<ParticipantModel> participants);
    Task SendAdminCancellationNotificationAsync(EventCompany company, Event evt, string? cancellationComment, bool isNonParticipation);
    Task SendGuestRegistrationConfirmationAsync(Registration guestRegistration, Registration brokerRegistration);
}
