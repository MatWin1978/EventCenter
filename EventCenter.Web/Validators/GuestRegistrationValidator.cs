using FluentValidation;
using EventCenter.Web.Models;

namespace EventCenter.Web.Validators;

public class GuestRegistrationValidator : AbstractValidator<GuestRegistrationFormModel>
{
    public GuestRegistrationValidator()
    {
        RuleFor(r => r.Salutation)
            .NotEmpty().WithMessage("Anrede ist erforderlich.")
            .Must(s => s == "Herr" || s == "Frau" || s == "Divers")
            .WithMessage("Ungültige Anrede.");

        RuleFor(r => r.FirstName)
            .NotEmpty().WithMessage("Vorname ist erforderlich.")
            .MaximumLength(100).WithMessage("Vorname darf maximal 100 Zeichen lang sein.");

        RuleFor(r => r.LastName)
            .NotEmpty().WithMessage("Nachname ist erforderlich.")
            .MaximumLength(100).WithMessage("Nachname darf maximal 100 Zeichen lang sein.");

        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("E-Mail ist erforderlich.")
            .EmailAddress().WithMessage("Gültige E-Mail-Adresse ist erforderlich.");

        RuleFor(r => r.RelationshipType)
            .NotEmpty().WithMessage("Beziehungstyp ist erforderlich.")
            .MaximumLength(100).WithMessage("Beziehungstyp darf maximal 100 Zeichen lang sein.");

        RuleFor(r => r.SelectedAgendaItemIds)
            .NotEmpty().WithMessage("Bitte wählen Sie mindestens einen Agendapunkt aus.");
    }
}
