using FluentValidation;
using EventCenter.Web.Models;

namespace EventCenter.Web.Validators;

public class RegistrationValidator : AbstractValidator<RegistrationFormModel>
{
    public RegistrationValidator()
    {
        RuleFor(r => r.FirstName)
            .NotEmpty().WithMessage("Vorname ist erforderlich.")
            .MaximumLength(100).WithMessage("Vorname darf maximal 100 Zeichen lang sein.");

        RuleFor(r => r.LastName)
            .NotEmpty().WithMessage("Nachname ist erforderlich.")
            .MaximumLength(100).WithMessage("Nachname darf maximal 100 Zeichen lang sein.");

        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Gültige E-Mail-Adresse ist erforderlich.")
            .EmailAddress().WithMessage("Gültige E-Mail-Adresse ist erforderlich.");

        RuleFor(r => r.EventId)
            .GreaterThan(0).WithMessage("Veranstaltung ist erforderlich.");

        RuleFor(r => r.SelectedAgendaItemIds)
            .NotEmpty().WithMessage("Bitte wählen Sie mindestens einen Agendapunkt aus.");
    }
}
