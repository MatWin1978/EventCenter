using EventCenter.Web.Domain.Entities;
using FluentValidation;

namespace EventCenter.Web.Validators;

public class EventAgendaItemValidator : AbstractValidator<EventAgendaItem>
{
    public EventAgendaItemValidator()
    {
        RuleFor(a => a.Title)
            .NotEmpty().WithMessage("Titel ist erforderlich")
            .MaximumLength(200).WithMessage("Titel darf maximal 200 Zeichen lang sein");

        RuleFor(a => a.EndDateTimeUtc)
            .GreaterThan(a => a.StartDateTimeUtc)
            .WithMessage("Endzeitpunkt muss nach Startzeitpunkt liegen");

        RuleFor(a => a.CostForMakler)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Kosten dürfen nicht negativ sein");

        RuleFor(a => a.CostForGuest)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Kosten dürfen nicht negativ sein");
    }
}
