using CoachOS.Application.OrganizationSettings.DTOs;
using FluentValidation;

namespace CoachOS.Application.OrganizationSettings.Validators;

public class UpdateOrganizationSettingsRequestValidator : AbstractValidator<UpdateOrganizationSettingsRequest>
{
    public UpdateOrganizationSettingsRequestValidator()
    {
        // Geen veld-niveau regels: AdminsActAsTrainers is een verplichte bool en kan
        // beide waarden aannemen. Dit class staat klaar voor toekomstige settings die wél
        // validatie nodig hebben (bv. enum-velden, ranges, regex).
    }
}
