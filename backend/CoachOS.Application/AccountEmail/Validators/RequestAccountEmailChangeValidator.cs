using CoachOS.Application.AccountEmail.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CoachOS.Application.AccountEmail.Validators;

public class RequestAccountEmailChangeValidator : AbstractValidator<RequestAccountEmailChange>
{
    public RequestAccountEmailChangeValidator(IStringLocalizer<AccountEmailResources> text)
    {
        RuleFor(x => x.NewEmail).NotEmpty().WithMessage(text["EmailRequired"])
            .EmailAddress().WithMessage(text["EmailInvalid"])
            .MaximumLength(256).WithMessage(text["EmailInvalid"]);
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage(text["PasswordRequired"])
            .MaximumLength(1024).WithMessage(text["PasswordInvalid"]);
    }
}