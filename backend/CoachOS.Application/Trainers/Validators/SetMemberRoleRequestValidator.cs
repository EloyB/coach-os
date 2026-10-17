using CoachOS.Application.Trainers.DTOs;
using FluentValidation;

namespace CoachOS.Application.Trainers.Validators;

public class SetMemberRoleRequestValidator : AbstractValidator<SetMemberRoleRequest>
{
    public SetMemberRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .Must(r => r is "Admin" or "Trainer")
            .WithMessage("Rol moet 'Admin' of 'Trainer' zijn");
    }
}
