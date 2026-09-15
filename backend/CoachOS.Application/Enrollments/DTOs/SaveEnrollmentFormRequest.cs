namespace CoachOS.Application.Enrollments.DTOs;

public record SaveFormFieldRequest
{
    public Guid? Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public int Type { get; init; }
    public bool IsRequired { get; init; }
    public bool IsForEachGroupMember { get; init; }
    public int Order { get; init; }
    public List<string>? Options { get; init; }
}

public record SaveEnrollmentFormRequest
{
    public List<SaveFormFieldRequest> Fields { get; init; } = new();
}
