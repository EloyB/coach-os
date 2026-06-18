namespace CoachOS.Application.Camps.DTOs;

public record SaveCampFormFieldRequest
{
    public Guid? Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public int Type { get; init; }
    public bool IsRequired { get; init; }
    public int Order { get; init; }
    public List<string>? Options { get; init; }
}

public record SaveCampFormRequest
{
    public List<SaveCampFormFieldRequest> Fields { get; init; } = new();
}
