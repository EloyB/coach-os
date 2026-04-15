using CoachOS.Application.Auth.DTOs;
using FluentValidation;

namespace CoachOS.Application.Auth.Validators;

public class SwitchOrganizationRequestValidator : AbstractValidator<SwitchOrganizationRequest>
{
    public SwitchOrganizationRequestValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty().WithMessage("OrganizationId is verplicht");
    }
}
