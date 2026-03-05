using FluentValidation;

namespace CoachOS.Application.Trainers.Commands.DeactivateTrainer;

public class DeactivateTrainerCommandValidator : AbstractValidator<DeactivateTrainerCommand>
{
    public DeactivateTrainerCommandValidator()
    {
        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("TrainerId is verplicht");

        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("OrganizationId is verplicht");
    }
}
