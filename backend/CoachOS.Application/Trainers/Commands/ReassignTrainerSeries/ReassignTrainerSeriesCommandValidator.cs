using FluentValidation;

namespace CoachOS.Application.Trainers.Commands.ReassignTrainerSeries;

public class ReassignTrainerSeriesCommandValidator : AbstractValidator<ReassignTrainerSeriesCommand>
{
    public ReassignTrainerSeriesCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("OrganizationId is verplicht");

        RuleFor(x => x.FromTrainerId)
            .NotEmpty().WithMessage("FromTrainerId is verplicht");

        RuleFor(x => x.ToTrainerId)
            .NotEmpty().WithMessage("ToTrainerId is verplicht")
            .NotEqual(x => x.FromTrainerId).WithMessage("ToTrainerId mag niet gelijk zijn aan FromTrainerId");
    }
}
