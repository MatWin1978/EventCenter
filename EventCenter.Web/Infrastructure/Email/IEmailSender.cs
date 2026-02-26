namespace EventCenter.Web.Infrastructure.Email;

using EventCenter.Web.Domain.Entities;

public interface IEmailSender
{
    Task SendRegistrationConfirmationAsync(Registration registration);
}
