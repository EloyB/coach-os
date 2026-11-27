using CoachOS.Application.AccountEmail.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CoachOS.Application.AccountEmail.Validators;

public class ConfirmAccountEmailChangeValidator : AbstractValidator<ConfirmAccountEmailChange>
{
    public ConfirmAccountEmailChangeValidator(IStringLocalizer<AccountEmailResources> text)
    {
        RuleFor(x => x.NewEmail).NotEmpty().WithMessage(text["EmailRequired"])
            .EmailAddress().WithMessage(text["EmailInvalid"])
            .MaximumLength(256).WithMessage(text["EmailInvalid"]);
        RuleFor(x => x.Token).NotEmpty().WithMessage(text["LinkInvalid"])
            .MaximumLength(4096).WithMessage(text["LinkInvalid"]);
    }
}