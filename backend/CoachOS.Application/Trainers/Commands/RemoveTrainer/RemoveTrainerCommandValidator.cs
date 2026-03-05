using FluentValidation;

namespace CoachOS.Application.Trainers.Commands.RemoveTrainer;

public class RemoveTrainerCommandValidator : AbstractValidator<RemoveTrainerCommand>
{
    public RemoveTrainerCommandValidator()
    {
        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("TrainerId is verplicht");

        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("OrganizationId is verplicht");
    }
}
