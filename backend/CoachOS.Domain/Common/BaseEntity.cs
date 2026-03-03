namespace CoachOS.Domain.Common;

/// <summary>
/// Base class voor alle domain entities met audit fields.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
