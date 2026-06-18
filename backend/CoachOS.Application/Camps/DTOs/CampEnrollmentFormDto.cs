namespace CoachOS.Application.Camps.DTOs;

public class CampEnrollmentFormDto
{
    public Guid Id { get; set; }
    public Guid CampId { get; set; }
    public List<CampFormFieldDto> Fields { get; set; } = new();
}
