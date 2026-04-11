using CoachOS.Application.Planning.DTOs;
using FluentValidation;

namespace CoachOS.Application.Planning.Validators;

public class UpdateAssignmentRequestValidator : AbstractValidator<UpdateAssignmentRequest>
{
    public UpdateAssignmentRequestValidator()
    {
        RuleFor(x => x.WeeklyTemplateEntryId)
            .NotEmpty().WithMessage("WeeklyTemplateEntryId is verplicht");
    }
}
