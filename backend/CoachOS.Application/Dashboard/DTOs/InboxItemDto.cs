namespace CoachOS.Application.Dashboard.DTOs;

public class InboxItemDto
{
    public string Type { get; set; } = string.Empty;
    public string RefType { get; set; } = string.Empty;
    public Guid RefId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Meta { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
