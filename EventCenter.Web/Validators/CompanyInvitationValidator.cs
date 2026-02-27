using FluentValidation;
using EventCenter.Web.Models;

namespace EventCenter.Web.Validators;

public class CompanyInvitationValidator : AbstractValidator<CompanyInvitationFormModel>
{
    public CompanyInvitationValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Firmenname ist erforderlich")
            .MaximumLength(200);

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Kontakt-E-Mail ist erforderlich")
            .EmailAddress().WithMessage("Ungültige E-Mail-Adresse")
            .MaximumLength(200);

        RuleFor(x => x.ContactPhone)
            .MaximumLength(50);

        RuleFor(x => x.PercentageDiscount)
            .InclusiveBetween(0, 100).When(x => x.PercentageDiscount.HasValue)
            .WithMessage("Rabatt muss zwischen 0% und 100% liegen");

        RuleFor(x => x.PersonalMessage)
            .MaximumLength(2000);

        RuleForEach(x => x.AgendaItemPrices)
            .SetValidator(new CompanyAgendaItemPriceValidator());
    }
}

public class CompanyAgendaItemPriceValidator : AbstractValidator<CompanyAgendaItemPriceModel>
{
    public CompanyAgendaItemPriceValidator()
    {
        RuleFor(x => x.ManualOverride)
            .GreaterThanOrEqualTo(0).When(x => x.ManualOverride.HasValue)
            .WithMessage("Preis darf nicht negativ sein");
    }
}
