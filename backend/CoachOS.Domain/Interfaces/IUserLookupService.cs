namespace CoachOS.Domain.Interfaces;

public interface IUserLookupService
{
    Task<Dictionary<Guid, string>> GetUserNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<string?> GetUserNameByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<(Guid Id, string FullName)>> GetOrganizationMembersAsync(Guid organizationId, CancellationToken ct = default);
    Task<bool> IsActiveTrainerAsync(Guid trainerId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Telt actieve memberships in deze organisatie met uitsluitend rol Trainer.
    /// Admins tellen hier NIET mee, ook al kunnen ze zelf lesgeven — voor dashboard-
    /// statistieken willen we enkel de "echte" trainers.
    /// </summary>
    Task<int> CountActiveTrainersAsync(Guid organizationId, CancellationToken ct = default);
    Task<Dictionary<Guid, (string FullName, string Email)>> GetUserNamesAndEmailsByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<(string FullName, string Email)?> GetUserInfoByIdAsync(Guid id, CancellationToken ct = default);
}
