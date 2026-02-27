using FluentValidation;
using EventCenter.Web.Models;

namespace EventCenter.Web.Validators;

public class CompanyInvitationValidator : AbstractValidator<CompanyInvitationFormModel>
{
    public CompanyInvitationValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .WithMessage("Firmenname ist erforderlich");

        RuleFor(x => x.ContactEmail)
            .NotEmpty()
            .WithMessage("E-Mail-Adresse ist erforderlich")
            .EmailAddress()
            .WithMessage("Ungültige E-Mail-Adresse");

        RuleFor(x => x.PercentageDiscount)
            .InclusiveBetween(0, 100)
            .When(x => x.PercentageDiscount.HasValue)
            .WithMessage("Rabatt muss zwischen 0 und 100 Prozent liegen");

        RuleForEach(x => x.AgendaItemPrices)
            .ChildRules(price =>
            {
                price.RuleFor(p => p.ManualOverride)
                    .GreaterThanOrEqualTo(0)
                    .When(p => p.ManualOverride.HasValue)
                    .WithMessage("Preis darf nicht negativ sein");
            });
    }
}
