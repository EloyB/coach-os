using CoachOS.Application.Abstractions;

namespace CoachOS.Infrastructure.Email;

/// <summary>
/// Scoped: bepaalt bij welke organisatie een mail in de outbox hoort. Eerst de organisatie die
/// een publieke flow expliciet meegaf, anders de tenant van het verzoek, anders geen
/// (platformmails zoals wachtwoordreset of website).
/// </summary>
internal sealed class EmailOrganizationContext(ITenantContext tenant) : IEmailOrganizationContext
{
    private Guid? _explicit;

    public void Use(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Een mail kan niet bij een lege organisatie horen.", nameof(organizationId));
        _explicit = organizationId;
    }

    public void Clear() => _explicit = null;

    public Guid? Current
        => _explicit ?? (tenant.OrganizationId == Guid.Empty ? null : tenant.OrganizationId);
}
