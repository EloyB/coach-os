using CoachOS.Domain.Interfaces;

namespace CoachOS.Infrastructure.MultiTenancy;

/// <summary>
/// Scoped tenant-context. Gevuld door <c>TenantContextMiddleware</c> direct
/// na authenticatie. De middleware resolvet deze concrete class en roept
/// <see cref="Set"/> aan; de rest van de applicatie leest via <see cref="ITenantContext"/>.
/// </summary>
public class TenantContext : ITenantContext
{
    public Guid OrganizationId { get; private set; } = Guid.Empty;
    public Guid UserId { get; private set; } = Guid.Empty;
    public bool IsAuthenticated => OrganizationId != Guid.Empty;

    public void Set(Guid organizationId, Guid userId)
    {
        OrganizationId = organizationId;
        UserId = userId;
    }
}
