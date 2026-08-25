using CoachOS.Application.Trainers.DTOs;
using FluentValidation;

namespace CoachOS.Application.Trainers.Validators;

public class SetHeadTrainerClubsRequestValidator : AbstractValidator<SetHeadTrainerClubsRequest>
{
    public SetHeadTrainerClubsRequestValidator()
    {
        RuleFor(x => x.ClubIds)
            .NotNull().WithMessage("ClubIds is verplicht (mag leeg zijn).");

        RuleForEach(x => x.ClubIds)
            .NotEmpty().WithMessage("Ongeldige club-id.");

        RuleFor(x => x.ClubIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .When(x => x.ClubIds is not null)
            .WithMessage("Dubbele club-id's zijn niet toegestaan.");
    }
}
