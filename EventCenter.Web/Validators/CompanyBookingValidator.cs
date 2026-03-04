using EventCenter.Web.Models;
using FluentValidation;

namespace EventCenter.Web.Validators;

public class CompanyBookingValidator : AbstractValidator<CompanyBookingFormModel>
{
    public CompanyBookingValidator()
    {
        RuleFor(x => x.Participants)
            .NotEmpty().WithMessage("Mindestens ein Teilnehmer erforderlich");

        RuleForEach(x => x.Participants)
            .SetValidator(new ParticipantValidator());
    }
}

public class ParticipantValidator : AbstractValidator<ParticipantModel>
{
    public ParticipantValidator()
    {
        RuleFor(x => x.Salutation)
            .NotEmpty().WithMessage("Anrede ist erforderlich")
            .Must(s => new[] { "Herr", "Frau", "Divers" }.Contains(s))
            .WithMessage("Ungültige Anrede");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Vorname ist erforderlich")
            .MaximumLength(100).WithMessage("Vorname darf maximal 100 Zeichen lang sein");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Nachname ist erforderlich")
            .MaximumLength(100).WithMessage("Nachname darf maximal 100 Zeichen lang sein");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-Mail ist erforderlich")
            .EmailAddress().WithMessage("Ungültige E-Mail-Adresse")
            .MaximumLength(200).WithMessage("E-Mail darf maximal 200 Zeichen lang sein");

        RuleFor(x => x.SelectedAgendaItemIds)
            .NotEmpty().WithMessage("Mindestens ein Agendapunkt muss ausgewählt werden")
            .When(x => x.AgendaRequired);
    }
}
