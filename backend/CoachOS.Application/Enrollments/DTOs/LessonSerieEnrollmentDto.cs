namespace CoachOS.Application.Enrollments.DTOs;

public class LessonSerieEnrollmentDto
{
    public Guid Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? StudentEmail { get; set; }
    public string? StudentPhone { get; set; }

    /// <summary>Adres waar de communicatie voor deze inschrijving heen gaat.</summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>False = communicatie loopt via de contactpersoon van de groep.</summary>
    public bool HasOwnEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public string? Notes { get; set; }

    /// <summary>Geboortedatum als yyyy-MM-dd, null voor inschrijvingen van vóór deze feature.</summary>
    public string? DateOfBirth { get; set; }

    /// <summary>1 = volwassenen, 2 = jeugd, null = onbekend.</summary>
    public int? Category { get; set; }

    public string? CategoryLabel { get; set; }

    /// <summary>Null = solo-inschrijving; anders de groep waartoe deze inschrijving hoort.</summary>
    public Guid? EnrollmentGroupId { get; set; }

    /// <summary>True als deze inschrijving de groepsleider is (draagt de gedeelde betaling).</summary>
    public bool IsGroupLeader { get; set; }

    public bool IsOpenToGrouping { get; set; }

    /// <summary>Gekozen prijsoptie (null = geen optie/legacy prijs). Voor de aanpas-dialog.</summary>
    public Guid? SelectedPriceOptionId { get; set; }

    public List<EnrollmentResponseItemDto> FormResponses { get; set; } = new();
}
