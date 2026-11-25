namespace CoachOS.Application.Enrollments.DTOs;

public record SaveFormFieldRequest
{
    public Guid? Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public int Type { get; init; }
    public bool IsRequired { get; init; }
    /// <summary>0 = per persoon (standaard), 1 = één keer voor de hele inschrijving.</summary>
    public int Scope { get; init; }
    public int Order { get; init; }
    public List<string>? Options { get; init; }
}

public record SaveEnrollmentFormRequest
{
    public List<SaveFormFieldRequest> Fields { get; init; } = new();
}
