namespace CoachOS.Application.Camps.DTOs;

/// <summary>
/// De betaalkeuze van een deelnemer voor een kampinschrijving.
/// <see cref="Method"/>: 1 = Online (Mollie), 2 = Cash (coach bevestigt later).
/// </summary>
public record ChooseCampPaymentRequest
{
    public int Method { get; init; }
}
