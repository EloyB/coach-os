using CoachOS.Application.Trainers.DTOs;
using FluentValidation;

namespace CoachOS.Application.Trainers.Validators;

public class ReassignSeriesRequestValidator : AbstractValidator<ReassignSeriesRequest>
{
    public ReassignSeriesRequestValidator()
    {
        RuleFor(x => x.ToTrainerId)
            .NotEmpty().WithMessage("ToTrainerId is verplicht");
    }
}
