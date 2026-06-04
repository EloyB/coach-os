using CoachOS.Application.Onboarding.DTOs;
using FluentValidation;

namespace CoachOS.Application.Onboarding.Validators;

public class SetTrainerModeRequestValidator : AbstractValidator<SetTrainerModeRequest>
{
    public SetTrainerModeRequestValidator()
    {
        // Geen veld-niveau regels: AdminActsAsTrainer is een verplichte bool die beide waarden kan
        // aannemen. Validator staat klaar voor toekomstige uitbreiding (bv. extra modes).
    }
}
