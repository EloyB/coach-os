using System.Globalization;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Camps;

/// <summary>Controleert of een gekozen set dagdelen past bij de dagen en instellingen van een kamp.</summary>
public static class CampDaySelectionRules
{
    public static Error? Validate(Camp camp, IReadOnlyList<CampDaySelectionDto> selection)
    {
        if (selection.Count == 0)
            return Fail("Kies minstens één dag.");

        if (selection.Select(s => s.CampDayId).Distinct().Count() != selection.Count)
            return Fail("Een dag mag maar één keer gekozen worden.");

        foreach (CampDaySelectionDto choice in selection)
        {
            CampDay? day = camp.Days.FirstOrDefault(d => d.Id == choice.CampDayId);
            if (day is null)
                return Fail("Een gekozen dag hoort niet bij dit kamp.");

            string label = day.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            if (!choice.Morning && !choice.Afternoon)
                return Fail($"Kies voormiddag, namiddag of beide voor {label}.");
            if (choice.Morning && day.MorningStart is null)
                return Fail($"Er is geen voormiddag op {label}.");
            if (choice.Afternoon && day.AfternoonStart is null)
                return Fail($"Er is geen namiddag op {label}.");

            bool wholeDay = choice.Morning == day.MorningStart.HasValue && choice.Afternoon == day.AfternoonStart.HasValue;
            if (!camp.AllowHalfDays && !wholeDay)
                return Fail($"Halve dagen zijn niet mogelijk voor dit kamp ({label}).");
        }

        return null;
    }

    private static Error Fail(string message) => new(ErrorCodes.Validation, message);
}
