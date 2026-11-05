namespace CoachOS.Application.Subscriptions;

/// <summary>Resolve an existing public capability/resource to its tenant; never use the caller's unrelated session.</summary>
public interface IOrganizationPublicScopeResolver
{
    Task<Guid?> ResolveAsync(string resource, string value, CancellationToken ct = default);
}
