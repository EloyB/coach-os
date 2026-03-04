namespace CoachOS.Application.Common.Interfaces;

public interface IUserLookupService
{
    Task<Dictionary<Guid, string>> GetUserNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<string?> GetUserNameByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<(Guid Id, string FullName)>> GetOrganizationMembersAsync(Guid organizationId, CancellationToken ct = default);
}
