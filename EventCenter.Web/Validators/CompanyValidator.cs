using FluentValidation;
using EventCenter.Web.Domain.Entities;

namespace EventCenter.Web.Validators;

public class CompanyValidator : AbstractValidator<Company>
{
    public CompanyValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Firmenname ist erforderlich")
            .MaximumLength(200);

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("E-Mail ist erforderlich")
            .EmailAddress().WithMessage("Ungültige E-Mail-Adresse")
            .MaximumLength(200);

        RuleFor(x => x.ContactPhone)
            .MaximumLength(50);

        RuleFor(x => x.ContactFirstName)
            .MaximumLength(100);

        RuleFor(x => x.ContactLastName)
            .MaximumLength(100);

        RuleFor(x => x.Street)
            .MaximumLength(200);

        RuleFor(x => x.PostalCode)
            .MaximumLength(20);

        RuleFor(x => x.City)
            .MaximumLength(100);

        RuleFor(x => x.Notes)
            .MaximumLength(2000);
    }
}
