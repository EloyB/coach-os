using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Subscriptions;
using FluentValidation;

namespace CoachOS.Application.Billing.Validators;

public class SaveBillingProfileRequestValidator : AbstractValidator<SaveBillingProfileRequest>
{
    public SaveBillingProfileRequestValidator()
    {
        RuleFor(x => x.Name).Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Naam is verplicht")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn");
        RuleFor(x => x.Street).Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Straat en nummer zijn verplicht")
            .MaximumLength(200).WithMessage("Straat mag maximaal 200 karakters zijn");
        RuleFor(x => x.PostalCode).Matches(@"^\d{4}$").WithMessage("Postcode moet uit 4 cijfers bestaan");
        RuleFor(x => x.City).Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Gemeente is verplicht")
            .MaximumLength(100).WithMessage("Gemeente mag maximaal 100 karakters zijn");
        RuleFor(x => x.Country).Length(2).WithMessage("Ongeldig land");
        RuleFor(x => x.InvoiceEmail).NotEmpty().WithMessage("Factuur-e-mail is verplicht")
            .EmailAddress().WithMessage("Ongeldig e-mailadres")
            .MaximumLength(256).WithMessage("E-mailadres mag maximaal 256 karakters zijn");
        RuleFor(x => x.VatNumber)
            .Must(v => BelgianVat.IsValidFormat(v!)).WithMessage("Ongeldig Belgisch btw-nummer (bv. BE0123456789)")
            .When(x => !string.IsNullOrWhiteSpace(x.VatNumber));
    }
}
