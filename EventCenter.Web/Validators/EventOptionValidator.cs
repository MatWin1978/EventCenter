using EventCenter.Web.Domain.Entities;
using FluentValidation;

namespace EventCenter.Web.Validators;

public class EventOptionValidator : AbstractValidator<EventOption>
{
    public EventOptionValidator()
    {
        RuleFor(o => o.Name)
            .NotEmpty().WithMessage("Name ist erforderlich")
            .MaximumLength(200).WithMessage("Name darf maximal 200 Zeichen lang sein");

        RuleFor(o => o.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Preis darf nicht negativ sein");

        RuleFor(o => o.MaxQuantity)
            .GreaterThan(0)
            .When(x => x.MaxQuantity.HasValue)
            .WithMessage("Maximale Anzahl muss größer als 0 sein");
    }
}
