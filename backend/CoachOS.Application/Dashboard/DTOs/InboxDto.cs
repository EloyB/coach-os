namespace CoachOS.Application.Dashboard.DTOs;

public class InboxDto
{
    public List<InboxItemDto> Items { get; set; } = [];

    /// <summary>Totaal aantal openstaande items, ook als <see cref="Items"/> door de limiet korter is.</summary>
    public int TotalCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}
