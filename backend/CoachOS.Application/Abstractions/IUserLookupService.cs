namespace CoachOS.Application.Abstractions;

public interface IUserLookupService
{
    Task<Dictionary<Guid, string>> GetUserNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<string?> GetUserNameByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<(Guid Id, string FullName)>> GetOrganizationMembersAsync(Guid organizationId, CancellationToken ct = default);
    Task<bool> IsActiveTrainerAsync(Guid trainerId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Telt actieve trainer-memberships in deze organisatie.
    /// Admins tellen mee wanneer AdminsActAsTrainers aan staat, zodat het dashboard-
    /// cijfer overeenkomt met de trainerslijst die aan de admin getoond wordt.
    /// </summary>
    /// <summary>Wekelijkse capaciteit (uren) van een lid binnen de organisatie; null als het lid niet bestaat.</summary>
    Task<int?> GetWeeklyCapacityHoursAsync(Guid userId, Guid organizationId, CancellationToken ct = default);
    Task<int> CountActiveTrainersAsync(Guid organizationId, CancellationToken ct = default);
    Task<Dictionary<Guid, (string FullName, string Email)>> GetUserNamesAndEmailsByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<(string FullName, string Email)?> GetUserInfoByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>E-mailadressen van de actieve beheerders (rol Admin) van een organisatie.</summary>
    Task<IReadOnlyList<string>> GetActiveAdminEmailsAsync(Guid organizationId, CancellationToken ct = default);
}
