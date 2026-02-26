using FluentValidation;
using EventCenter.Web.Domain.Entities;

namespace EventCenter.Web.Validators;

public class EventValidator : AbstractValidator<Event>
{
    public EventValidator()
    {
        RuleFor(e => e.Title)
            .NotEmpty().WithMessage("Titel ist erforderlich")
            .MaximumLength(200).WithMessage("Titel darf maximal 200 Zeichen lang sein");

        RuleFor(e => e.Location)
            .NotEmpty().WithMessage("Ort ist erforderlich")
            .MaximumLength(200).WithMessage("Ort darf maximal 200 Zeichen lang sein");

        RuleFor(e => e.StartDateUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Veranstaltungsbeginn muss in der Zukunft liegen")
            .When(e => e.Id == 0); // Only for new events

        RuleFor(e => e.EndDateUtc)
            .GreaterThan(e => e.StartDateUtc)
            .WithMessage("Veranstaltungsende muss nach Beginn liegen");

        RuleFor(e => e.RegistrationDeadlineUtc)
            .LessThanOrEqualTo(e => e.StartDateUtc)
            .WithMessage("Anmeldefrist muss vor Veranstaltungsbeginn liegen");

        RuleFor(e => e.MaxCapacity)
            .GreaterThan(0).WithMessage("Maximale Kapazität muss größer als 0 sein");

        RuleFor(e => e.MaxCompanions)
            .GreaterThanOrEqualTo(0).WithMessage("Maximale Begleitpersonen darf nicht negativ sein");

        RuleFor(e => e.ContactEmail)
            .EmailAddress()
            .When(e => !string.IsNullOrEmpty(e.ContactEmail))
            .WithMessage("Ungültige E-Mail-Adresse");

        RuleFor(e => e.ContactPhone)
            .MaximumLength(50)
            .WithMessage("Telefonnummer darf maximal 50 Zeichen lang sein");

        RuleForEach(e => e.AgendaItems)
            .SetValidator(new EventAgendaItemValidator());

        RuleForEach(e => e.EventOptions)
            .SetValidator(new EventOptionValidator());
    }
}
