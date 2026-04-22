namespace CoachOS.Application.Dashboard.DTOs;

public class InboxDto
{
    public List<InboxItemDto> Items { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
}
