using CoachOS.Application.StudentAuth.DTOs;
using FluentValidation;

namespace CoachOS.Application.StudentAuth.Validators;

public class RedeemMagicLinkRequestValidator : AbstractValidator<RedeemMagicLinkRequest>
{
    public RedeemMagicLinkRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is verplicht");
    }
}
